using AutoMapper;

namespace mp3_storage.Services.Dto;

[AutoMap(typeof(Mp3Storage.Core.Models.File), ReverseMap = true)]
public class FileDto
{
    public int Id { get; set; }
    public DateTime DateTime { get; set; }
    public string FromPhone { get; set; }
    public string ToNumber { get; set; }
    public string SessionNumber { get; set; }
    public string Name { get; set; }
}