using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DataStreaming.Common.Extensions;

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

    public static int GetHostOrderInt(this Span<byte> memory) =>
        IPAddress.NetworkToHostOrder(BitConverter.ToInt32(memory));

    public static int GetHostOrderInt(this byte[] memory) =>
        IPAddress.NetworkToHostOrder(BitConverter.ToInt32(memory));

    public static long GetHostOrderInt64(this Span<byte> memory) =>
        IPAddress.NetworkToHostOrder(BitConverter.ToInt64(memory));

    public static async Task<int> ReadInt(this NetworkStream stream, byte[] buffer, CancellationToken token = default)
    {
        await stream.ReadExactlyAsync(buffer, 0, 4, token);
        return buffer.GetHostOrderInt();
    }

    public static async Task<int> ReadInt(this NetworkStream stream, Memory<byte> buffer, CancellationToken token = default)
    {
        var properSlice = buffer[..4];
        await stream.ReadExactlyAsync(properSlice, token);
        return properSlice.Span.GetHostOrderInt();
    }

    public static async Task<long> ReadLong(this NetworkStream stream, Memory<byte> buffer, CancellationToken token = default)
    {
        var properSlice = buffer[..8];
        await stream.ReadExactlyAsync(properSlice, token);
        return properSlice.Span.GetHostOrderInt64();
    }

    public static int GetUtf8BytesCount(this string str) => Encoding.UTF8.GetByteCount(str);
}