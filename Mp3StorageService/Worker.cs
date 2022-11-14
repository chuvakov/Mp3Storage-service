using System.Globalization;
using log4net;
using Mp3Storage.AudioDownloader;

namespace Mp3StorageService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ILog _log;

    private readonly IAudioDownloader _audioDownloader;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IAudioDownloader audioDownloader, IConfiguration configuration)
    {
        _logger = logger;
        _log = LogManager.GetLogger(typeof(Program));
        _audioDownloader = audioDownloader;
        _configuration = configuration;
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
        var timer = new Timer(t => DownloadMp3Files(), state: null, dueTime: 0, period: 3600 * 24 * 1000);
    }

    private void DownloadMp3Files()
    {
        bool isDailyMode = bool.Parse(_configuration["App:IsDailyMode"]);
        DateTime dateFrom, dateTo;

        if (isDailyMode)
        {
            var dateTimeNow = DateTimeOffset.Now;
            dateFrom = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day);
            dateTo = dateFrom.AddDays(1).AddSeconds(-1);
        }
        else
        {
            dateFrom = DateTime.Parse(_configuration["App:DateFrom"]);
            dateTo = DateTime.ParseExact(_configuration["App:DateTo"], "dd.MM.yyyy", CultureInfo.InvariantCulture);
        }

        var maxRequestDownloadCountSetting = _configuration["App:MaxRequestDownloadCount"];
        int? maxRequestDownloadCount = maxRequestDownloadCountSetting != "Max" ? int.Parse(maxRequestDownloadCountSetting) : null;

        var groupByType = _configuration["App:GroupBy"];
        _audioDownloader.Download(dateFrom, dateTo, maxRequestDownloadCount, groupByType).Wait();
    }
}