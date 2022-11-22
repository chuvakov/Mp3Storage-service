using Mp3Storage.AudioDownloader;
using Mp3Storage.AudioDownloader.Storage;
using System.Reflection;

namespace Mp3StorageService.Models
{
    public class SessionKeyFileStorage : ISessionKeyStorage
    {
        private readonly ILoggerManager _loggerManager;

        private readonly string _pathToFile;

        public SessionKeyFileStorage(ILoggerManager loggerManager)
        {
            var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _pathToFile = Path.Combine(appDir, "sessionKey.txt");
            _loggerManager=loggerManager;
        }

        public string GetSessionKey()
        {
            _loggerManager.Info($"{DateTimeOffset.Now}: Получаем ключ сессии из файла({nameof(GetSessionKey)})");

            string sessionKey = null;
            if (File.Exists(_pathToFile))
            {
                using (var sr = new StreamReader(_pathToFile))
                {
                    sessionKey = sr.ReadLine();
                }
            }

            return sessionKey;
        }

        public void SetSessionKey(string sessionKey)
        {
            _loggerManager.Info($"{DateTimeOffset.Now}: Записываем новый ключ сессии в файл({nameof(SetSessionKey)})");

            using (var sw = new StreamWriter(_pathToFile))
            {
                sw.WriteLine(sessionKey);
            }
        }
    }
}
