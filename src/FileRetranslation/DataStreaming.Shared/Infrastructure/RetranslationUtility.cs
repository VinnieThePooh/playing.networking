using System.Net.Sockets;
using DataStreaming.Common.Extensions;
using ImageRetranslationShared.Models;

namespace ImageRetranslationShared.Infrastructure;

public static class RetranslationUtility
{
    //preamble should actually be of a fixed size, but ok now
    /// <summary>
    ///  Read variable length message preamble
    /// </summary>
    /// <param name="newMessage"></param>
    /// <param name="stream"></param>
    /// <param name="preambulaReserveSize">512 bytes - MAX for dynamic name (up to 512 latin characters or 254 cyrillic). Plus extra 8 for dataLength</param>
    /// <returns></returns>
    public static PreambleReadResult ReadPreamble(Memory<byte> newMessage, NetworkStream stream, int preambulaReserveSize = 520)
    {
        PreambleReadResult result = new();
        Memory<byte> reserve;

        //4 + n + 8 - required bytes to read preamble
        //single read operation
        if (newMessage.Length < preambulaReserveSize)
        {
            reserve = new Memory<byte>(new byte[preambulaReserveSize]);
            newMessage.CopyTo(reserve);
            var free = reserve[newMessage.Length..];
            //assume we read free.Length always or 0 - according to protocol
            var read = stream.Read(free.Span);
            if (read == 0)
                return PreambleReadResult.DisconnectedPrematurely;
        }
        else
        {
            reserve = newMessage;
            result.ReadFromBufferOnly = true;
        }

        var nameLength = reserve.Span.GetHostOrderInt();

        result.NameLength = nameLength;
        result.NameBytes = reserve[4..(nameLength + 4)].ToArray();
        result.DataLength = reserve[(nameLength + 4)..].Span.GetHostOrderInt64();
        result.DataLeft = reserve[(nameLength + 12)..];
        return result;
    }
}