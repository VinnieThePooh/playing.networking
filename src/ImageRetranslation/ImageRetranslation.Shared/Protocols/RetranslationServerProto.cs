using System.Diagnostics;
using System.Net.Sockets;
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
        var memoryWrapper = new Memory<byte>(memory);
        for (int i = 0; i < numberOfFiles; i++)
            await ReadFileData(party, memoryWrapper, i + 1, numberOfFiles, token);

        party.Close();
    }

    private async Task ReadFileData(TcpClient party, Memory<byte> memory, int orderNumber, int batchSize, CancellationToken token)
    {
        var stream = party.GetStream();
        var nameLength = await stream.ReadInt(memory, token);
        await stream.ReadExactlyAsync(memory[..nameLength], token);
        var nameBytes = memory[..nameLength].ToArray();

        var dataLength = await stream.ReadLong(memory, token);

        Debug.WriteLine($"Length of image stream: {dataLength}");
        int totalRead = 0;

        await using var memoryStream = new MemoryStream();

        while (totalRead < dataLength)
        {
            var bytesRead = await stream.ReadAsync(memory, token);

            if (bytesRead == 0)
            {
                Console.WriteLine($"[RetranslationServer]: Client {party.GetRemoteEndpoint()} disconnected prematurely");
                stream.Close();
                return;
            }

            memoryStream.Write(memory[..bytesRead].Span);
            totalRead += bytesRead;
        }

        ImageUploaded?.Invoke(this,
            new ImageUploadedEventArgs
            {
                ImageData = memoryStream.ToArray(),
                ImageNameData = nameBytes,
                Uploader = party.GetRemoteEndpoint()!,
                EventOrderNumber = orderNumber,
                BatchSize = batchSize
            });
    }
}