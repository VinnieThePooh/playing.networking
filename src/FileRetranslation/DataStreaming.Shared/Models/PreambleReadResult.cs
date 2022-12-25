namespace ImageRetranslationShared.Models;

public struct PreambleReadResult
{
    public bool ReadFromBufferOnly { get; set; }

    public bool IsDisconnectedPrematurely { get; set; }

    public int NameLength { get; set; }

    public byte[] NameBytes { get; set; }

    public Memory<byte> DataLeft { get; set; }

    public long DataLength { get; set; }

    public static PreambleReadResult DisconnectedPrematurely => new() { IsDisconnectedPrematurely = true };
}