using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ImageRetranslationShared.Commands;
using ImageRetranslationShared.Extensions;
using ImageRetranslationShared.Settings;
using Microsoft.Extensions.Configuration;
using RetranslationReceiver.Models;

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
    await foreach (var file in AwaitImageData(netStream, cts.Token))
    {
        //todo: check for existing file names
        await using var fs = File.Create(Path.Combine(ImageFolder, file.FileName));
        await fs.WriteAsync(file.Data, cts.Token);
        Console.WriteLine($"Created file '{file.FileName}' with image data. (Origin: {file.Origin})");
    }
}
catch (Exception e)
{
    Console.WriteLine(e);
}

Console.ReadLine();

//todo: refactor
async IAsyncEnumerable<NetworkFile> AwaitImageData(NetworkStream netStream, CancellationToken token)
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
        int read = 0;
        int toWrite = 0;
        int iterCounter = 0;

        using var memoryStream = new MemoryStream();

        while (totalRead < dataLength)
        {
            read = await netStream.ReadAsync(memory, token);
            Debug.IndentLevel = 2;

            if (read == 0)
            {
                Console.WriteLine("Server disconnected prematurely");
                netStream.Close();
                yield break;
            }

            toWrite = Math.Min(read, leftToRead);
            await memoryStream.WriteAsync(memory[..toWrite], token);

            Debug.WriteLine($"{++iterCounter}.Read - {read}; Write - {toWrite} bytes");

            if (read > toWrite)
                Debugger.Break();

            totalRead += read;
            leftToRead -= read;
        }

        Memory<byte> hostData;

        if (toWrite < read)
            hostData = memory[toWrite..(toWrite + 8)];
        else
        {
            hostData = memory[..8];
            await netStream.ReadExactlyAsync(hostData);
        }

        var senderIp = new IPAddress(hostData[..4].Span);
        var portBytes = hostData[4..8].ToArray();
        var senderPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(portBytes));

        Debug.WriteLine($"IPAddress: {senderIp}");
        Debug.WriteLine($"Port: {senderPort}");
        Debug.WriteLine($"Network port bytes: {string.Join(' ', portBytes)}");

        var sender = new IPEndPoint(senderIp, senderPort);
        yield return new NetworkFile(fileName, memoryStream.ToArray(), sender);

        Console.WriteLine($"Received total: {totalRead + 4} bytes form sender {sender}");
    }
}

