using System.Net;
using System.Net.Sockets;
using System.Text;
using ImageRetranslationShared.Commands;
using ImageRetranslationShared.Extensions;
using ImageRetranslationShared.Settings;

namespace ImageRetranslationShared.Models;

public class ImageSender : IImageSender, IAsyncDisposable
{
    private readonly ImageRetranslationSettings settings;
    private TcpClient tcpClient;

    public ImageSender(ImageRetranslationSettings settings)
    {
        this.settings = settings;
    }

    public async Task SendImages(string[] images, CancellationToken token)
    {
        tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(IPAddress.Parse(settings.Host), settings.Port, token);

        var networkStream = tcpClient.GetStream();

        Console.Write("Sending client type attribute (ClientType = Sender) and number of files to be passed...");
        networkStream.WriteByte((byte)ClientType.Sender);
        Console.WriteLine("completed");

        networkStream.Write(images.Length.ToNetworkBytes());
        await networkStream.FlushAsync(token);

        Console.Write("Sending image stream data to server...");
        for (int i = 0; i < images.Length; ++i)
            await SendImage(networkStream, images[i], token);
        Console.WriteLine("succeeded.");
        tcpClient.Close();
    }

    private async Task SendImage(NetworkStream networkStream, string filePath, CancellationToken token)
    {
        var fname = Path.GetFileName(filePath);
        await using var fs = File.Open(filePath, FileMode.Open);

        networkStream.Write(fname.Length.ToNetworkBytes());
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