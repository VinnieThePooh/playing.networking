namespace DataStreaming.Common.Settings;

public class ImageRetranslationSettings
{
    public const string SectionName = "ImageRetranslation";

    public string Host { get; set; }
    public int Port { get; set; }

    public override string ToString() => $"{Host}:{Port}";
}