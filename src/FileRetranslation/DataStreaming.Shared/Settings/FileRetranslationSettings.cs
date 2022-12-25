namespace DataStreaming.Common.Settings;

public class FileRetranslationSettings
{
    public const string SectionName = "FileRetranslation";

    public string Host { get; set; }
    public int Port { get; set; }

    public uint BufferSize { get; set; } = 1024 * 8;

    public override string ToString() => $"{Host}:{Port}";

    public static uint FallbackBufferSize => 1024 * 8;
}