using DataStreaming.Services.FileTransfer;
using DataStreaming.Services.Interfaces;
using DataStreaming.Settings;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var settings = config.GetSection(FileRetranslationSettings.SectionName).Get<FileRetranslationSettings>()!;

const string ImageFolder = "images";

if (!Directory.Exists(ImageFolder))
    Directory.CreateDirectory(ImageFolder);

Console.WriteLine($"Retranslation host: {settings}");
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) => cts.Cancel();

try
{
    await using IFileReceiver receiver = new FileReceiver(settings);
    receiver.BatchLoaded += (_, e) => Console.WriteLine($"[{e.Origin}]: batch ({e.FileNames.Count} files) completed transfer.");

    await foreach (var file in receiver.AwaitFiles(cts.Token))
    {
        await using var fs = File.Create(Path.Combine(ImageFolder, file.FileName));
        await fs.WriteAsync(file.Data, cts.Token);
        Console.WriteLine($"Created file '{file.FileName}' (From {file.Origin})");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine($"AwaitImageData was cancelled");
}
catch (Exception e)
{
    Console.WriteLine(e);
}
Console.ReadLine();
