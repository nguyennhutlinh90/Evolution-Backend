using Evolution_Backend.DbModels;
using Evolution_Backend.Models;

using MongoDB.Bson;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public interface IUserService
    {
        Task<ServiceResponse<Authentication>> Authentication(string userName, string password);

        Task<string> Create(User_Collection user);

        Task<string> Delete(string userName);

        Task<string> Update(string username, User_Collection user);

        Task<string> UpdateDeviceId(string userName, string deviceId);

        Task<ServiceReadResponse<T>> Read<T>(IEnumerable<BsonDocument> stages = null, int pageSkip = 0, int pageLimit = int.MaxValue);

        Task<ServiceResponse<User_Collection>> Get(string username);

        Task<ServiceResponse<User_Collection>> GetById(string userId);
    }
}
