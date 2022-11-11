using Mp3Storage.AudioDownloader.Api;
using Mp3Storage.AudioDownloader.Storage;
using System.Net.Http.Headers;
using System.Reflection;

namespace Mp3Storage.AudioDownloader
{
    public class AudioDownloader : IAudioDownloader
    {
        private readonly ICoMagicApiClient _coMagicApiClient;
        private readonly ISessionKeyStorage _sessionKeyStorage;

        public AudioDownloader(ICoMagicApiClient coMagicApiClient, ISessionKeyStorage sessionKeyStorage)
        {
            _coMagicApiClient = coMagicApiClient;
            _sessionKeyStorage = sessionKeyStorage;
        }

        public async void Download()
        {
            //Получаем путь относительно проекта
            var appDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string sessionKey = _sessionKeyStorage.GetSessionKey();

            if (sessionKey is null)
            {
                sessionKey = await _coMagicApiClient.GetSessionKey();                
            }
            


            var request = $"v1/call/?session_key={sessionKey}&date_from=2022-07-11%2012:33:00&date_till=2022-07-11%2012:34:00";

            var response = await client.GetAsync(request);

            // Unauthorized

            var t = response.Content.ReadAsStringAsync();

            var callResponse = await client.GetFromJsonAsync<CallResponse>(request);

            if (callResponse == null)
            {
                return;
            }

            //проверка авторизации
            if (callResponse.Message != null && callResponse.Message.Contains("Unauthorized"))
            {
                var key = await client.GetFromJsonAsync<SessionKey>("login/?login=systemapi&password=Putin2020!@21Aw");

                if (key != null)
                {
                    Console.WriteLine($"Success: {key.Success}   Data: {key.Data.Session_key}");
                }

                sessionKey = key.Data.Session_key;

                using (var sw = new StreamWriter(Path.Combine(appDir, "sessionKey.txt")))
                {
                    sw.WriteLine(sessionKey);
                }
            }

            var calls = callResponse?.Calls?.Where(c => c.Links.Any());

            if (calls != null)
            {
                foreach (var call in calls)
                {
                    foreach (var link in call.Links)
                    {
                        string urlToAudio = "https:" + link;  //дернул ссылку на аудио - для теста

                        using (WebClient webClient = new WebClient())
                        {
                            webClient.OpenRead(urlToAudio);

                            //получаю имя файла
                            string header_contentDisposition = webClient.ResponseHeaders["content-disposition"];
                            string filename = new ContentDisposition(header_contentDisposition).FileName;



                            //если папка для хранения отсутствует то создаем ее
                            if (!Directory.Exists(System.IO.Path.Combine(appDir, "mp3storage")))
                                Directory.CreateDirectory(System.IO.Path.Combine(appDir, "mp3storage"));

                            //получаем полный путь для будущего файла
                            var fullPath = System.IO.Path.Combine(appDir, "mp3storage", filename);

                            //скачиваем файл по ссылке и кладем его по полному пути
                            await webClient.DownloadFileTaskAsync(new Uri(urlToAudio), fullPath);
                        }
                    }
                }
            }

            //Получение медиафайлов по ссылкам


        }
    }
    }
}