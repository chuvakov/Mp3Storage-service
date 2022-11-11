using Mp3Storage.AudioDownloader.Storage;
using System.Reflection;

namespace Mp3StorageService.Models
{
    public class SessionKeyFileStorage : ISessionKeyStorage
    {
        private readonly string _pathToFile;

        public SessionKeyFileStorage()
        {
            var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _pathToFile = Path.Combine(appDir, "sessionKey.txt");
        }

        public string GetSessionKey()
        {
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
            using (var sw = new StreamWriter(_pathToFile))
            {
                sw.WriteLine(sessionKey);
            }
        }
    }
}
