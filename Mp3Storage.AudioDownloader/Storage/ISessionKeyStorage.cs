namespace Mp3Storage.AudioDownloader.Storage;

public interface ISessionKeyStorage
{
    string GetSessionKey();
    void SetSessionKey(string sessionKey);
}