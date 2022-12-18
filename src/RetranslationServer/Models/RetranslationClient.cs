using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using ImageRetranslationShared.Commands;
using ImageRetranslationShared.Events;
using ImageRetranslationShared.Protocols;

namespace RetranslationServer.Models;

public class RetranslationClient
{
    private readonly ConcurrentDictionary<IPEndPoint, RetranslationClient> receiversDictionary;

    public RetranslationClient(IPEndPoint endPoint, RetranslationServerProto protocol, ConcurrentDictionary<IPEndPoint, RetranslationClient> receiversDictionary)
    {
        this.receiversDictionary = receiversDictionary;
        EndPoint = endPoint;
        Protocol = protocol;
    }

    public void SetClient(TcpClient client)
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
        Console.WriteLine($"New client type detected: {e.Type} ({EndPoint})");

        ClientType = e.Type;
        if (ClientType == ClientType.Receiver)
            receiversDictionary.TryAdd(EndPoint, this);

        Protocol.ClientTypeDetected -= OnClientTypeDetected;
    }
}