using Mp3Storage.AudioDownloader.Dto;

namespace Mp3Storage.AudioDownloader.Storage;

public interface ILinkStorage
{
    void Add(string link);
    string[] GetLinksNotExist(IEnumerable<CallDto> calls);
}