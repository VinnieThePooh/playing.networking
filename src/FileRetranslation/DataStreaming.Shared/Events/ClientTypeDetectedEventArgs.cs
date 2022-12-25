using DataStreaming.Common.Constants;

namespace DataStreaming.Common.Events;

public class ClientTypeDetectedEventArgs
{
    public ClientTypeDetectedEventArgs(ClientType type)
    {
        Type = type;
    }

    public ClientType Type { get; }
}