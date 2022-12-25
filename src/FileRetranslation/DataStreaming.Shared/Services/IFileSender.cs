namespace DataStreaming.Services;

public interface IFileSender : IAsyncDisposable
{
    Task SendImages(string[] filePaths, CancellationToken token);
}