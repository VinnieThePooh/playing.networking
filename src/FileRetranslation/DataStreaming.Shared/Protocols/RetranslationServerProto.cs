using System.Net.Sockets;
using DataStreaming.Common.Constants;
using DataStreaming.Common.Events;
using DataStreaming.Common.Extensions;
using DataStreaming.Common.Settings;
using ImageRetranslationShared.Infrastructure;
using ImageRetranslationShared.Models;

namespace DataStreaming.Common.Protocols;

public class RetranslationServerProto : IServerProtocol
{
    public EventHandler<ClientTypeDetectedEventArgs>? ClientTypeDetected;
    public EventHandler<ImageUploadedEventArgs>? ImageUploaded;

    public FileRetranslationSettings? RetranslationSettings { get; set; }

    public async Task DoCommunication(TcpClient party, CancellationToken token)
    {
        var stream = party.GetStream();
        var memory = new byte[RetranslationSettings?.BufferSize ?? FileRetranslationSettings.FallbackBufferSize];

        await stream.ReadExactlyAsync(memory, 0, 1, token);
        var clientType = (ClientType)memory[0];
        ClientTypeDetected?.Invoke(this, new ClientTypeDetectedEventArgs(clientType));

        // don't do anything here
        // cause of we need to use event-based approach for Receivers
        // kinda deferred protocol execution
        if (clientType == ClientType.Receiver)
            return;

        var numberOfFiles = await stream.ReadInt(memory, token);

        var memoryWrapper = new Memory<byte>(memory);

        StreamingInfo iterInfo = new()
        {
            BatchSize = numberOfFiles,
            MessageOrderNumber = 1,
            Buffer = memoryWrapper
        };

        for (int i = 0; i < numberOfFiles; i++)
        {
            try
            {
                iterInfo = await ReadFileData(party, iterInfo, token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                throw;
            }

            if (iterInfo.IsDisconnectedPrematurely)
                break;
        }

        party.Close();
    }

    //todo: refactor, add logging
    private async Task<StreamingInfo> ReadFileData(TcpClient party, StreamingInfo iterInfo, CancellationToken token)
    {
        var memory = iterInfo.Buffer;
        var stream = party.GetStream();

        int nameLength;
        byte[] nameBytes;
        long dataLength;

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

            nameBytes = readResult.NameBytes;
            nameLength = readResult.NameLength;
            dataLength = readResult.DataLength;

            var dataLeft = readResult.DataLeft;
            if (!dataLeft.IsEmpty)
            {
                leftToRead = readResult.DataLength - dataLeft.Length;
                totalRead = dataLeft.Length;
                await memoryStream.WriteAsync(dataLeft, token);
            }
        }

        // Debug.WriteLine($"[Preamble]: Name length: {nameLength}");
        // Debug.WriteLine($"[Preamble]: Name: {Encoding.UTF8.GetString(nameBytes)}");
        // Debug.WriteLine($"[Preamble]: Length of image stream: {dataLength}");
        // Debug.WriteLine($"[Preamble]: Preamble is left buffer data: {!newMessage.IsEmpty}");
        // Debug.WriteLine($"[Preamble]: totalRead before main loop: {totalRead}");
        // Debug.WriteLine($"[Preamble]: leftToRead before main loop: {leftToRead}\n");

        while (totalRead < dataLength)
        {
            read = await stream.ReadAsync(memory, token);

            if (read == 0)
            {
                Console.WriteLine(
                    $"[RetranslationServer]: Client {party.GetRemoteEndpoint()} disconnected prematurely");
                stream.Close();

                return StreamingInfo.DisconnectedPrematurely;
            }

            toWrite = (int)Math.Min(read, leftToRead);
            memoryStream.Write(memory[..toWrite].Span);
            leftToRead -= toWrite;
            totalRead += read;
        }

        ImageUploaded?.Invoke(this,
            new ImageUploadedEventArgs
            {
                ImageData = memoryStream.ToArray(),
                ImageNameData = nameBytes,
                Uploader = party.GetRemoteEndpoint()!,
                MessageOrderNumber = iterInfo.MessageOrderNumber,
                BatchSize = iterInfo.BatchSize
            });

        iterInfo.MessageOrderNumber++;
        iterInfo.NewMessageData = toWrite < read ? memory[toWrite..read] : Memory<byte>.Empty;

        return iterInfo;
    }
}