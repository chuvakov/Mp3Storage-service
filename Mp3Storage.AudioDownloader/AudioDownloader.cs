using Mp3Storage.AudioDownloader.Api;
using Mp3Storage.AudioDownloader.Storage;
using System.Reflection;
using Mp3Storage.AudioDownloader.Dto;

namespace Mp3Storage.AudioDownloader
{
    public class AudioDownloader : IAudioDownloader
    {
        private readonly string _pathToStorage;
        private readonly ICoMagicApiClient _coMagicApiClient;
        private readonly ISessionKeyStorage _sessionKeyStorage;
        private static Semaphore SemaphoreMaxRequestDownload { get; set; }

        public AudioDownloader(ICoMagicApiClient coMagicApiClient, ISessionKeyStorage sessionKeyStorage, string login, string password, string pathToStorage)
        {
            _coMagicApiClient = coMagicApiClient;
            _sessionKeyStorage = sessionKeyStorage;

            if (pathToStorage != "Base")
            {
                _pathToStorage = pathToStorage;
            }
            else
            {
                //Получаем путь относительно проекта
                var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                _pathToStorage = Path.Combine(appDir, "mp3storage");
            }

            _coMagicApiClient.Login = login;
            _coMagicApiClient.Password = password;

            _coMagicApiClient.SessionKeyChange += _sessionKeyStorage.SetSessionKey;
        }

        public async Task Download(DateTime fromDate, DateTime toDate, int? maxRequestDownloadCount, string groupBy)
        {
            string sessionKey = _sessionKeyStorage.GetSessionKey();

            if (sessionKey is null)
            {
                sessionKey = await _coMagicApiClient.GetSessionKey();
                _sessionKeyStorage.SetSessionKey(sessionKey);
            }

            _coMagicApiClient.SessionKey = sessionKey;

            var calls = await _coMagicApiClient.GetCalls(fromDate, toDate);
            if (calls != null)
            {
                //если папка для хранения отсутствует то создаем ее
                if (!Directory.Exists(_pathToStorage))
                    Directory.CreateDirectory(_pathToStorage);

                calls = calls.Where(c => c.Links.Any());

                IEnumerable<string> links = calls.SelectMany(c => c.Links);

                var tasks = links.Select(l => DownloadAudio(l));

                switch (groupBy)
                {
                    case "Month":
                        IEnumerable<ShortCallDto> shortCalls = calls.SelectMany(c => c.Links.Select(l => new ShortCallDto { Link = l, FolderName = c.Date.ToString("MM.yyyy") }));
                        break;
                    case "Day":
                        break;
                    default: "None":
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
        }

        public async Task DownloadAudio(string link, string folderName = null)
        {
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
                    await Task.Delay(10000);
                    SemaphoreMaxRequestDownload.Release();
                }

                //получаю имя файла
                string filename = response.Content.Headers.ContentDisposition.FileName;

                //получаем полный путь для будущего файла
                var fullPath = Path.Combine(_pathToStorage, Path.GetFileName(filename));

                if (folderName != null)
                {
                    fullPath = Path.Combine(_pathToStorage, folderName , Path.GetFileName(filename));
                }

                using (var fs = new FileStream(fullPath, FileMode.CreateNew))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }
        }

    }
}