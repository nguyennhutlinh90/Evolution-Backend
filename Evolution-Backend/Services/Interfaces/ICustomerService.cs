using Evolution_Backend.DbModels;
using Evolution_Backend.Models;

using MongoDB.Bson;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public interface ICustomerService
    {
        Task<string> Create(Customer_Collection customer);

        Task<string> Delete(string customerCode);

        Task<string> Update(string customerCode, Customer_Collection customer);

        Task<ServiceReadResponse<T>> Read<T>(IEnumerable<BsonDocument> stages = null, int pageSkip = 0, int pageLimit = int.MaxValue);

        Task<ServiceResponse<Customer_Collection>> Get(string customerCode);
    }
}
