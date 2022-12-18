using ImageRetranslationShared.Settings;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var settings = config.GetSection(ImageRetranslationSettings.SectionName).Get<ImageRetranslationSettings>();


var server = new RetranslationServer.Models.RetranslationServer(settings);
await server.Start();

