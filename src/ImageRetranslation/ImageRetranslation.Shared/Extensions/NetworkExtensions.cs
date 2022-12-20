using System.Net;
using System.Net.Sockets;

namespace ImageRetranslationShared.Extensions;

public static class NetworkExtensions
{
    public static IPEndPoint? GetRemoteEndpoint(this TcpClient client)
    {
        if (client.Client.RemoteEndPoint is null)
            return null;

        return (IPEndPoint)client.Client.RemoteEndPoint;
    }

    public static byte[] ToNetworkBytes(this int integer) =>
        BitConverter.GetBytes(IPAddress.HostToNetworkOrder(integer));

    public static byte[] ToNetworkBytes(this long integer) =>
        BitConverter.GetBytes(IPAddress.HostToNetworkOrder(integer));
}