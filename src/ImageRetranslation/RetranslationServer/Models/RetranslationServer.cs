using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DataStreaming.Common.Constants;
using DataStreaming.Common.Events;
using DataStreaming.Common.Extensions;
using DataStreaming.Common.Protocols;
using DataStreaming.Common.Protocols.Factories;
using DataStreaming.Common.Settings;

namespace Retranslation;

public class RetranslationServer : IRetranslationServer
{
    private CancellationTokenSource _cts;
    public FileRetranslationSettings RetranslationSettings { get; }

    public RetranslationServer(FileRetranslationSettings settings)
    {
        RetranslationSettings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<bool> Start()
    {
        if (_cts is not null)
                return false;

        _cts = new CancellationTokenSource();
        IProtocolFactory protoFactory = ImageRetranslationProtocolFactory.Create();

        var listener = new TcpListener(IPAddress.Any, RetranslationSettings.Port);
        listener.Start();

        Console.WriteLine($"[{nameof(RetranslationServer)}]: Listening at {IPAddress.Any}:{RetranslationSettings.Port}");

        while (!_cts.Token.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync(_cts.Token);
            var proto = (RetranslationServerProto)protoFactory.CreateServerProtocol();
            proto.RetranslationSettings = RetranslationSettings;
            proto.ImageUploaded += OnImageUploaded;

            var clientProxy = new ClientProxy(client.GetRemoteEndpoint()!, proto);
            clientProxy.SetClient(client);
            _ = clientProxy.DoCommunication(_cts.Token);
        }

        return true;
    }

    private async void OnImageUploaded(object? sender, ImageUploadedEventArgs e)
    {
        var dataLengthBytes = ((long)e.ImageData.Length).ToNetworkBytes();
        var nameLengthBytes = e.ImageNameData.Length.ToNetworkBytes();
        var addressBytes = e.Uploader.Address.GetAddressBytes();
        var portBytes = e.Uploader.Port.ToNetworkBytes();

        int mNumber = e.MessageOrderNumber;
        int bSize = e.BatchSize;

        var tasks = ClientProxies.Values.Where(p => p.ClientType == ClientType.Receiver).Select(async receiver =>
        {
            var stream = receiver.Client.GetStream();
            stream.Write(nameLengthBytes);
            stream.Write(e.ImageNameData);
            stream.Write(dataLengthBytes);
            await stream.WriteAsync(e.ImageData);
            stream.Write(addressBytes);
            stream.Write(portBytes);
            if (mNumber == bSize)
                stream.Write(Prologs.EndOfBatch.ToNetworkBytes());
            await stream.FlushAsync();
        });

        try
        {
            await tasks.WhenAll();
        }
        catch (AggregateException exception)
        {
            Console.WriteLine(exception);
        }
    }

    public Task<bool> Stop()
    {
        if (_cts is null)
            return Task.FromResult(false);

        _cts.Cancel();
        _cts.Dispose();
        _cts = null;

        return Task.FromResult(true);
    }

    public ConcurrentDictionary<IPEndPoint, ClientProxy> ClientProxies { get; } = new();
}