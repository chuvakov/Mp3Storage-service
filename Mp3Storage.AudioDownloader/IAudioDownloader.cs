using Mp3Storage.AudioDownloader.Jobs;

namespace Mp3Storage.AudioDownloader;

public interface IAudioDownloader
{
    /// <summary>
    /// Скачивание всех аудио
    /// </summary>
    /// <returns></returns>
    Task Execute(JobDownload job);

    /// <summary>
    /// Скачивание одного аудио по URL
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    Task DownloadAudio(string link, string folderName = null);

    void InitSettings(string login, string password, string pathToStorage);
}