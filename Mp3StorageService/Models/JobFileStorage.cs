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
        private List<JobDownload> Jobs { get; set; } = new List<JobDownload>();
        private static object _lock = new object();
        private readonly IAudioDownloader _audioDownloader;
        public static bool IsAlreadyExecuteJob;

        public JobFileStorage(IAudioDownloader audioDownloader, ILoggerManager logger)
        {
            _audioDownloader = audioDownloader;
            _logger = logger;

            var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _pathToFile = Path.Combine(appDir, "jobStorage.txt");

            if (!File.Exists(_pathToFile))
            {
                using var file = File.CreateText(_pathToFile);
                file.WriteLine(JsonConvert.SerializeObject(Jobs));
            }
            else
            {
                InitJobs();
            }
        }

        /// <summary>
        /// Инициализация списка задач на уровне класса из файла
        /// </summary>
        private void InitJobs()
        {
            var text = File.ReadAllText(_pathToFile);
            Jobs = JsonConvert.DeserializeObject<List<JobDownload>>(text);
        }

        /// <summary>
        /// Добавление работы
        /// </summary>
        /// <param name="job">работа для выгрузки аудио</param>
        public void AddJob(JobDownload job)
        {
            lock (_lock)
            {
                InitJobs();
                InitJobChilds(job);

                Jobs.Add(job);
                UpdateJobsInFile();
            }
        }

        /// <summary>
        /// Обновление работ в файле
        /// </summary>
        private void UpdateJobsInFile()
        {
            File.WriteAllText(_pathToFile, JsonConvert.SerializeObject(Jobs));
        }

        /// <summary>
        /// Инициализация дочерних задач для переданной работы (разбиение задач)
        /// </summary>
        /// <param name="job">работа для выгрузки аудио</param>
        private static void InitJobChilds(JobDownload job)
        {
            for (DateTime dateStart = job.DateFrom; dateStart <= job.DateTo; dateStart = dateStart.AddHours(6))
            {
                job.ChildJobs.Add(new JobDownload()
                {
                    DateFrom = dateStart,
                    DateTo = dateStart.AddHours(6).AddSeconds(-1),
                    GroupBy = job.GroupBy,
                    MaxRequestDownloadCount = job.MaxRequestDownloadCount,
                });
            }
        }

        /// <summary>
        /// Смена статуса задачи
        /// </summary>
        /// <param name="job">работа</param>
        /// <param name="state">статус</param>
        public void ChangeState(JobDownload job, JobState state)
        {
            lock (_lock)
            {
                job.State = state;
                UpdateJobsInFile();
            }
        }

        /// <summary>
        /// Выполнение первой не выполненной задачи из хранилища задач
        /// </summary>
        /// <returns></returns>
        public async Task ExecuteFirstJob()
        {
            try
            {
                _logger.Info($"Зашли в метод ExecuteFirstJob()");
                if (!IsCanExecuteJob()) 
                    return;

                IsAlreadyExecuteJob = true;

                var job = Jobs.First(j => j.State != JobState.Success);
                await ExecuteJob(job);

                if (job.ChildJobs.All(cj => cj.State == JobState.Success))
                {
                    ChangeState(job, JobState.Success);
                }

                IsAlreadyExecuteJob = false;
            }
            catch (Exception e)
            {
                IsAlreadyExecuteJob = false;
                _logger.Error($"Ошибка при выполнении работы", e);
            }
        }

        /// <summary>
        /// Выполнение работы
        /// </summary>
        /// <param name="job">работа</param>
        /// <returns></returns>
        private async Task ExecuteJob(JobDownload job)
        {
            ChangeState(job, JobState.Execute);

            _logger.Info($"Работа job начала выполняться - {job.DateFrom} {job.DateTo}");
            var childJobs = job.ChildJobs.Where(j => j.State != JobState.Success);

            foreach (var childJob in childJobs)
            {
                await ExecuteChildJob(childJob);
            }
        }

        /// <summary>
        /// Выполнение дочерней работы
        /// </summary>
        /// <param name="childJob">дочерняя работа</param>
        /// <returns></returns>
        private async Task ExecuteChildJob(JobDownload childJob)
        {
            try
            {
                _logger.Info($"Работа childJob начала выполняться - {childJob.DateFrom} {childJob.DateTo}");
                await _audioDownloader.Download(childJob);
                ChangeState(childJob, JobState.Success);
            }
            catch (Exception)
            {
                ChangeState(childJob, JobState.Error);
            }
        }

        /// <summary>
        /// Проверка на то что можно выполнять работу из хранилища работ
        /// </summary>
        /// <returns></returns>
        private bool IsCanExecuteJob()
        {
            return Jobs.Any(j => j.State != JobState.Success) && !IsAlreadyExecuteJob;
        }
    }
}
