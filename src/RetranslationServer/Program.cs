using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using ImageRetranslationShared.Protocols;
using ImageRetranslationShared.Protocols.Factories;
using ImageRetranslationShared.Settings;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var settings = config.GetSection(ImageRetranslationSettings.SectionName).Get<ImageRetranslationSettings>();

var listener = new TcpListener(IPAddress.Any, settings.Port);
listener.Start();

IProtocolFactory protoFactory = ImageRetranslationProtocolFactory.Create();

Console.WriteLine($"Listening at {IPAddress.Any}:{settings.Port}");

var cts = new CancellationTokenSource();

//todo: need common state storing all the clients
// pass to proto OR other abstraction-wrapper?
// ConcurrentDictionary<IPEndPoint, >

while (!cts.Token.IsCancellationRequested)
{
    var client = await listener.AcceptTcpClientAsync();
    var proto = (RetranslationServerProto)protoFactory.CreateServerProtocol();
    Task.Run(() => proto.DoCommunication(client, cts.Token), cts.Token);
}