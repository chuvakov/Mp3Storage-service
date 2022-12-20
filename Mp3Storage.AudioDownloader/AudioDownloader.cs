using Mp3Storage.AudioDownloader.Api;
using Mp3Storage.AudioDownloader.Common;
using Mp3Storage.AudioDownloader.Dto;
using Mp3Storage.AudioDownloader.Jobs;
using Mp3Storage.AudioDownloader.Storage;
using Mp3Storage.AudioDownloader.Utils;
using Polly;

namespace Mp3Storage.AudioDownloader
{
    public class AudioDownloader : IAudioDownloader
    {
        private readonly ILoggerManager _loggerManager;

        private string _pathToStorage;
        private readonly ICoMagicApiClient _coMagicApiClient;
        private readonly ILinkStorage _linkStorage;
        public static object _locker = new object();

        private static Semaphore SemaphoreMaxRequestDownload { get; set; }

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
            }
            catch (Mp3StorageException e)
            {
                _loggerManager.Error(e.Message, e);
                _loggerManager.Info($"{DateTimeOffset.Now}: Пробуем скачать с дроблениме, попыток({retryCount})");

                await policy.ExecuteAsync(() => DownloadWithDivider(divider, job));
            }

            if (calls != null)
            {
                await DownloadCalls(calls, job.GroupBy, job.MaxRequestDownloadCount);
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

        public async Task DownloadCalls(IEnumerable<CallDto> calls, string groupBy, int? maxRequestDownloadCount)
        {
            //если папка для хранения отсутствует то создаем ее
            if (!Directory.Exists(_pathToStorage))
                Directory.CreateDirectory(_pathToStorage);

            calls = calls.Where(c => c.Links.Any());

            IEnumerable<string> links = calls.SelectMany(c => c.Links);
            links = _linkStorage.GetLinksNotExist(links.ToArray());

            _loggerManager.Info($"{DateTimeOffset.Now}: Колличество ссылок в звонках({links.Count()})");

            var tasks = links.Select(l => DownloadAudio(l));

            IEnumerable<ShortCallDto> shortCalls;

            switch (groupBy)
            {
                case "Month":
                    shortCalls = calls.SelectMany(c => c.Links.Select(l => new ShortCallDto
                    { Link = l, FolderName = DateTime.Parse(c.Date).ToString("MM.yyyy") }));
                    shortCalls = shortCalls.Where(c => links.Contains(c.Link));
                    tasks = shortCalls.Select(c => DownloadAudio(c.Link, c.FolderName));
                    break;
                case "Day":
                    shortCalls = calls.SelectMany(c => c.Links.Select(l => new ShortCallDto
                    { Link = l, FolderName = DateTime.Parse(c.Date).ToString("dd.MM.yyyy") }));
                    shortCalls = shortCalls.Where(c => links.Contains(c.Link));
                    tasks = shortCalls.Select(c => DownloadAudio(c.Link, c.FolderName));
                    break;
            }

            if (maxRequestDownloadCount.HasValue)
            {
                SemaphoreMaxRequestDownload = new Semaphore(maxRequestDownloadCount.Value, maxRequestDownloadCount.Value);
            }

            await Task.WhenAll(tasks);

            if (SemaphoreMaxRequestDownload != null)
            {
                SemaphoreMaxRequestDownload = null;
            }
        }

        public async Task DownloadAudio(string link, string folderName = null)
        {
            _loggerManager.Info($"{DateTimeOffset.Now}: Скачивание аудио по ссылке({link})");

            var url = "https:" + link;  //дернул ссылку на аудио - для теста

            using (var httpClient = new HttpClient())
            {
                if (SemaphoreMaxRequestDownload != null)
                {
                    SemaphoreMaxRequestDownload.WaitOne();
                }

                var response = await httpClient.GetAsync(url);

                if (SemaphoreMaxRequestDownload != null)
                {
                    SemaphoreMaxRequestDownload.Release();
                }

                //получаю имя файла
                string filename = response.Content.Headers.ContentDisposition.FileName;

                //получаем полный путь для будущего файла
                var fullPath = Path.Combine(_pathToStorage, Path.GetFileName(filename));

                if (folderName != null)
                {
                    var pathToFolder = Path.Combine(_pathToStorage, folderName);

                    if (!Directory.Exists(pathToFolder))
                    {
                        Directory.CreateDirectory(pathToFolder);
                    }
                    fullPath = Path.Combine(pathToFolder, Path.GetFileName(filename));
                }

                using (var fs = new FileStream(fullPath, FileMode.CreateNew))
                {
                    await response.Content.CopyToAsync(fs);
                }

                lock (_locker)
                {
                    _linkStorage.Add(link);
                }
            }
        }

    }
}