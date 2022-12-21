using System.Diagnostics;
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

try
{
    await AwaitImageData(netStream, cts.Token);
}
catch (Exception e)
{
    Console.WriteLine(e);
}

Console.ReadLine();

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

        Debug.WriteLine($"Filename: {fileName} ({nameLength} bytes)");
        Debug.WriteLine($"Data length: {dataLength}");

        int totalRead = 0;
        int leftToRead = dataLength;
        int toWrite = 0;
        int iterCounter = 0;

        var memoryStream = new MemoryStream();

        while (totalRead < dataLength)
        {
            var read = await netStream.ReadAsync(memory, token);
            Debug.IndentLevel = 2;
            Debug.WriteLine($"{++iterCounter}.Has read: {read} bytes");

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

        // what would be if sender and receiver app buffers are of different size?
        netStream.ReadExactly(memory[..8].Span);

        //todo: ??
        // if (totalRead >= dataLength + 8)
        //     hostData = memory[toWrite..(toWrite + 8)];
        // else
        // {
        //     netStream.ReadExactly(memory[toWrite..(toWrite + dataLength + 8 - totalRead)].Span);
        //     hostData = memory[toWrite..(toWrite + 8)];
        // }

        var senderIp = new IPAddress(memory[..4].Span);
        var portBytes = memory[4..8].ToArray();
        var senderPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(portBytes));

        Debug.WriteLine($"IPAddress: {senderIp}");
        Debug.WriteLine($"Port: {senderIp}");
        Debug.WriteLine($"Network port bytes: {string.Join(' ', portBytes)}");

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

