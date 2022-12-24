using System.Collections.Concurrent;
using System.Net;

namespace Retranslation;

public interface IRetranslationServer
{
    Task<bool> Start();

    Task<bool> Stop();

    Dictionary<IPEndPoint, ClientProxy> ClientProxies { get; }
}