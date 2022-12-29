using ImageRetranslationShared.Services.FileTransfer;
using ImageRetranslationShared.Services.Interfaces;
using ImageRetranslationShared.Settings;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var settings = config.GetSection(FileRetranslationSettings.SectionName).Get<FileRetranslationSettings>()!;

Console.WriteLine($"Retranslation host: {settings}");

var cts = new CancellationTokenSource();
var images = Directory.EnumerateFiles("Images");

Console.Write($"Sending {images.Count()} to Retranslation host...");

await using IFileSender sender = new FileSender(settings);
await sender.SendFiles(images, cts.Token);

Console.WriteLine($"completed");