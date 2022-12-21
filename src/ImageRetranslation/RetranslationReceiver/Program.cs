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
    int leftDataOffset = 0;
    bool newMessage = false;

    while (!token.IsCancellationRequested)
    {
        FileIterInfo fileIterInfo;
        using MemoryStream memoryStream = new();

        try
        {
            Debug.WriteLine($"Left data offset: {leftDataOffset}");
            fileIterInfo = await GetFileAndIterationInfo(!newMessage ? memory : memory[leftDataOffset..], netStream, memoryStream, token, newMessage);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        Debug.WriteLine($"Filename: {fileIterInfo.FileName} ({fileIterInfo.NameLength} bytes)");
        Debug.WriteLine($"Data length: {fileIterInfo.DataLength}");

        int totalRead = fileIterInfo.TotalRead;
        int leftToRead = fileIterInfo.LeftToRead;
        int read = 0;
        int toWrite = 0;
        var iterCounter = 0;

        while (totalRead < fileIterInfo.DataLength)
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

            totalRead += read;
            leftToRead -= read;
        }

        Memory<byte> hostData;

        if (toWrite < read)
        {
            leftDataOffset = toWrite + 8;
            hostData = memory[toWrite..leftDataOffset];
            newMessage = true;
        }
        else
        {
            hostData = memory[..8];
            await netStream.ReadExactlyAsync(hostData);
            newMessage = false;
        }

        var senderIp = new IPAddress(hostData[..4].Span);
        var portBytes = hostData[4..8].ToArray();
        var senderPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(portBytes));

        Debug.WriteLine($"IPAddress: {senderIp}");
        Debug.WriteLine($"Port: {senderPort}");
        Debug.WriteLine($"Network port bytes: {string.Join(' ', portBytes)}");

        var sender = new IPEndPoint(senderIp, senderPort);
        yield return new NetworkFile(fileIterInfo.FileName, memoryStream.ToArray(), sender);

        Console.WriteLine($"Received total: {totalRead + 4} bytes form sender {sender}");
    }
}

static async ValueTask<FileIterInfo> GetFileAndIterationInfo(Memory<byte> memory, NetworkStream nStream, MemoryStream mStream, CancellationToken token, bool newMessage = false)
{
    int nameLength;
    string fileName;
    int dataLength;

    if (!newMessage)
    {
        nameLength = await nStream.ReadInt(memory, token);
        var nameData = memory[..nameLength];
        await nStream.ReadExactlyAsync(nameData);
        fileName = Encoding.UTF8.GetString(nameData.Span);
        dataLength = await nStream.ReadInt(memory, token);

        return new FileIterInfo
        {
            DataLength = dataLength,
            FileName = fileName,
            NameLength = nameLength,
            LeftToRead = dataLength
        };
    }

    //assume newly arrived message data length is always > (4 + nameLength + 4)
    //control via buffer size?

    nameLength = memory.Span.GetHostOrderInt();
    fileName = Encoding.UTF8.GetString(memory[4..(nameLength + 4)].Span);
    dataLength = memory[(nameLength + 4)..].Span.GetHostOrderInt();

    var leftData = memory[(nameLength + 8)..];

    if (!leftData.IsEmpty)
        await mStream.WriteAsync(leftData, token);

    return new FileIterInfo
    {
        FileName = fileName,
        NameLength = nameLength,
        DataLength = dataLength,
        TotalRead = leftData.Length,
        LeftToRead = dataLength - leftData.Length
    };
}

struct FileIterInfo
{
    public string FileName { get; set; }

    public int NameLength { get; set; }
    public int DataLength { get; set; }

    public int LeftToRead { get; set; }

    public int TotalRead { get; set; }
}

