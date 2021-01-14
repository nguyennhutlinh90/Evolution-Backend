using Evolution_Backend.DbModels;
using Evolution_Backend.Models;

using Microsoft.Extensions.Options;

namespace Evolution_Backend.Services
{
    public class LogService : ILogService
    {
        private readonly DbContext _dbContext;
        public LogService(IOptions<Configuration> config)
        {
            _dbContext = new DbContext(config);
        }

        public void Warning(string message, params object[] args)
        {
            _dbContext.log.Warning(message, args);
        }

        public void Information(string message, params object[] args)
        {
            _dbContext.log.Information(message, args);
        }

        public void Error(string message, params object[] args)
        {
            _dbContext.log.Error(message, args);
        }

        public void Debug(string message, params object[] args)
        {
            _dbContext.log.Debug(message, args);
        }
    }
}
