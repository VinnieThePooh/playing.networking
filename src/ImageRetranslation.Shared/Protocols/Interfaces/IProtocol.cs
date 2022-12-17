using System.Net.Sockets;

namespace ImageRetranslationShared.Protocols;

public interface IProtocol
{
    Task DoCommunication(TcpClient party, CancellationToken token);
}