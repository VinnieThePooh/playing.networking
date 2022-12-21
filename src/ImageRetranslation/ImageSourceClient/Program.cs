using ImageRetranslationShared.Models;
using ImageRetranslationShared.Settings;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var settings = config.GetSection(ImageRetranslationSettings.SectionName).Get<ImageRetranslationSettings>()!;

Console.WriteLine($"Retranslation host: {settings}");

var cts = new CancellationTokenSource();

string[] images = Directory.EnumerateFiles("Images").OrderBy(x => Random.Shared.Next()).ToArray();

Console.Write($"Sending {images.Length} to Retranslation host...");
IImageSender sender = new ImageSender(settings);
await sender.SendImages(images, cts.Token);
Console.WriteLine($"completed");