using Evolution_Backend.DbModels;
using Evolution_Backend.Models;

using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Driver;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public class CustomerService : ServiceBase, ICustomerService
    {
        private readonly DbContext _dbContext;
        public CustomerService(IOptions<Configuration> config)
        {
            _dbContext = new DbContext(config);
        }

        public async Task<string> Create(Customer_Collection customer)
        {
            return await ExecuteAsync(async () =>
            {
                customer.created_on = customer.updated_on = DateTime.Now;

                await _dbContext.customer.InsertOneAsync(customer);
            });
        }

        public async Task<string> Delete(string customerCode)
        {
            return await ExecuteAsync(async () =>
            {
                await _dbContext.customer.DeleteOneAsync(c => c.customer_code == customerCode);
            });
        }

        public async Task<string> Update(string customerCode, Customer_Collection customer)
        {
            return await ExecuteAsync(async () =>
            {
                customer.updated_on = DateTime.Now;

                await _dbContext.customer.ReplaceOneAsync(c => c.customer_code == customerCode, customer);
            });
        }

        public async Task<ServiceReadResponse<T>> Read<T>(IEnumerable<BsonDocument> stages = null, int pageSkip = 0, int pageLimit = int.MaxValue)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.customer.Read<Customer_Collection, T>(stages, pageSkip, pageLimit);
            });
        }

        public async Task<ServiceResponse<Customer_Collection>> Get(string customerCode)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.customer.FindOneAsync(c => c.customer_code == customerCode);
            });
        }
    }
}
