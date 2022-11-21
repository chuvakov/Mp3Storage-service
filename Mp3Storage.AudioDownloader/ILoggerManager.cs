namespace Mp3Storage.AudioDownloader
{
    public interface ILoggerManager
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception exception);
    }
}
