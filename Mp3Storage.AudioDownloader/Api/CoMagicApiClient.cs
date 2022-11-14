using Microsoft.Extensions.Configuration;
using Mp3Storage.AudioDownloader.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Mp3Storage.AudioDownloader.Api
{
    public class CoMagicApiClient : ICoMagicApiClient
    {
        private readonly HttpClient _httpClient;

        public string Login { get; set; }
        public string Password { get; set; }

        public string SessionKey { get; set; }
        public event ICoMagicApiClient.SessionKeyChangeHandler SessionKeyChange;

        public CoMagicApiClient(HttpClient httpClient)
        {
            _httpClient=httpClient;
        }

        public async Task<IEnumerable<CallDto>> GetCalls(DateTime from, DateTime to)
        {
            var dateFrom = from.ToString("yyyy-MM-dd") + "%20" + from.ToString("HH:mm:ss");
            var dateEnd = to.ToString("yyyy-MM-dd") + "%20" + to.ToString("HH:mm:ss");

            var request = $"v1/call/?session_key={SessionKey}&date_from={dateFrom}&date_till={dateEnd}";
            var response = await _httpClient.GetFromJsonAsync<CallResponse>(request);

            if (response == null)
            {
                throw new Exception($"Не удалось получить данные о звонках за период: c {from} по {to}");
            }

            if (response.Message != null && response.Message.Contains("Unauthorized"))
            {
                SessionKey = await GetSessionKey();
                SessionKeyChange?.Invoke(SessionKey);

                request = $"v1/call/?session_key={SessionKey}&date_from={dateFrom}&date_till={dateEnd}";
                response = await _httpClient.GetFromJsonAsync<CallResponse>(request);

                if (response == null)
                {
                    throw new Exception($"Не удалось получить данные о звонках за период: c {from} по {to}");
                }
            }

            return response.Calls;
        }

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
}
