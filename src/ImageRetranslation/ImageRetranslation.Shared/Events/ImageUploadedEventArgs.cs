using System.Net;

namespace ImageRetranslationShared.Events;

public class ImageUploadedEventArgs
{
    public IPEndPoint Uploader { get; init; }

    public byte[] ImageData { get; init; }

    public byte[] ImageNameData { get; init; }

    //total count of sequential images came from receiver
    public int BatchSize { get; init; }

    //event order number within the batch of images
    public int EventOrderNumber { get; init; }
}