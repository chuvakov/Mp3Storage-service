namespace Mp3Storage.AudioDownloader.Jobs;

public interface IJobStorage
{
    void AddJob(JobDownload job);
    Task ExecuteFirstJob();

    void ChangeState(JobDownload job, JobState state);
}