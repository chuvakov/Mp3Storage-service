namespace Mp3Storage.Core.Models;

public class File : Entity<int>
{
    public DateTime DateTime { get; set; }
    public string FromPhone { get; set; }
    public string ToNumber { get; set; }
    public string SessionNumber { get; set; }
    public string Name { get; set; }
    public string FullPath { get; set; }
}   