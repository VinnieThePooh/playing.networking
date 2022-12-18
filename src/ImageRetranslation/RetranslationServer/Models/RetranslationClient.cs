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
        Protocol.ImageUploaded += OnImageUploaded;

        return Task.Run(() => Protocol.DoCommunication(Client, token), token);
    }

    private async void OnImageUploaded(object? sender, ImageUploadedEventArgs e)
    {
        var len = IPAddress.HostToNetworkOrder(e.ImageData.Length);
        var lenBytes = BitConverter.GetBytes(len);
        var addressBytes = e.Uploader.Address.GetAddressBytes();
        var portBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(e.Uploader.Port));

        var tasks = receiversDictionary.Values.Select(async receiver =>
        {
            var stream = receiver.Client.GetStream();
            await stream.WriteAsync(lenBytes);
            await stream.WriteAsync(e.ImageData);
            await stream.WriteAsync(addressBytes);
            await stream.WriteAsync(portBytes);
            await stream.FlushAsync();
        });

        try
        {
            //todo: add TaskExt.WhenAll - this one catch only first exception
            await Task.WhenAll(tasks);
        }
        catch (AggregateException exception)
        {
            Console.WriteLine(exception);
        }
    }

    private void OnClientTypeDetected(object? sender, ClientTypeDetectedEventArgs e)
    {
        Console.WriteLine($"[{nameof(RetranslationServer)}]: New client type detected: {e.Type} ({EndPoint})");

        ClientType = e.Type;
        if (ClientType == ClientType.Receiver)
            receiversDictionary.TryAdd(EndPoint, this);

        Protocol.ClientTypeDetected -= OnClientTypeDetected;
    }
}