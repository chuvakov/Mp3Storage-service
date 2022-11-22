using log4net;
using log4net.Config;
using Mp3Storage.AudioDownloader;
using Mp3Storage.AudioDownloader.Api;
using Mp3Storage.AudioDownloader.Storage;
using Mp3StorageService;
using Mp3StorageService.Models;
using System.Net.Http.Headers;
using System.Reflection;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(opt => opt.ServiceName = "Mp3Storage")
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Worker>();        

        var configuration = hostContext.Configuration;
        services.AddHttpClient<ICoMagicApiClient, CoMagicApiClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["App:CoMagicApi:BaseAddress"]);
            client.Timeout = TimeSpan.Parse(configuration["App:CoMagicApi:Timeout"]);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddTransient<ISessionKeyStorage, SessionKeyFileStorage>();
        services.AddTransient<ILinkStorage, LinkFileStorage>();
        services.AddTransient<ILoggerManager, LoggerManager>();
        services.AddTransient<IAudioDownloader>(serviceProvider =>
            new AudioDownloader(serviceProvider.GetRequiredService<ICoMagicApiClient>(),
            serviceProvider.GetRequiredService<ISessionKeyStorage>(),
            configuration["App:CoMagicApi:Login"], configuration["App:CoMagicApi:Password"],
            configuration["App:PathToStorage"], serviceProvider.GetRequiredService<ILinkStorage>(), serviceProvider.GetRequiredService<ILoggerManager>()));      
    })
    .Build();

var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

await host.RunAsync();