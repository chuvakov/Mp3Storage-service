using System.Globalization;
using Mp3Storage.AudioDownloader;
using Mp3Storage.AudioDownloader.Jobs;

namespace Mp3StorageService;

public class Worker : BackgroundService
{
    private readonly ILoggerManager _loggerManager;
    private readonly IConfiguration _configuration;
    private readonly IJobStorage _jobStorage;
    private bool IsAutoDailyMode = false;
    private Timer _timer;
    private Timer _timerExecuter;

    public Worker(IConfiguration configuration, ILoggerManager loggerManager, IJobStorage jobStorage)
    {
        _configuration = configuration;
        _loggerManager = loggerManager;
        _jobStorage = jobStorage;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _loggerManager.Info($"������ �������� {DateTimeOffset.Now}");
        new Thread(Start).Start();

        while (!stoppingToken.IsCancellationRequested)
        {

        }
    }

    public void Start()
    {
        _loggerManager.Info($"������ ������ ��� �������������� ���������� {DateTimeOffset.Now}");
        _timer = new Timer(t => DownloadMp3Files(), state: null, dueTime: 0, period: 3600 * 24 * 1000); //3600 * 24 * 1000
        _timerExecuter = new Timer(t => _jobStorage.ExecuteFirstJob(), state: null, dueTime: 0, period: 1 * 5 * 1000); // 4 * 60 * 1000) - 4 ������
    }

    private async void DownloadMp3Files(DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        try
        {
            //ToDo: ���������� ���������� ��� ������ ��������� ��� ��������� ������ �� �������
            var isDailyMode = bool.Parse(_configuration["App:IsDailyMode"]);
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
                    dateFrom = DateTime.ParseExact(_configuration["App:DateFrom"], "dd.MM.yyyy", CultureInfo.CurrentCulture);
                    var dateToFromConfig = _configuration["App:DateTo"];
                    if (dateToFromConfig == "Today")
                    {
                        dateTo = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day);
                    }
                    else
                    {
                        dateTo = DateTime.ParseExact(_configuration["App:DateTo"], "dd.MM.yyyy", CultureInfo.CurrentCulture);
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
            _loggerManager.Error($"��������� ������ ��� ���������� ������ ({nameof(DownloadMp3Files)}) - {DateTimeOffset.Now}", e);
        }
    }
}