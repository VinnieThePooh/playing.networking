using System.Net;
using System.Net.Sockets;
using System.Text;
using DataStreaming.Common.Constants;
using DataStreaming.Common.Settings;
using DataStreaming.Common.Extensions;

namespace DataStreaming.Services;

public class FileSender : IFileSender
{
    private readonly FileRetranslationSettings settings;
    private TcpClient? tcpClient;

    public FileSender(FileRetranslationSettings settings)
    {
        this.settings = settings;
    }

    public async Task SendFiles(IEnumerable<string> filePaths, CancellationToken token)
    {
        tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(IPAddress.Parse(settings.Host), settings.Port, token);

        var networkStream = tcpClient.GetStream();

        Console.Write("Sending client type attribute (ClientType = Sender) and number of files to be passed...");
        networkStream.WriteByte((byte)ClientType.Sender);
        Console.WriteLine("completed");

        networkStream.Write(filePaths.Count().ToNetworkBytes());
        await networkStream.FlushAsync(token);

        Console.Write("Sending stream data to server...");
        foreach (var path in filePaths)
            await SendFile(networkStream, path, token);

        Console.WriteLine("succeeded.");
        tcpClient.Close();
    }

    private async Task SendFile(NetworkStream networkStream, string filePath, CancellationToken token)
    {
        var fname = Path.GetFileName(filePath);
        await using var fs = File.Open(filePath, FileMode.Open);

        networkStream.Write(fname.GetUtf8BytesCount().ToNetworkBytes());
        networkStream.Write(Encoding.UTF8.GetBytes(fname));
        networkStream.Write(fs.Length.ToNetworkBytes());
        await fs.CopyToAsync(networkStream, token);
    }

    public ValueTask DisposeAsync()
    {
        if (tcpClient is null)
            return ValueTask.CompletedTask;

        tcpClient.Close();
        tcpClient = null;

        return ValueTask.CompletedTask;
    }
}