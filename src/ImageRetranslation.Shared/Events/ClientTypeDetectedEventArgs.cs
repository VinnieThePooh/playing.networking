using ImageRetranslationShared.Commands;

namespace ImageRetranslationShared.Events;

public class ClientTypeDetectedEventArgs
{
    public ClientTypeDetectedEventArgs(ClientType type)
    {
        Type = type;
    }

    public ClientType Type { get; }
}