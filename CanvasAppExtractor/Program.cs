using CanvasAppExtractor;
using Microsoft.Extensions.Options;


var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WindowsBackgroundService>()
    .AddTransient<FileSystemWatcher>()
    .AddSingleton(s => s.GetRequiredService<IOptions<CanvasAppExtractorSettings>>().Value);

builder.Configuration.AddJsonFile("appsettings.json");
builder.Services.Configure<CanvasAppExtractorSettings>(builder.Configuration.GetSection(nameof(CanvasAppExtractorSettings)));

var host = builder.Build();
host.Run();
