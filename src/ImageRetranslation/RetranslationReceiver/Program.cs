using System.Net;
using System.Net.Sockets;
using System.Text;
using ImageRetranslationShared.Commands;
using ImageRetranslationShared.Extensions;
using ImageRetranslationShared.Settings;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var settings = config.GetSection(ImageRetranslationSettings.SectionName).Get<ImageRetranslationSettings>();

const string ImageFolder = "images";

if (!Directory.Exists(ImageFolder))
    Directory.CreateDirectory(ImageFolder);

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

//todo: refactor
async Task AwaitImageData(NetworkStream netStream, CancellationToken token)
{
    var memory = new Memory<byte>(new byte[1024]);

    while (!token.IsCancellationRequested)
    {
        var nameLength = await netStream.ReadInt(memory, token);

        var nameData = memory[..nameLength];
        await netStream.ReadExactlyAsync(nameData);
        var fileName = Encoding.UTF8.GetString(nameData.Span);
        var dataLength = await netStream.ReadInt(memory, token);

        int totalRead = 0;
        int leftToRead = dataLength;
        int toWrite = 0;

        var memoryStream = new MemoryStream();

        while (totalRead < dataLength)
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
        if (totalRead >= nameLength + 8)
            hostData = memory[toWrite..(toWrite + 8)];
        else
        {
            netStream.ReadExactly(memory[toWrite..(toWrite + (nameLength + 8 - totalRead))].Span);
            hostData = memory[toWrite..(toWrite + 8)];
        }

        var senderIp = new IPAddress(hostData[..4].Span);

        //??
        var portBytes = hostData[4..8].ToArray();
        var senderPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(portBytes));
        var sender = new IPEndPoint(senderIp, senderPort);

        //todo: check for existing file names
        await using (var fs = File.Create(Path.Combine(ImageFolder, fileName)))
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.CopyTo(fs);
            await memoryStream.DisposeAsync();
        }

        Console.WriteLine($"Received total: {totalRead + 4} bytes form sender {sender}");
        Console.WriteLine($"Created file '{fileName}' with image data.");
    }
}


