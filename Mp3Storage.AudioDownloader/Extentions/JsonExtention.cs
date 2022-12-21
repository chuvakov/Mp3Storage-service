using Newtonsoft.Json;

namespace Mp3Storage.AudioDownloader.Extentions;

public static class JsonExtention
{
    public static string ToJson(this object obj) => JsonConvert.SerializeObject(obj);
    public static T FromJson<T>(this string json) => JsonConvert.DeserializeObject<T>(json);
}