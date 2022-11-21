using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Mp3StorageService.Models
{
    public class LoggerManager
    {
        private readonly ILog _log;
        private readonly ILogger _logger;

        public LoggerManager(ILog log, ILogger logger)
        {
            _log = log;
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
