using System.Net;
using System.Net.Sockets;
using DataStreaming.Common.Constants;
using DataStreaming.Common.Events;
using DataStreaming.Common.Protocols;

namespace Retranslation;

public class ClientProxy
{
    internal ClientProxy(IPEndPoint endPoint, RetranslationServerProto protocol)
    {
        EndPoint = endPoint;
        Protocol = protocol;
    }

    internal void SetClient(TcpClient client)
    {
        Client = client;
    }

    public TcpClient Client { get; private set; }

    public IPEndPoint EndPoint { get; }

    public ClientType ClientType { get; private set; }

    public RetranslationServerProto Protocol { get; }

    public Task DoCommunication(CancellationToken token)
    {
        if (Client is null)
            throw new InvalidOperationException("Client was not initialized");

        Protocol.ClientTypeDetected += OnClientTypeDetected;
        return Task.Run(() => Protocol.DoCommunication(Client, token), token);
    }

    private void OnClientTypeDetected(object? sender, ClientTypeDetectedEventArgs e)
    {
        Console.WriteLine($"[{nameof(RetranslationServer)}]: New client type detected: {e.Type} ({EndPoint})");
        ClientType = e.Type;
        Protocol.ClientTypeDetected -= OnClientTypeDetected;
    }
}