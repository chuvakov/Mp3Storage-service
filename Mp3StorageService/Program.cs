using System.Reflection;
using log4net;
using log4net.Config;
using Mp3StorageService;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(opt => opt.ServiceName = "Mp3Storage")
    .ConfigureServices(services => { services.AddHostedService<Worker>(); })
    .Build();

var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

await host.RunAsync();