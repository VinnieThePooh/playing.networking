using DataStreaming.Services;

namespace DataStreaming.Common.Protocols.Factories;

//todo: may be implement some tricky IClientProtocol later
public class ImageRetranslationProtocolFactory : IProtocolFactory
{
    private ImageRetranslationProtocolFactory()
    {
    }

    public IClientProtocol CreateClientProtocol() => throw new NotSupportedException($"Not supported: Use {nameof(IFileSender)} or {nameof(IFileReceiver)} implementations instead");

    public IServerProtocol CreateServerProtocol() => new RetranslationServerProto();

    public static IProtocolFactory Create() => new ImageRetranslationProtocolFactory();
}