using Mp3Storage.AudioDownloader.Api;
using Mp3Storage.AudioDownloader.Common;
using Mp3Storage.AudioDownloader.Dto;
using Mp3Storage.AudioDownloader.Jobs;
using Mp3Storage.AudioDownloader.Storage;
using Mp3Storage.AudioDownloader.Utils;
using Polly;

namespace Mp3Storage.AudioDownloader;

public class AudioDownloader : IAudioDownloader
{
    private readonly ILoggerManager _loggerManager;
    private readonly ICoMagicApiClient _coMagicApiClient;
    private readonly ILinkStorage _linkStorage;

    private string _pathToStorage;
    private static object _locker = new object();
    private static Semaphore _semaphoreMaxRequestDownload { get; set; }

    public AudioDownloader(
        ICoMagicApiClient coMagicApiClient,
        ILinkStorage linkStorage,
        ILoggerManager loggerManager
    )
    {
        _coMagicApiClient = coMagicApiClient;
        _linkStorage = linkStorage;
        _loggerManager=loggerManager;
    }

    /// <summary>
    /// Инициализация настроек для скачивания
    /// </summary>
    /// <param name="login"></param>
    /// <param name="password"></param>
    /// <param name="pathToStorage"></param>
    public void InitSettings(string login, string password, string pathToStorage)
    {
        _coMagicApiClient.Login = login;
        _coMagicApiClient.Password = password;

        _pathToStorage = pathToStorage != "Base" ? pathToStorage : FileUtility.GetPathTo("mp3storage");
        if (!Directory.Exists(_pathToStorage))
            Directory.CreateDirectory(_pathToStorage);
    }

    /// <summary>
    /// Выполняем работу (скачивание)
    /// </summary>
    /// <param name="job"></param>
    /// <returns></returns>
    public async Task Execute(JobDownload job)
    {
        _loggerManager.Info($"{DateTimeOffset.Now}: Скачивание({nameof(Execute)}) с {job.DateFrom} по {job.DateTo} maxRequestDownloadCount: {job.MaxRequestDownloadCount} groupBy:{job.GroupBy}");

        IEnumerable<CallDto> calls = null;
        var divider = 2;
        var retryCount = 4;

        //Задаем политику повторения выполнения метода
        var policy = Policy
            .Handle<Mp3StorageException>()
            .RetryAsync(retryCount, (e, retryCount) =>
            {
                _loggerManager.Info(
                    $"{DateTimeOffset.Now}: Пробуем скачать с дроблениме, попытка номер({retryCount}), делитель({divider})");
                divider++;
            });

        try
        {
            calls = await _coMagicApiClient.GetCalls(job.DateFrom, job.DateTo);
            await DownloadCalls(calls, job.GroupBy, job.MaxRequestDownloadCount);
        }
        catch (Mp3StorageException e)
        {
            _loggerManager.Error(e.Message, e);
            _loggerManager.Info($"{DateTimeOffset.Now}: Пробуем скачать с дроблениме, попыток({retryCount})");

            await policy.ExecuteAsync(() => DownloadWithDivider(divider, job));
        }
    }

    /// <summary>
    /// Скачивание с дроблением
    /// </summary>
    /// <param name="divider"></param>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <param name="groupBy"></param>
    /// <param name="maxRequestDownloadCount"></param>
    /// <returns></returns>
    private async Task DownloadWithDivider(int divider, JobDownload job)
    {
        var different = job.DateTo - job.DateFrom;

        for (var dateFrom = job.DateFrom; dateFrom <= job.DateTo; dateFrom += different / divider)
        {
            var dateTo = dateFrom + different / divider;
            var calls = await _coMagicApiClient.GetCalls(dateFrom, dateTo);
            await DownloadCalls(calls, job.GroupBy, job.MaxRequestDownloadCount);
        }
    }

    /// <summary>
    /// Скачиваем звонки
    /// </summary>
    /// <param name="calls"></param>
    /// <param name="groupBy"></param>
    /// <param name="maxRequestDownloadCount"></param>
    /// <returns></returns>
    public async Task DownloadCalls(IEnumerable<CallDto> calls, string groupBy, int? maxRequestDownloadCount)
    {
        if (calls == null)
            return;

        var tasks = GetTasksForDownload(calls, groupBy);

        if (maxRequestDownloadCount.HasValue)
        {
            _semaphoreMaxRequestDownload = new Semaphore(maxRequestDownloadCount.Value,
                maxRequestDownloadCount.Value);
        }

        await Task.WhenAll(tasks);
        _semaphoreMaxRequestDownload = null;
    }

    /// <summary>
    /// Получение списка задач на скачивание
    /// </summary>
    /// <param name="calls"></param>
    /// <param name="groupBy"></param>
    /// <returns></returns>
    private IEnumerable<Task> GetTasksForDownload(IEnumerable<CallDto> calls, string groupBy)
    {
        var links = _linkStorage.GetLinksNotExist(calls);

        return groupBy switch
        {
            "Month" => GetTasksForDownloadByGroup(calls, links, "MM.yyyy"),
            "Day" => GetTasksForDownloadByGroup(calls, links, "dd.MM.yyyy"),
            _ => links.Select(l => DownloadAudio(l))
        };
    }

    /// <summary>
    /// Получение списка задач при скачивании с группировкой по папкам
    /// </summary>
    /// <param name="calls"></param>
    /// <param name="links"></param>
    /// <param name="folderMask"></param>
    /// <returns></returns>
    private IEnumerable<Task> GetTasksForDownloadByGroup(IEnumerable<CallDto> calls, string[] links, string folderMask)
    {
        var tasks = new List<Task>();

        foreach (var call in calls)
        {
            var folderName = DateTime.Parse(call.Date).ToString(folderMask);
            var callLinks = call.Links.Where(l => links.Contains(l));

            foreach (var link in callLinks)
            {
                tasks.Add(DownloadAudio(link, folderName));
            }
        }

        return tasks;
    }

    /// <summary>
    /// Скачиваем одно вудио по ссылке
    /// </summary>
    /// <param name="link"></param>
    /// <param name="folderName"></param>
    /// <returns></returns>
    public async Task DownloadAudio(string link, string folderName = null)
    {
        _loggerManager.Info($"{DateTimeOffset.Now}: Скачивание аудио по ссылке({link})");

        using var httpClient = new HttpClient();

        _semaphoreMaxRequestDownload?.WaitOne();
        var response = await httpClient.GetAsync("https:" + link);
        _semaphoreMaxRequestDownload?.Release();

        //получаю имя файла
        var fileName = response.Content.Headers.ContentDisposition.FileName;
        var fullPath = GetFullPathToFile(folderName, fileName);

        await using var fs = new FileStream(fullPath, FileMode.CreateNew);
        await response.Content.CopyToAsync(fs);

        lock (_locker)
        {
            _linkStorage.Add(link);
        }
    }

    /// <summary>
    /// получаем полный путь для будущего файла
    /// </summary>
    /// <param name="folderName"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private string GetFullPathToFile(string folderName, string fileName)
    {
        if (folderName == null)
            return Path.Combine(_pathToStorage, Path.GetFileName(fileName));

        var pathToFolder = Path.Combine(_pathToStorage, folderName);
        if (!Directory.Exists(pathToFolder))
        {
            Directory.CreateDirectory(pathToFolder);
        }

        return Path.Combine(pathToFolder, Path.GetFileName(fileName));
    }
}