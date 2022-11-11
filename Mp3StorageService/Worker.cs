using log4net;

namespace Mp3StorageService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ILog _log;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        _log = log4net.LogManager.GetLogger(typeof(Program));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        var thread = new Thread(Start);
        thread.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            
        }
    }

    public void Start()
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        _log.Info($"2Worker running at: {DateTimeOffset.Now}");
        var timer = new Timer(t => DownloadMp3Files(), state: null, dueTime: 0, period: 5000);
    }

    private void DownloadMp3Files()
    {
        //using (var sw = new StreamWriter(@"C:\Users\a.chuvakov\Desktop\Новая папка (2)\Logs.txt", true))
        //{
        //    sw.WriteLine($"2Download: {DateTimeOffset.Now}");
        //}
        _logger.LogInformation("Download: {time}", DateTimeOffset.Now);
        _log.Info($"2Download: {DateTimeOffset.Now}");
    }
}