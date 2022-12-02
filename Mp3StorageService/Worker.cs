using System.Globalization;
using log4net;
using Microsoft.Win32;
using Mp3Storage.AudioDownloader;
using Mp3Storage.AudioDownloader.Jobs;

namespace Mp3StorageService;

public class Worker : BackgroundService
{
    private readonly ILoggerManager _loggerManager;

    private readonly IAudioDownloader _audioDownloader;
    private readonly IConfiguration _configuration;
    private readonly IJobStorage _jobStorage;
    private bool IsAutoDailyMode = false;
    private Timer _timer;
    private Timer _timerExecuter;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public Worker(IAudioDownloader audioDownloader, IConfiguration configuration, ILoggerManager loggerManager, IJobStorage jobStorage, IHostApplicationLifetime applicationLifetime)
    {
        _audioDownloader = audioDownloader;
        _configuration = configuration;
        _loggerManager = loggerManager;
        _jobStorage = jobStorage;
        _applicationLifetime = applicationLifetime;

        _applicationLifetime.ApplicationStopped.Register(Stop);
        _applicationLifetime.ApplicationStopping.Register(Stop);
        AppDomain.CurrentDomain.ProcessExit += Exit;
    }

    private void Exit(object? sender, EventArgs e)
    {
        _loggerManager.Info("Exit");
    }

    private void Stop()
    {
        _loggerManager.Info("Stop");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _loggerManager.Info($"Служба запущена {DateTimeOffset.Now}");
        var thread = new Thread(Start);
        thread.Start();

        SystemEvents.SessionEnding += SystemEvents_SessionEnding;

        while (!stoppingToken.IsCancellationRequested)
        {

        }
    }

    public void Start()
    {
        _loggerManager.Info($"Создан таймер для периодического скачивания {DateTimeOffset.Now}");
        _timer = new Timer(t => DownloadMp3Files(), state: null, dueTime: 0, period: 3600 * 24 * 1000); //3600 * 24 * 1000
        _timerExecuter = new Timer(t => _jobStorage.Execute(), state: null, dueTime: 0, period: 1 * 5 * 1000); // 4 * 60 * 1000) - 4 минуты
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
            _jobStorage.AddJob(new JobDownload()
            {
                DateFrom = dateFrom.Value,
                DateTo = dateTo.Value,
                MaxRequestDownloadCount = maxRequestDownloadCount,
                GroupBy = groupByType
            });
        }
        catch (Exception e)
        {
            _loggerManager.Error($"Произошла ошибка при добавлении работы ({nameof(DownloadMp3Files)}) - {DateTimeOffset.Now}", e);

            await Task.Delay(3000); // 3600 * 1000 - один час, через который повторяем попытку
            DownloadMp3Files(dateFrom, dateTo);
        }
    }
    void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
    {
        switch (e.Reason)
        {
            case SessionEndReasons.Logoff:
                _loggerManager.Info("User logging off");
                break;

            case SessionEndReasons.SystemShutdown:
                _loggerManager.Info("System is shutting down");
                break;
        }
    }
}