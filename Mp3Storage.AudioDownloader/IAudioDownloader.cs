using Mp3Storage.AudioDownloader.Jobs;

namespace Mp3Storage.AudioDownloader
{
    public interface IAudioDownloader
    {
        /// <summary>
        /// Скачивание всех аудио
        /// </summary>
        /// <returns></returns>
        Task Download(JobDownload job, int? maxRequestDownloadCount, string groupBy);

        /// <summary>
        /// Скачивание одного аудио по URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        Task DownloadAudio(string link, string folderName = null);
    }
}
