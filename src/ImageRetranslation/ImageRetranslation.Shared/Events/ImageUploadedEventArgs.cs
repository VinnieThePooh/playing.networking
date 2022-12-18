using System.Net;

namespace ImageRetranslationShared.Events;

public class ImageUploadedEventArgs
{
    public ImageUploadedEventArgs(IPEndPoint uploader, byte[] imageData)
    {
        Uploader = uploader;
        ImageData = imageData;
    }

    public IPEndPoint Uploader { get; }

    public byte[] ImageData { get; }
}