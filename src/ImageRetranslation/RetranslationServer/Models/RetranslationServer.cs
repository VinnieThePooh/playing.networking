using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using ImageRetranslationShared.Extensions;
using ImageRetranslationShared.Protocols;
using ImageRetranslationShared.Protocols.Factories;
using ImageRetranslationShared.Settings;

namespace Retranslation;

public class RetranslationServer : IRetranslationServer
{
    private CancellationTokenSource _cts;
    public ImageRetranslationSettings Settings { get; }

    public RetranslationServer(ImageRetranslationSettings settings)
    {
        Settings = settings;
    }

    public async Task<bool> Start()
    {
        if (_cts is not null)
                return false;

        _cts = new CancellationTokenSource();
        IProtocolFactory protoFactory = ImageRetranslationProtocolFactory.Create();

        var listener = new TcpListener(IPAddress.Any, Settings.Port);
        listener.Start();

        Console.WriteLine($"[{nameof(RetranslationServer)}]: Listening at {IPAddress.Any}:{Settings.Port}");

        while (!_cts.Token.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync(_cts.Token);
            var proto = (RetranslationServerProto)protoFactory.CreateServerProtocol();
            var clientProxy = new ClientProxy(client.GetRemoteEndpoint()!, proto, ReceiversDictionary);
            clientProxy.SetClient(client);
            //fire and forget
            _ = clientProxy.DoCommunication(_cts.Token);
        }

        return true;
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

    public ConcurrentDictionary<IPEndPoint, ClientProxy> ReceiversDictionary { get; } = new();
}