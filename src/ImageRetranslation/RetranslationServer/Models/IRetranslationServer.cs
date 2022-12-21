using System.Collections.Concurrent;
using System.Net;

namespace RetranslationServer.Models;

public interface IRetranslationServer
{
    Task<bool> Start();

    Task<bool> Stop();

    ConcurrentDictionary<IPEndPoint, ClientProxy> ReceiversDictionary { get; }
}