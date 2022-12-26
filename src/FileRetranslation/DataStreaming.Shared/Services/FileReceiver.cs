using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using DataStreaming.Common.Constants;
using DataStreaming.Common.Events;
using DataStreaming.Common.Extensions;
using DataStreaming.Common.Settings;
using ImageRetranslationShared.Infrastructure;
using ImageRetranslationShared.Models;

namespace DataStreaming.Services;

public class FileReceiver : IFileReceiver
{
    private TcpClient? receiverParty;

    public event EventHandler<BatchLoadedEventArgs>? BatchLoaded;

    public FileReceiver(FileRetranslationSettings networkSettings)
    {
        NetworkSettings = networkSettings ?? throw new ArgumentNullException(nameof(networkSettings));
    }

    public FileRetranslationSettings NetworkSettings { get; }

    public async IAsyncEnumerable<NetworkFile> AwaitFiles([EnumeratorCancellation] CancellationToken token)
    {
        receiverParty = new();

        try
        {
            Console.Write("Connecting to host...");
            await receiverParty.ConnectAsync(IPAddress.Parse(NetworkSettings.Host), NetworkSettings.Port, token);
            Console.WriteLine("connected");

            var netStream = receiverParty.GetStream();
            Console.Write("Sending out client type attribute (ClientType = Receiver)...");
            netStream.WriteByte((byte)ClientType.Receiver);
            await netStream.FlushAsync(token);
            Console.WriteLine("completed");
        }
        catch (SocketException e)
        {
            Console.WriteLine(e);
            Environment.Exit(e.ErrorCode);
        }

        var memory = new Memory<byte>(new byte[NetworkSettings.BufferSize]);
        var streamingInfo = StreamingInfo.DefaultWithBuffer(memory);

        var fileNames = new List<string>();

        Console.WriteLine("Awaiting for image data...");
        while (!token.IsCancellationRequested)
        {
            streamingInfo = await ReadFileData(receiverParty, streamingInfo, token);

            if (streamingInfo.IsDisconnectedPrematurely)
            {
                Debug.WriteLine($"[FileReceiver]: RetranslationServer disconnected prematurely");
                yield break;
            }

            var networkFile = streamingInfo.NetworkFile!;
            yield return networkFile;

            streamingInfo.MessageOrderNumber++;
            fileNames.Add(streamingInfo.NetworkFile!.FileName);

            if (streamingInfo.IsEndOfBatch)
            {
                BatchLoaded?.Invoke(this, new BatchLoadedEventArgs(fileNames, streamingInfo.NetworkFile.Origin));
                fileNames.Clear();
                streamingInfo = StreamingInfo.DefaultWithBuffer(memory);
            }
        }
    }

    //todo: try to refactor?
    //todo: templated later some way?
    //pattern detected for working with length-prefixed streams
    private async ValueTask<StreamingInfo> ReadFileData(TcpClient party, StreamingInfo iterInfo, CancellationToken token)
    {
        var memory = iterInfo.Buffer;
        var stream = party.GetStream();
        int nameLength;
        byte[] nameBytes;
        long dataLength;
        string fileName;

        long totalRead = 0;
        long leftToRead = 0;
        int toWrite = 0;
        int read = 0;

        await using var memoryStream = new MemoryStream();
        var newMessage = iterInfo.NewMessageData;

        if (newMessage.IsEmpty)
        {
            nameLength = await stream.ReadInt(memory, token);
            await stream.ReadExactlyAsync(memory[..nameLength], token);
            nameBytes = memory[..nameLength].ToArray();
            dataLength = await stream.ReadLong(memory, token);
            leftToRead = dataLength;
        }
        else
        {
            var readResult = RetranslationUtility.ReadPreamble(newMessage, stream);
            if (readResult.IsDisconnectedPrematurely)
                return StreamingInfo.DisconnectedPrematurely;

            dataLength = readResult.DataLength;
            nameLength = readResult.NameLength;
            nameBytes = readResult.NameBytes;

            var dataLeft = readResult.DataLeft;
            if (!dataLeft.IsEmpty)
            {
                leftToRead = dataLength - dataLeft.Length;
                totalRead = dataLeft.Length;
                await memoryStream.WriteAsync(dataLeft, token);
            }
        }

        fileName = Encoding.UTF8.GetString(nameBytes);

        Debug.WriteLine($"[Preamble]: Name length: {nameLength}");
        Debug.WriteLine($"[Preamble]: Name: {fileName}");
        Debug.WriteLine($"[Preamble]: Length of image stream: {dataLength}");
        Debug.WriteLine($"[Preamble]: Preamble is left buffer data: {!newMessage.IsEmpty}");
        Debug.WriteLine($"[Preamble]: totalRead before main loop: {totalRead}");
        Debug.WriteLine($"[Preamble]: leftToRead before main loop: {leftToRead}\n");

        while (totalRead < dataLength)
        {
            read = await stream.ReadAsync(memory, token);

            if (read == 0)
            {
                Console.WriteLine($"[FileReceiver]: RetranslationServer {party.GetRemoteEndpoint()} disconnected prematurely");
                stream.Close();
                return StreamingInfo.DisconnectedPrematurely;
            }

            toWrite = (int)Math.Min(read, leftToRead);
            memoryStream.Write(memory[..toWrite].Span);
            leftToRead -= toWrite;
            totalRead += read;
        }

        Memory<byte> prologData;

        if (toWrite < read)
        {
            prologData = memory[toWrite..(toWrite + 16)];
        }
        else
        {
            prologData = memory[..16];
            await stream.ReadExactlyAsync(prologData, token);
        }

        long prolog = prologData[8..].Span.GetHostOrderInt64();

        var senderIp = new IPAddress(prologData[..4].Span);
        var portBytes = prologData[4..8].ToArray();
        var senderPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(portBytes));

        Debug.WriteLine($"IPAddress: {senderIp}");
        Debug.WriteLine($"Port: {senderPort}");
        Debug.WriteLine($"Network port bytes: {string.Join(' ', portBytes)}");

        var sender = new IPEndPoint(senderIp, senderPort);
        iterInfo.NetworkFile = new NetworkFile(fileName, memoryStream.ToArray(), sender);
        iterInfo.IsEndOfBatch = prolog.Equals(Prologs.EndOfBatch);

        Memory<byte> newMessageData = Memory<byte>.Empty;

        if (toWrite < read)
        {
            try
            {
                newMessageData = iterInfo.IsEndOfBatch ? memory[(toWrite + 16)..read] : memory[(toWrite + 8)..read];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                throw;
            }
        }

        iterInfo.NewMessageData = newMessageData;
        return iterInfo;
    }

    public ValueTask DisposeAsync()
    {
        if (receiverParty is null)
            return ValueTask.CompletedTask;

        receiverParty.Close();
        receiverParty = null;
        return ValueTask.CompletedTask;
    }
}