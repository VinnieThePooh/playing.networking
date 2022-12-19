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
Console.WriteLine("connected");

var netStream = clientSender.GetStream();

Console.Write("Sending out client type attribute (ClientType = Receiver)...");
netStream.WriteByte((byte)ClientType.Receiver);
await netStream.FlushAsync(cts.Token);
Console.WriteLine("completed");

Console.WriteLine("Awaiting for image data...");
await AwaitImageData(netStream, cts.Token);

async Task AwaitImageData(NetworkStream netStream, CancellationToken token)
{
    var memory = new Memory<byte>(new byte[1024]);

    while (!token.IsCancellationRequested)
    {
        netStream.ReadExactly(memory[..4].Span);
        var len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(memory[..4].Span));

        int totalRead = 0;
        int leftToRead = len;
        int toWrite = 0;

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

            toWrite = Math.Min(read, leftToRead);
            await memoryStream.WriteAsync(memory[..toWrite], token);
            totalRead += read;
            leftToRead -= read;
        }

        Memory<byte> hostData;

        //todo: ??
        if (totalRead >= len + 8)
            hostData = memory[toWrite..(toWrite + 8)];
        else
        {
            netStream.ReadExactly(memory[toWrite..(toWrite + (len + 8 - totalRead))].Span);
            hostData = memory[toWrite..(toWrite + 8)];
        }

        var senderIp = new IPAddress(hostData[..4].Span);

        //??
        var portBytes = hostData[4..8].ToArray();
        var senderPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(portBytes));
        var sender = new IPEndPoint(senderIp, senderPort);

        var filename = $"{Path.GetRandomFileName()}.jpg";

        await using (var fs = File.Create($"{filename}"))
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.CopyTo(fs);
            await memoryStream.DisposeAsync();
        }

        Console.WriteLine($"Received total: {totalRead + 4} bytes form sender {sender}");
        Console.WriteLine($"Created file '{filename}' with image data.");
    }
}


