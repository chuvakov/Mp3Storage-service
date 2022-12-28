using Mp3Storage.AudioDownloader.Common;
using Mp3Storage.AudioDownloader.Dto;
using Mp3Storage.AudioDownloader.Storage;
using System.Net.Http.Json;

namespace Mp3Storage.AudioDownloader.Api;

public class CoMagicApiClient : ICoMagicApiClient
{
    private readonly HttpClient _httpClient;

    public string Login { get; set; }
    public string Password { get; set; }

    public string SessionKey { get; set; }
    private readonly ISessionKeyStorage _sessionKeyStorage;

    public CoMagicApiClient(HttpClient httpClient, ISessionKeyStorage sessionKeyStorage)
    {
        _httpClient = httpClient;
        _sessionKeyStorage = sessionKeyStorage;
    }

    /// <summary>
    /// Инициализация ключа сессии
    /// </summary>
    /// <param name="sessionKeyStorage"></param>
    public void InitSessionKey(bool isUpdate = false)
    {
        string sessionKey = _sessionKeyStorage.GetSessionKey();

        if (sessionKey is null || isUpdate)
        {
            sessionKey = GetSessionKey().Result;
            _sessionKeyStorage.SetSessionKey(sessionKey);
        }

        SessionKey = sessionKey;
    }

    /// <summary>
    /// Получаем звонки через API
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="isRetry"></param>
    /// <returns></returns>
    /// <exception cref="Mp3StorageException"></exception>
    public async Task<IEnumerable<CallDto>> GetCalls(DateTime from, DateTime to, bool isRetry = false)
    {
        try
        {
            var dateFrom = from.ToString("yyyy-MM-dd") + "%20" + from.ToString("HH:mm:ss");
            var dateEnd = to.ToString("yyyy-MM-dd") + "%20" + to.ToString("HH:mm:ss");

            var request = $"v1/call/?session_key={SessionKey}&date_from={dateFrom}&date_till={dateEnd}";
            var response = await _httpClient.GetFromJsonAsync<CallResponse>(request);

            if (response == null)
            {
                throw new Exception($"Не удалось получить данные о звонках за период: c {from} по {to}");
            }

            if (response.Message == null || !response.Message.Contains("Unauthorized"))
                return response.Calls;

            if (isRetry)
            {
                throw new Exception(
                    $"Не удалось получить данные о звонках за период: c {from} по {to}, после обновления ключа сессии");
            }

            InitSessionKey(true);
            return await GetCalls(from, to, true);
        }
        catch (Exception e)
        {
            if (!e.Message.Contains("504"))
                throw;

            var isConnect = await CheckConnect();
            if (isConnect)
            {
                throw new Mp3StorageException("Слишком много аудио за данный период", ExceptionCode.ResponseOverflowCalls);
            }
            throw;
        }
    }

    //ToDo - нужно ли?
    private async Task<bool> CheckConnect()
    {
        try
        {
            var dateStart = new DateTime(2022, 11, 07, 12, 00, 00);
            var dateEnd = new DateTime(2022, 11, 07, 13, 00, 00);
            var calls = await GetCalls(dateStart, dateEnd);
            return calls.Any();
        }
        catch (Exception)
        {
            return false;
        }

    }

    /// <summary>
    /// Берем ключ сессии через API
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<string> GetSessionKey()
    {
        var response = await _httpClient.GetFromJsonAsync<SessionKeyResponse>($"login/?login={Login}&password={Password}");

        if (response == null)
        {
            throw new Exception("Не удалось получить ключ сессиии");
        }

        return response.SessionKey.Value;
    }
}