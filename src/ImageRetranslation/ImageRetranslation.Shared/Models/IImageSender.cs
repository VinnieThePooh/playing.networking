namespace ImageRetranslationShared.Models;

public interface IImageSender
{
    Task SendImages(string[] images, CancellationToken token);
}