namespace Mp3Storage.AudioDownloader.Common;
public class Mp3StorageException : Exception
{
    public ExceptionCode Code { get; }

    public Mp3StorageException(string message, ExceptionCode code) : base(message)
    {
        Code = code;
    }
}
