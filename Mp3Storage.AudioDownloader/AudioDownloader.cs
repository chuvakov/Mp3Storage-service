using Mp3Storage.AudioDownloader.Api;
using Mp3Storage.AudioDownloader.Common;
using Mp3Storage.AudioDownloader.Dto;
using Mp3Storage.AudioDownloader.Jobs;
using Mp3Storage.AudioDownloader.Storage;
using Mp3Storage.AudioDownloader.Utils;

namespace Mp3Storage.AudioDownloader
{
    public class AudioDownloader : IAudioDownloader
    {
        private readonly ILoggerManager _loggerManager;

        private string _pathToStorage;
        private readonly ICoMagicApiClient _coMagicApiClient;
        private readonly ISessionKeyStorage _sessionKeyStorage;
        private readonly ILinkStorage _linkStorage;
        public static object _locker = new object();

        private static Semaphore SemaphoreMaxRequestDownload { get; set; }

        public AudioDownloader(
            ICoMagicApiClient coMagicApiClient,
            ISessionKeyStorage sessionKeyStorage,
            ILinkStorage linkStorage,
            ILoggerManager loggerManager
            )
        {
            _coMagicApiClient = coMagicApiClient;
            _sessionKeyStorage = sessionKeyStorage;
            _linkStorage = linkStorage;
            _loggerManager=loggerManager;

            _coMagicApiClient.SessionKeyChange += _sessionKeyStorage.SetSessionKey;
        }

        private void InitSettings(string login, string password, string pathToStorage)
        {
            _coMagicApiClient.Login = login;
            _coMagicApiClient.Password = password;
            _pathToStorage = pathToStorage != "Base" ? pathToStorage : FileUtility.GetPathTo("mp3storage");
        }

        public async Task Download(JobDownload job)
        {
            _loggerManager.Info($"{DateTimeOffset.Now}: Скачивание({nameof(Download)}) с {job.DateFrom} по {job.DateTo} maxRequestDownloadCount: {job.MaxRequestDownloadCount} groupBy:{job.GroupBy}");
            await DownloadOneDay(job.DateFrom, job.DateTo, job.MaxRequestDownloadCount, job.GroupBy);
        }

        private async Task DownloadOneDay(DateTime fromDate, DateTime toDate, int? maxRequestDownloadCount, string groupBy)
        {
            _loggerManager.Info($"{DateTimeOffset.Now}: Скачивание({nameof(DownloadOneDay)}) с {fromDate} по {toDate}");

            string sessionKey = _sessionKeyStorage.GetSessionKey();

            if (sessionKey is null)
            {
                sessionKey = await _coMagicApiClient.GetSessionKey();
                _sessionKeyStorage.SetSessionKey(sessionKey);
            }

            _coMagicApiClient.SessionKey = sessionKey;

            IEnumerable<CallDto> calls = null;

            try
            {
                calls = await _coMagicApiClient.GetCalls(fromDate, toDate);
            }
            catch (Mp3StorageException e)
            {
                int retryCount = 5;
                int divider = 2;

                _loggerManager.Error(e.Message, e);
                _loggerManager.Info($"{DateTimeOffset.Now}: Пробуем скачать с дроблениме, попыток({retryCount})");

                while (retryCount > 0)
                {
                    try
                    {
                        _loggerManager.Info($"{DateTimeOffset.Now}: Пробуем скачать с дроблениме, попытка номер({retryCount}), делитель({divider})");

                        TimeSpan different = toDate - fromDate;

                        for (DateTime dstart = fromDate; dstart <= toDate; dstart += different / divider)
                        {
                            var dend = dstart + different / divider;
                            calls = await _coMagicApiClient.GetCalls(dstart, dend);
                            await DownloadCalls(calls, groupBy, maxRequestDownloadCount);
                        }

                        break;
                    }
                    catch (Mp3StorageException ex)
                    {
                        retryCount--;
                        divider++;
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }

                return;
            }
            catch (Exception e)
            {
                throw;
            }


            if (calls != null)
            {
                await DownloadCalls(calls, groupBy, maxRequestDownloadCount);
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