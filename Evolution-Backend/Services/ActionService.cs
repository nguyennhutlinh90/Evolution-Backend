using Evolution_Backend.DbModels;
using Evolution_Backend.Models;

using Microsoft.Extensions.Options;

using System;
using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public class ActionService : ServiceBase, IActionService
    {
        private readonly DbContext _dbContext;
        public ActionService(IOptions<Configuration> config)
        {
            _dbContext = new DbContext(config);
        }

        public async Task<string> Create(Action_Collection action)
        {
            return await ExecuteAsync(async () =>
            {
                action.created_on = DateTime.Now;

                await _dbContext.action.InsertOneAsync(action);
            });
        }
    }
}
