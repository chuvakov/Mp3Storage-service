using Mp3Storage.AudioDownloader.Dto;

namespace Mp3Storage.AudioDownloader.Api;

public interface ICoMagicApiClient
{
    string Login { get; set; }
    string Password { get; set; }

    /// <summary>
    /// Текущий ключ сессии
    /// </summary>
    string SessionKey { get; set; }

    /// <summary>
    /// Получение нового ключа сессии
    /// </summary>
    /// <returns>ключ сессии</returns>
    Task<string> GetSessionKey();

    /// <summary>
    /// Получение информации о звонках
    /// </summary>
    /// <returns>информации о звонках</returns>
    Task<IEnumerable<CallDto>> GetCalls(DateTime from, DateTime to, bool isRetry = false);

    void InitSessionKey(bool isUpdate = false);
}