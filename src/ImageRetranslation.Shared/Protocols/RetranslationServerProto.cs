using System.Diagnostics;
using System.Net;
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
        if (clientType == ClientType.Receiver)
            return;

        await stream.ReadExactlyAsync(memory, 0, 4, token);

        var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(memory[..8]));
        Debug.WriteLine($"Length of image stream: {length}");
        int totalRead = 0;
        int bytesRead = 0;

        var memoryWrapper = new Memory<byte>(memory);
        await using var memoryStream = new MemoryStream();

        while (totalRead < length)
        {
            bytesRead = await party.Client.ReceiveAsync(memoryWrapper, token);

            if (bytesRead == 0)
            {
                Console.WriteLine($"[RetranslationServer]: Client {party.GetRemoteEndpoint()} got disconnected prematurely");
                party.Close();

                return;
            }

            memoryStream.Write(memoryWrapper[..bytesRead].Span);
            totalRead += bytesRead;
        }

        ImageUploaded?.Invoke(this, new ImageUploadedEventArgs(party.GetRemoteEndpoint()!, memoryStream.ToArray()));
        party.Close();
    }
}