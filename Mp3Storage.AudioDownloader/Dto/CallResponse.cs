using System.Text.Json.Serialization;

namespace Mp3Storage.AudioDownloader.Dto;

public class CallResponse
{
    [JsonPropertyName("data")]
    public CallDto[] Calls { get; set; }
    public string Message { get; set; }
}