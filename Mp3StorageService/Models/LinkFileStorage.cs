using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mp3Storage.AudioDownloader.Storage;

namespace Mp3StorageService.Models
{
    internal class LinkFileStorage : ILinkStorage
    {

        private readonly string _pathToFile;

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

        public string[] GetLinksNotExist(string[] links)
        {

            try
            {
                var result = new List<string>();
                var text = File.ReadAllText(_pathToFile);

                foreach (var link in links)
                {
                    if (!text.Contains(link))
                    {
                        result.Add(link);
                    }
                }
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
