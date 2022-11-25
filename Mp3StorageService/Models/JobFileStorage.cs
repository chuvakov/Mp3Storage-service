using Mp3Storage.AudioDownloader;
using Mp3Storage.AudioDownloader.Jobs;
using Newtonsoft.Json;
using System.Reflection;

namespace Mp3StorageService.Models
{
    public class JobFileStorage : IJobStorage
    {
        private readonly string _pathToFile;
        private List<JobDownload> Jobs { get; set; }
        private static object _lock = new object();
        private readonly IAudioDownloader _audioDownloader;

        public JobFileStorage()
        {
            var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _pathToFile = Path.Combine(appDir, "jobStorage.txt");

            if (!File.Exists(_pathToFile))
            {
                var file = File.CreateText(_pathToFile);
                Jobs = new List<JobDownload>();
                file.WriteLine(JsonConvert.SerializeObject(Jobs));
                file.Dispose();
            }
            else
            {
                var text = File.ReadAllText(_pathToFile);
                Jobs = JsonConvert.DeserializeObject<List<JobDownload>>(text);
            }
        }
        public void AddJob(JobDownload jobDownload)
        {
            lock (_lock)
            {
                var text = File.ReadAllText(_pathToFile);
                Jobs = JsonConvert.DeserializeObject<List<JobDownload>>(text);
                Jobs.Add(jobDownload);
                File.WriteAllText(_pathToFile, JsonConvert.SerializeObject(Jobs));
            }
        }

        private void ChangeState(JobDownload jobDownload)
        {
            lock (_lock)
            {
                jobDownload.IsExecute = true;
                File.WriteAllText(_pathToFile, JsonConvert.SerializeObject(Jobs));
            }
        }

        public void Execute()
        {            
            if (Jobs.Any() && Jobs.All(j => !j.IsExecute))
            {
                var job = Jobs.First();
                ChangeState(job);
                _audioDownloader.Download(job, null, null);
            }
        }
    }
}
