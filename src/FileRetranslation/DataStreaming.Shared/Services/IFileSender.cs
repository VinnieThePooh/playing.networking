namespace DataStreaming.Services;

public interface IFileSender : IAsyncDisposable
{
    Task SendImages(IEnumerable<string> filePaths, CancellationToken token);
}