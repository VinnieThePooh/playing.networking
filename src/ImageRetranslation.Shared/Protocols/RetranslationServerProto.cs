using System.Net;
using System.Net.Sockets;

namespace ImageRetranslationShared.Protocols;

public class RetranslationServerProto : IServerProtocol
{
    public async Task DoCommunication(TcpClient party, CancellationToken token)
    {
        //todo: two different tasks for different Simplex clients? cause of we only send or only receive
        //OK, with timeout kinda
        
        await using var stream = party.GetStream();
        var memory = new byte[1024];
        await stream.ReadExactlyAsync(memory, 0, 8, token);

        var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(memory[..8]));
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