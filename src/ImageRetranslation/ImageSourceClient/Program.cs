using ImageRetranslationShared.Models;
using ImageRetranslationShared.Settings;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var settings = config.GetSection(ImageRetranslationSettings.SectionName).Get<ImageRetranslationSettings>()!;

Console.WriteLine($"Retranslation host: {settings}");

var cts = new CancellationTokenSource();

string[] images = Array.Empty<string>();
IImageSender sender = new ImageSender(settings);
await sender.SendImages(images, cts.Token);




