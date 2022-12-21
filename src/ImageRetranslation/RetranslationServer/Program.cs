using ImageRetranslationShared.Settings;
using Microsoft.Extensions.Configuration;
using Retranslation;

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var settings = config.GetSection(ImageRetranslationSettings.SectionName).Get<ImageRetranslationSettings>();

var server = new RetranslationServer(settings);
await server.Start();

