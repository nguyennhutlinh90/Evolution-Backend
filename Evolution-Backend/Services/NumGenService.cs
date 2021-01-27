using Evolution_Backend.DbModels;
using Evolution_Backend.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

using System;
using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public class NumGenService : ServiceBase, INumGenService
    {
        private readonly DbContext _dbContext;
        public NumGenService(IOptions<Configuration> config)
        {
            _dbContext = new DbContext(config);
        }

        public async Task<string> Create(Num_Gen_Collection numGen)
        {
            return await ExecuteAsync(async () =>
            {
                numGen.created_on = numGen.updated_on = DateTime.Now;

                await _dbContext.num_gen.InsertOneAsync(numGen);
            });
        }

        public async Task<string> Update(string genType, Num_Gen_Collection numGen)
        {
            return await ExecuteAsync(async () =>
            {
                numGen.updated_on = DateTime.Now;

                await _dbContext.num_gen.ReplaceOneAsync(ng => ng.gen_type == genType, numGen);
            });
        }

        public async Task<ServiceResponse<Num_Gen_Collection>> Get(string genType)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.num_gen.FindOneAsync(ng => ng.gen_type == genType);
            });
        }
    }
}
