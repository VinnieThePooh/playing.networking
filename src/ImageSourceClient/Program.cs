using System.Net;
using System.Net.Sockets;
using ImageRetranslationShared.Settings;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var settings = config.GetSection(ImageRetranslationSettings.SectionName).Get<ImageRetranslationSettings>();

Console.WriteLine($"Retranslation host: {settings}");

var fileName = "bull_and_basketball.jpg";

using var clientSender = new TcpClient();
await clientSender.ConnectAsync(IPAddress.Parse(settings.Host), settings.Port);

var networkStream = clientSender.GetStream();

Console.WriteLine("Sending image stream data to server...");

await using (var fileStream = File.Open(fileName, FileMode.Open))
{
    var length = IPAddress.HostToNetworkOrder(fileStream.Length);
    await networkStream.WriteAsync(BitConverter.GetBytes(length));
    await fileStream.CopyToAsync(networkStream);
}

Console.WriteLine("succeeded.");
clientSender.Close();


