namespace DataStreaming.Services;

public interface IFileSender : IAsyncDisposable
{
    Task SendFiles(IEnumerable<string> filePaths, CancellationToken token);
}