using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using ImageRetranslationShared.Commands;
using ImageRetranslationShared.Events;

namespace ImageRetranslationShared.Protocols;

public class RetranslationServerProto : IServerProtocol
{
    public EventHandler<ClientTypeDetectedEventArgs> ClientTypeDetected;

    public async Task DoCommunication(TcpClient party, CancellationToken token)
    {
        var stream = party.GetStream();
        var memory = new byte[1024];

        await stream.ReadExactlyAsync(memory, 0, 1, token);
        var clientType = (ClientType)memory[0];
        
        ClientTypeDetected(this, new ClientTypeDetectedEventArgs(clientType));
        
        await stream.ReadExactlyAsync(memory, 0, 8, token);

        var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(memory[..8]));
        Debug.WriteLine($"Length: {length}");
        int totalRead = 0;
        int bytesRead = 0;

        var memoryWrapper = new Memory<byte>(memory);

        while (totalRead < (8 + length))
        {
            bytesRead = await party.Client.ReceiveAsync(memoryWrapper, token);

            //todo: retranslate here to other clients immediately
            if (bytesRead == 0)
            {
                //handle premature close
            }

            totalRead += bytesRead;
        }

        // retranslate IPAddress to everybody here
    }
}