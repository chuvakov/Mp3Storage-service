namespace Mp3Storage.AudioDownloader.Jobs;

public class JobDownload
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public IList<JobDownload> ChildJobs { get; set; }
    public JobState State { get; set; }
    public int? MaxRequestDownloadCount { get; set; }
    public string GroupBy { get; set; }

    public JobDownload()
    {
        Id = Guid.NewGuid();
        Date = DateTime.Now;
        ChildJobs = new List<JobDownload>();
    }
}