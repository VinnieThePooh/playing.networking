using System.Net;

namespace DataStreaming.Common.Events;

public class ImageUploadedEventArgs
{
    public IPEndPoint Uploader { get; init; }

    public byte[] ImageData { get; init; }

    public byte[] ImageNameData { get; init; }

    //number of messages within the batch
    public int BatchSize { get; init; }

    //within the batch of messages
    public int MessageOrderNumber { get; init; }
}