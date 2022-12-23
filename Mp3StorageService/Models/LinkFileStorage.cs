using Mp3Storage.AudioDownloader;
using Mp3Storage.AudioDownloader.Dto;
using Mp3Storage.AudioDownloader.Storage;
using System.Reflection;

namespace Mp3StorageService.Models
{
    internal class LinkFileStorage : ILinkStorage
    {

        private readonly string _pathToFile;
        private readonly ILoggerManager _loggerManager;

        public LinkFileStorage(ILoggerManager loggerManager)
        {
            _loggerManager = loggerManager;
            var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _pathToFile = Path.Combine(appDir, "downloadedLinks.txt");

            if (!File.Exists(_pathToFile))
            {
                var file = File.Create(_pathToFile);
                file.Dispose();
            }
        }

        /// <summary>
        /// Добавление сылки в файл с сылками на файл
        /// </summary>
        /// <param name="link"></param>
        public void Add(string link)
        {
            try
            {
                using var sw = new StreamWriter(_pathToFile, true);
                sw.WriteLine(link);
            }
            catch (Exception e)
            {
                _loggerManager.Error(e.Message, e);
                throw;
            }

        }

        /// <summary>
        /// Получение ссылок на аудио, которые небыли скачены в предыдущей попытке
        /// </summary>
        /// <param name="calls"></param>
        /// <returns></returns>
        public string[] GetLinksNotExist(IEnumerable<CallDto> calls)
        {
            try
            {
                calls = calls.Where(c => c.Links.Any());
                IEnumerable<string> links = calls.SelectMany(c => c.Links);

                var text = File.ReadAllText(_pathToFile);

                _loggerManager.Info($"{DateTimeOffset.Now}: Колличество не скачанных ссылок в звонках({links.Count()})");
                return links.Where(link => !text.Contains(link)).ToArray();
            }
            catch (Exception e)
            {
                _loggerManager.Error(e.Message, e);
                throw;
            }
        }
    }
}
