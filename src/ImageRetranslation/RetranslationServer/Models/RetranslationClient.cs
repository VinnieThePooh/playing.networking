using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using ImageRetranslationShared.Commands;
using ImageRetranslationShared.Events;
using ImageRetranslationShared.Extensions;
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
        var dataLengthBytes = e.ImageData.Length.ToNetworkBytes();
        var nameLengthBytes = e.ImageNameData.Length.ToNetworkBytes();
        var addressBytes = e.Uploader.Address.GetAddressBytes();
        var portBytes = e.Uploader.Port.ToNetworkBytes();

        var tasks = receiversDictionary.Values.Select(async receiver =>
        {
            var stream = receiver.Client.GetStream();
            await stream.WriteAsync(nameLengthBytes);
            stream.Write(e.ImageNameData);
            stream.Write(dataLengthBytes);
            stream.Write(e.ImageData);
            stream.Write(addressBytes);
            stream.Write(portBytes);
            await stream.FlushAsync();
        });

        try
        {
            await tasks.WhenAll();
            if (e.EventOrderNumber == e.BatchSize)
                Protocol.ImageUploaded -= OnImageUploaded;
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