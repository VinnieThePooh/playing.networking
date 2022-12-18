using System.Net.Sockets;

namespace ImageRetranslationShared.Protocols;

public class RetranslationClientProto : IClientProtocol
{
    public Task DoCommunication(TcpClient party, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}