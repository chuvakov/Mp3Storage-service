using Mp3Storage.AudioDownloader;
using Mp3Storage.AudioDownloader.Extentions;
using Mp3Storage.AudioDownloader.Jobs;
using Mp3Storage.AudioDownloader.Utils;

namespace Mp3StorageService.Models
{
    public class JobFileStorage : IJobStorage
    {
        private readonly string _pathToFile;
        private readonly ILoggerManager _logger;
        private readonly IAudioDownloader _audioDownloader;
        private List<JobDownload> Jobs { get; set; } = new List<JobDownload>();
        private static object _lock = new object();
        public static bool IsAlreadyExecuteJob;

        public JobFileStorage(IAudioDownloader audioDownloader, ILoggerManager logger, IConfiguration configuration)
        {
            _audioDownloader = audioDownloader;
            _logger = logger;
            _pathToFile = FileUtility.GetPathTo("jobStorage.json");

            InitAudioDownloader(configuration);

            FileUtility.CreateFileIfNotExist(_pathToFile, Jobs.ToJson());
            InitJobs();
        }

        /// <summary>
        /// Настройка AudioDownloader
        /// </summary>
        /// <param name="configuration"></param>
        private void InitAudioDownloader(IConfiguration configuration)
        {
            string login = configuration["App:CoMagicApi:Login"];
            string password = configuration["App:CoMagicApi:Password"];
            string pathToStorage = configuration["App:PathToStorage"];

            _audioDownloader.InitSettings(login, password, pathToStorage);
        }

        /// <summary>
        /// Инициализация списка задач на уровне класса из файла
        /// </summary>
        private void InitJobs()
        {
            var text = File.ReadAllText(_pathToFile);
            Jobs = text.FromJson<List<JobDownload>>();
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
            File.WriteAllText(_pathToFile, Jobs.ToJson());
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

                IsAlreadyExecuteJob = false;
            }
            catch (Exception e)
            {
                IsAlreadyExecuteJob = false;
                _logger.Error($"Ошибка при выполнении работы", e);
                ChangeState(job, JobState.Error);
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

            if (job.ChildJobs.All(cj => cj.State == JobState.Success))
            {
                ChangeState(job, JobState.Success);
            }

            if (job.ChildJobs.Any(cj => cj.State == JobState.Error))
            {
                ChangeState(job, JobState.Error);
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
                await _audioDownloader.Execute(childJob);
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
