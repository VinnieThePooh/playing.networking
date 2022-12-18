using System.Net;
using System.Net.Sockets;

namespace ImageRetranslationShared.Extensions;

public static class TcpClientExtensions
{
    public static IPEndPoint? GetRemoteEndpoint(this TcpClient client)
    {
        if (client.Client.RemoteEndPoint is null)
            return null;

        return (IPEndPoint)client.Client.RemoteEndPoint!;
    }
}