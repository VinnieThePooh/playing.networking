namespace DataStreaming.Common.Protocols;

public interface IProtocolFactory
{
    IClientProtocol CreateClientProtocol();

    IServerProtocol CreateServerProtocol();

    static abstract IProtocolFactory Create();
}