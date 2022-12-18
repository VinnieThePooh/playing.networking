using System.Net;
using System.Net.Sockets;
using ImageRetranslationShared.Commands;
using ImageRetranslationShared.Settings;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var settings = config.GetSection(ImageRetranslationSettings.SectionName).Get<ImageRetranslationSettings>();

Console.WriteLine($"Retranslation host: {settings}");
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    cts.Cancel();
};

using var clientSender = new TcpClient();

Console.Write("Connecting to host...");
await clientSender.ConnectAsync(IPAddress.Parse(settings.Host), settings.Port, cts.Token);
Console.Write("connected");

var netStream = clientSender.GetStream();

Console.Write("Sending out client type attribute (ClientType = Receiver)...");
netStream.WriteByte((byte)ClientType.Receiver);
await netStream.FlushAsync(cts.Token);
Console.WriteLine("completed");

Console.Write("Awaiting for image data...");
await AwaitImageData(netStream, cts.Token);

async Task AwaitImageData(NetworkStream netStream, CancellationToken token)
{
    var memory = new Memory<byte>(new byte[1024]);

    while (!token.IsCancellationRequested)
    {
        netStream.ReadExactly(memory[..4].Span);
        var len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(memory[..4].Span));

        int totalRead = 0;
        var memoryStream = new MemoryStream();

        while (totalRead < len)
        {
            var read = await netStream.ReadAsync(memory, token);

            if (read == 0)
            {
                Console.WriteLine("Server disconnected prematurely");
                netStream.Close();

                return;
            }

            await memoryStream.WriteAsync(memory[..read]);
            totalRead += read;
        }

        var filename = $"{Path.GetRandomFileName()}.jpg";
        await using (var fs = File.Create($"{filename}"))
            await memoryStream.CopyToAsync(fs);
        await memoryStream.DisposeAsync();

        Console.WriteLine($"Created file '{filename}' with image data.");
    }
}


