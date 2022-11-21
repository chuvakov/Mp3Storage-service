using System.Globalization;
using log4net;
using Mp3Storage.AudioDownloader;

namespace Mp3StorageService;

public class Worker : BackgroundService
{
    //private readonly ILoggerManager _loggerManager;

    private readonly IAudioDownloader _audioDownloader;
    private readonly IConfiguration _configuration;
    private bool IsAutoDailyMode = false;
    private Timer _timer;

    public Worker(IAudioDownloader audioDownloader, IConfiguration configuration) //, ILoggerManager loggerManager)
    {
        _audioDownloader = audioDownloader;
        _configuration = configuration;
        //_loggerManager = loggerManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //_loggerManager.Info($"Служба запущена {DateTimeOffset.Now}");
        var thread = new Thread(Start);
        thread.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            
        }
    }

    public void Start()
    {
        //_loggerManager.Info($"Создан таймер для периодического скачивания {DateTimeOffset.Now}");
        _timer = new Timer(t => DownloadMp3Files(), state: null, dueTime: 0, period: 3600 * 24 * 1000); //3600 * 24 * 1000
    }

    private async void DownloadMp3Files(DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        try
        {
            //ToDo: Попытаться определять что ошибка произошла при получении данных из конфига
            bool isDailyMode = bool.Parse(_configuration["App:IsDailyMode"]);            
            var dateTimeNow = DateTimeOffset.Now;

            if (!dateFrom.HasValue && !dateTo.HasValue)
            {
                if (isDailyMode || IsAutoDailyMode)
                {
                    dateFrom = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day);
                    dateTo = dateFrom.Value.AddDays(1).AddSeconds(-1);
                }
                else
                {
                    IsAutoDailyMode = true;
                    dateFrom = DateTime.Parse(_configuration["App:DateFrom"]);
                    var dateToFromConfig = _configuration["App:DateTo"];
                    if (dateToFromConfig == "Today")
                    {
                        dateTo = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day);
                    }
                    else
                    {
                        dateTo = DateTime.Parse(_configuration["App:DateTo"]);
                    }
                }
            }

            

            var maxRequestDownloadCountSetting = _configuration["App:MaxRequestDownloadCount"];
            int? maxRequestDownloadCount = maxRequestDownloadCountSetting != "Max" ? int.Parse(maxRequestDownloadCountSetting) : null;

            var groupByType = _configuration["App:GroupBy"];
            _audioDownloader.Download(dateFrom.Value, dateTo.Value, maxRequestDownloadCount, groupByType).Wait();
        }
        catch (Exception e)
        {
            //_loggerManager.Error($"Произошла ошибка при скачивании ({nameof(DownloadMp3Files)}) - {DateTimeOffset.Now}", e);

            await Task.Delay(3600 * 1000);
            DownloadMp3Files(dateFrom, dateTo);
        }
    }
}