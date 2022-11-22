using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Mp3Storage.AudioDownloader;

namespace Mp3StorageService.Models
{
    public class LoggerManager : ILoggerManager
    {
        private readonly ILog _log;
        private readonly ILogger<LogManager> _logger;

        public LoggerManager(ILogger<LogManager> logger)
        {
            _log = LogManager.GetLogger(typeof(LogManager));
            _logger = logger;
        }

        public void Info(string message)
        {
            _log.Info(message);
            _logger.LogInformation(message);
        }

        public void Warning(string message)
        {
            _log.Warn(message);
            _logger.LogWarning(message);
        }

        public void Error(string message, Exception exception)
        {
            _log.Error(message, exception);
            _logger.LogError(message, exception);
        }
    }
}
