namespace ImageRetranslationShared.Protocols.Factories;

public class ImageRetranslationProtocolFactory : IProtocolFactory
{
    private ImageRetranslationProtocolFactory()
    {
    }

    public IClientProtocol CreateClientProtocol() => new RetranslationClientProto();

    public IServerProtocol CreateServerProtocol() => new RetranslationServerProto();

    public static IProtocolFactory Create() => new ImageRetranslationProtocolFactory();
}