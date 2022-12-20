using System.Net.Sockets;

namespace ImageRetranslationShared.Protocols;

public abstract class RetranslationClientProto : IClientProtocol
{
    public abstract Task DoCommunication(TcpClient party, CancellationToken token);
}