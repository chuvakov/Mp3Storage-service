using Mp3Storage.AudioDownloader;
using Mp3Storage.AudioDownloader.Jobs;
using Newtonsoft.Json;
using System.Reflection;

namespace Mp3StorageService.Models
{
    public class JobFileStorage : IJobStorage
    {
        private readonly string _pathToFile;
        private readonly ILoggerManager _logger;
        private List<JobDownload> Jobs { get; set; }
        private static object _lock = new object();
        private readonly IAudioDownloader _audioDownloader;
        public static bool IsCanExecuteJob = true;

        public JobFileStorage(IAudioDownloader audioDownloader, ILoggerManager logger)
        {
            _audioDownloader = audioDownloader;
            _logger = logger;
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

                for (DateTime dateStart = jobDownload.DateFrom; dateStart <= jobDownload.DateTo; dateStart = dateStart.AddHours(6)) //AddDays(1))
                {
                    jobDownload.ChildJobs.Add(new JobDownload()
                    {
                        DateFrom = dateStart,
                        DateTo = dateStart.AddHours(6).AddSeconds(-1),
                        GroupBy = jobDownload.GroupBy,
                        MaxRequestDownloadCount = jobDownload.MaxRequestDownloadCount,
                    });
                }
                File.WriteAllText(_pathToFile, JsonConvert.SerializeObject(Jobs));
            }
        }

        public void ChangeState(JobDownload jobDownload, JobState state)
        {
            lock (_lock)
            {
                jobDownload.State = state;
                File.WriteAllText(_pathToFile, JsonConvert.SerializeObject(Jobs));
            }
        }

        public async Task Execute()
        {
            try
            {
                _logger.Info($"Зашли в метод Execute()");

                if (Jobs.Any(j => j.State != JobState.Success) && IsCanExecuteJob)
                {
                    IsCanExecuteJob = false;
                    var job = Jobs.First(j => j.State != JobState.Success);
                    _logger.Info($"Работа job - {job.DateFrom} {job.DateTo}");
                    ChangeState(job, JobState.Execute);

                    foreach (var childJob in job.ChildJobs.Where(j => j.State != JobState.Success))
                    {
                        _logger.Info($"Работа childJob - {childJob.DateFrom} {childJob.DateTo}");

                        try
                        {
                            ChangeState(childJob, JobState.Execute);
                            //await _audioDownloader.Download(childJob);
                            await Task.Delay(100000000);
                            ChangeState(childJob, JobState.Success);
                        }
                        catch (Exception)
                        {
                            ChangeState(childJob, JobState.Error);
                        }
                    }

                    if (job.ChildJobs.All(cj => cj.State == JobState.Success))
                    {
                        ChangeState(job, JobState.Success);
                    }

                    IsCanExecuteJob = true;
                }
            }
            catch (Exception e)
            {
                IsCanExecuteJob = true;
                _logger.Error($"Ошибка при выполнении работы", e);
            }

            
        }
    }
}
