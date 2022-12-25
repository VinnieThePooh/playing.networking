using System.Net.Sockets;

namespace DataStreaming.Common.Protocols;

public interface IProtocol
{
    Task DoCommunication(TcpClient party, CancellationToken token);
}