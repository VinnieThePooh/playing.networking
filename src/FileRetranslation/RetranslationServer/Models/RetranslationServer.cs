using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

    public RetranslationServer(FileRetranslationSettings settings)
    {
        RetranslationSettings = settings ?? throw new ArgumentNullException(nameof(settings));
    }
    public FileRetranslationSettings RetranslationSettings { get; }

    public Dictionary<IPEndPoint, ClientProxy> ClientProxies { get; } = new();

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
            var clientProxy = CreateClientProxy(client, protoFactory);
            ClientProxies.Add(clientProxy.EndPoint, clientProxy);
            _ = clientProxy.DoCommunication(_cts.Token);
        }

        return true;
    }

    private ClientProxy CreateClientProxy(TcpClient client, IProtocolFactory factory)
    {
        var proto = (RetranslationServerProto)factory.CreateServerProtocol();
        proto.RetranslationSettings = RetranslationSettings;
        proto.ImageUploaded += OnImageUploaded;

        var ep = client.GetRemoteEndpoint()!;
        var clientProxy = new ClientProxy(ep, proto);
        clientProxy.SetClient(client);
        return clientProxy;
    }

    private async void OnImageUploaded(object? sender, ImageUploadedEventArgs e)
    {
        var dataLengthBytes = ((long)e.ImageData.Length).ToNetworkBytes();
        var nameLengthBytes = e.ImageNameData.Length.ToNetworkBytes();
        var addressBytes = e.Uploader.Address.GetAddressBytes();
        var portBytes = e.Uploader.Port.ToNetworkBytes();

        int mNumber = e.MessageOrderNumber;
        int bSize = e.BatchSize;

        // if (Directory.Exists("images"))
        //     Directory.CreateDirectory("images");
        //
        // await using (var fs = File.Create(Path.Combine("images", Encoding.UTF8.GetString(e.ImageNameData))))
        //     await fs.WriteAsync(e.ImageData);

        var tasks = ClientProxies.Values
            .Where(p => p.ClientType == ClientType.Receiver)
            .Select(async receiver =>
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
            Debug.WriteLine($"[RetranslationServer]: Sent file '{Encoding.UTF8.GetString(e.ImageNameData)}' to all");
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
}