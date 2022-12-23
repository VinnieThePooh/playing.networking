using System.Net;

namespace DataStreaming.Common.Events;

public class BatchLoadedEventArgs : EventArgs
{
    public BatchLoadedEventArgs(IList<string> fileNames, IPEndPoint origin)
    {
        if (fileNames == null) throw new ArgumentNullException(nameof(fileNames));

        Origin = origin;
        FileNames = fileNames.AsReadOnly();
    }
    public IReadOnlyCollection<string> FileNames { get; }

    //number of files within the batch
    public int BatchSize => FileNames.Count;

    public IPEndPoint Origin { get; }
}