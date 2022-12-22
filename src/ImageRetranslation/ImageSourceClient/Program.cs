using DataStreaming.Services;
using DataStreaming.Common.Settings;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var settings = config.GetSection(FileRetranslationSettings.SectionName).Get<FileRetranslationSettings>()!;

Console.WriteLine($"Retranslation host: {settings}");

var cts = new CancellationTokenSource();

string[] images = Directory.EnumerateFiles("Images").ToArray();

Console.Write($"Sending {images.Length} to Retranslation host...");

IFileSender sender = new FileSender(settings);
await sender.SendImages(images, cts.Token);

Console.WriteLine($"completed");
Console.ReadLine();