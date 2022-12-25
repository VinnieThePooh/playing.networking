using System.Net;

namespace ImageRetranslationShared.Models;

public class NetworkFile
{
    public NetworkFile(string fileName, byte[] data, IPEndPoint origin)
    {
        FileName = fileName;
        Data = data;
        Origin = origin;
    }

    public string FileName { get; }

    public byte[] Data { get; }

    public IPEndPoint Origin { get; }
}