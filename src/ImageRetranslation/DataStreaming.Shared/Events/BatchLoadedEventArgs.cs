namespace DataStreaming.Common.Events;

public class BatchLoadedEventArgs : EventArgs
{
    public BatchLoadedEventArgs(IList<string> fileNames)
    {
        if (fileNames == null) throw new ArgumentNullException(nameof(fileNames));
        FileNames = fileNames.AsReadOnly();
    }
    public IReadOnlyCollection<string> FileNames { get; }

    //number of files within the batch
    public int BatchSize => FileNames.Count;
}