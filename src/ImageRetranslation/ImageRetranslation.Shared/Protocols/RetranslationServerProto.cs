using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using ImageRetranslationShared.Commands;
using ImageRetranslationShared.Events;
using ImageRetranslationShared.Extensions;

namespace ImageRetranslationShared.Protocols;

public class RetranslationServerProto : IServerProtocol
{
    public EventHandler<ClientTypeDetectedEventArgs>? ClientTypeDetected;
    public EventHandler<ImageUploadedEventArgs>? ImageUploaded;

    public async Task DoCommunication(TcpClient party, CancellationToken token)
    {
        var stream = party.GetStream();
        var memory = new byte[1024];

        await stream.ReadExactlyAsync(memory, 0, 1, token);
        var clientType = (ClientType)memory[0];
        ClientTypeDetected?.Invoke(this, new ClientTypeDetectedEventArgs(clientType));

        // don't do anything here
        // cause of we need to use event-based approach for Receivers
        // kinda deferred protocol execution
        if (clientType == ClientType.Receiver)
            return;

        var numberOfFiles = await stream.ReadInt(memory, token);
        Debug.IndentLevel = 2;
        Debug.WriteLine($"Number of files: {numberOfFiles}");

        var memoryWrapper = new Memory<byte>(memory);

        IterInfo iterInfo = new()
        {
            BatchSize = numberOfFiles,
            EventOrderNumber = 1,
            Buffer = memoryWrapper,
            NewMessageData = Memory<byte>.Empty
        };

        for (int i = 0; i < numberOfFiles; i++)
        {
            iterInfo = await ReadFileData(party, iterInfo, token);
            if (iterInfo.IsDisconnectedPrematurely)
                break;
        }
        party.Close();
    }

    private async Task<IterInfo> ReadFileData(TcpClient party, IterInfo iterInfo, CancellationToken token)
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
            //assume new data is enough to read "preamble"
            nameLength = newMessage.Span.GetHostOrderInt();
            nameBytes = newMessage[4..(nameLength + 4)].ToArray();
            dataLength = newMessage[(nameLength + 4)..].Span.GetHostOrderInt64();

            var dataLeft = newMessage[(nameLength + 12)..];
            if (!dataLeft.IsEmpty)
            {
                leftToRead = dataLength - dataLeft.Length;
                totalRead = dataLeft.Length;
                await memoryStream.WriteAsync(dataLeft, token);
            }
        }

        Debug.WriteLine($"[Preamble]: Name length: {nameLength}");
        Debug.WriteLine($"[Preamble]: Name: {Encoding.UTF8.GetString(nameBytes)}");
        Debug.WriteLine($"[Preamble]: Length of image stream: {dataLength}");
        Debug.WriteLine($"[Preamble]: Preamble is left buffer data: {!newMessage.IsEmpty}");
        Debug.WriteLine($"[Preamble]: totalRead before main loop: {totalRead}");
        Debug.WriteLine($"[Preamble]: leftToRead before main loop: {leftToRead}\n\n");

        while (totalRead < dataLength)
        {
            read = await stream.ReadAsync(memory, token);

            if (read == 0)
            {
                Console.WriteLine($"[RetranslationServer]: Client {party.GetRemoteEndpoint()} disconnected prematurely");
                stream.Close();
                return IterInfo.DisconnectedPrematurely;
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
                EventOrderNumber = iterInfo.EventOrderNumber,
                BatchSize = iterInfo.BatchSize
            });

        iterInfo.EventOrderNumber++;
        iterInfo.NewMessageData = toWrite < read ? memory[toWrite..read] : Memory<byte>.Empty;
        return iterInfo;
    }

    private struct IterInfo
    {
        public int EventOrderNumber { get; set; }

        public int BatchSize { get; set; }

        public Memory<byte> Buffer { get; set; }

        public Memory<byte> NewMessageData { get; set; }

        public bool IsDisconnectedPrematurely { get; init; }

        public static IterInfo DisconnectedPrematurely { get; } = new() { IsDisconnectedPrematurely = true };
    }
}