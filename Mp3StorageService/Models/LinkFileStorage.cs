using Mp3Storage.AudioDownloader.Dto;
using Mp3Storage.AudioDownloader.Storage;
using System.Reflection;
using Mp3Storage.AudioDownloader;

namespace Mp3StorageService.Models
{
    internal class LinkFileStorage : ILinkStorage
    {

        private readonly string _pathToFile;
        private readonly ILoggerManager _loggerManager;

        public LinkFileStorage()
        {
            var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _pathToFile = Path.Combine(appDir, "downloadedLinks.txt");

            if (!File.Exists(_pathToFile))
            {
                var file = File.Create(_pathToFile);
                file.Dispose();
            }
        }
        public void Add(string link)
        {
            try
            {
                using (var sw = new StreamWriter(_pathToFile, true))
                {
                    sw.WriteLine(link);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        public string[] GetLinksNotExist(IEnumerable<CallDto> calls)
        {

            try
            {
                calls = calls.Where(c => c.Links.Any());
                IEnumerable<string> links = calls.SelectMany(c => c.Links);

                var result = new List<string>();
                var text = File.ReadAllText(_pathToFile);

                foreach (var link in links)
                {
                    if (!text.Contains(link))
                    {
                        result.Add(link);
                    }
                }

                _loggerManager.Info($"{DateTimeOffset.Now}: Колличество не скачанных ссылок в звонках({links.Count()})");
                return result.ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}
