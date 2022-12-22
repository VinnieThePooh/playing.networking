using System.Net;

namespace DataStreaming.Common.Events;

public class ImageUploadedEventArgs
{
    public IPEndPoint Uploader { get; init; }

    public byte[] ImageData { get; init; }

    public byte[] ImageNameData { get; init; }

    //total count of sequential images came from receiver
    public int BatchSize { get; init; }

    //within the batch of images
    public int MessageOrderNumber { get; init; }
}