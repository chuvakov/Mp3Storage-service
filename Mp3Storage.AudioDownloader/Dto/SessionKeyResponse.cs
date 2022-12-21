using System.Text.Json.Serialization;

namespace Mp3Storage.AudioDownloader.Dto;

public class SessionKeyResponse
{
    [JsonPropertyName("data")]
    public SessionKeyDto SessionKey { get; set; }
}

public class SessionKeyDto
{
    [JsonPropertyName("session_key")]
    public string Value { get; set; }
}