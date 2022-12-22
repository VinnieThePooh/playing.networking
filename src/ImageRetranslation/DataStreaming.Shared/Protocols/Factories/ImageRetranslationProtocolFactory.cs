namespace DataStreaming.Common.Protocols.Factories;

public class ImageRetranslationProtocolFactory : IProtocolFactory
{
    private ImageRetranslationProtocolFactory()
    {
    }

    public IClientProtocol CreateClientProtocol() => throw new NotSupportedException("Not supported: Use IFileSender service instead of IClientProtocol");

    public IServerProtocol CreateServerProtocol() => new RetranslationServerProto();

    public static IProtocolFactory Create() => new ImageRetranslationProtocolFactory();
}