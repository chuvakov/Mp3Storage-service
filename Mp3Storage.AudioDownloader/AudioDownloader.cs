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

        public async Task DownloadCalls(IEnumerable<CallDto> calls, string groupBy, int? maxRequestDownloadCount)
        {
            if (calls == null)
                return;

            if (!Directory.Exists(_pathToStorage))
                Directory.CreateDirectory(_pathToStorage);

            var tasks = GetTasksForDownload(calls, groupBy);

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