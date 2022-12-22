namespace ImageRetranslationShared.Models;
/// <summary>
/// Commonly used structure for assembling streaming messages on the receiver side
/// </summary>
public struct StreamingInfo
{
    public StreamingInfo()
    {
        NewMessageData = Memory<byte>.Empty;
    }

    public int MessageOrderNumber { get; set; }

    public int BatchSize { get; set; }

    public Memory<byte> Buffer { get; set; }

    public Memory<byte> NewMessageData { get; set; }

    public bool IsDisconnectedPrematurely { get; init; }

    public bool IsEndOfBatch { get; set; }

    public static StreamingInfo DisconnectedPrematurely { get; } = new() { IsDisconnectedPrematurely = true };
}