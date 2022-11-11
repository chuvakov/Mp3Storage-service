using mp3_storage.Services.Dto;

namespace mp3_storage.Services;

public interface IDownloadService
{
    Task DownloadFileAsync(string url);
    Task<IEnumerable<FileDto>> ShowTable();
    void ConvertFromBytes();
}