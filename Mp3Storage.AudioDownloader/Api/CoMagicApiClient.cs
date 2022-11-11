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

        private readonly string _login;
        private readonly string _password;

        public string SessionKey { get; set; }
        public delegate void SessionKeyChangeHandler(string sessionKey);
        public event SessionKeyChangeHandler SessionKeyChange;

        public CoMagicApiClient(HttpClient httpClient, string login, string password)
        {
            _httpClient = httpClient;
            _login = login;
            _password = password;

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<IEnumerable<CallDto>> GetCalls(DateTime from, DateTime to)
        {
            var dateFrom = from.ToString("yyyy-MM-dd") + "%20" + from.ToString("hh:mm:ss");
            var dateEnd = to.ToString("yyyy-MM-dd") + "%20" + to.ToString("hh:mm:ss");

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
            var response = await _httpClient.GetFromJsonAsync<SessionKeyResponse>($"login/?login={_login}&password={_password}");

            if (response == null)
            {
                throw new Exception("Не удалось получить ключ сессиии");
            }

            return response.SessionKey.Value;
        }
    }
}
