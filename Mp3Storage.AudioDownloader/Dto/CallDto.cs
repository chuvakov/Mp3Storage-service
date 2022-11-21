using System.Text.Json.Serialization;

namespace Mp3Storage.AudioDownloader.Dto
{
    public class CallDto
    {
        public long Id { get; set; }

        [JsonPropertyName("file_link")]
        public string[] Links { get; set; }

        [JsonPropertyName("call_date")]
        public string Date { get; set; }
    }
}
