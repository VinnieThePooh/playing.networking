using DataStreaming.Services.FileTransfer;
using DataStreaming.Settings;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var settings = config.GetSection(FileRetranslationSettings.SectionName).Get<FileRetranslationSettings>();

var server = new RetranslationServer(settings);
await server.Start();

