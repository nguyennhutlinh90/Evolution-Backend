using Evolution_Backend.DbModels;
using Evolution_Backend.Models;

using MongoDB.Bson;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public interface IPLService
    {
        Task<string> Create(PL_Collection PL, IEnumerable<PL_Detail_Collection> PL_Details);

        Task<string> Delete(string PLNumber);

        Task<string> Update(string PLNumber, PL_Collection PL);

        Task<string> UpdateStatus(string PLNumber, int status, string updatedBy);

        Task<string> SetReady(string PLNumber, string updatedBy);

        Task<ServiceResponse<Dictionary<string, long>>> Count(BsonDocument filter = null, Dictionary<string, BsonDocument> countInfos = null);

        Task<ServiceReadResponse<T>> Read<T>(IEnumerable<BsonDocument> stages = null, int pageSkip = 0, int pageLimit = int.MaxValue);

        Task<ServiceResponse<PL_Collection>> Get(string PLNumber);

        Task<ServiceResponse<List<T>>> GetDetails<T>(string PLNumber, bool includeItem = true);

        Task<ServiceResponse<List<T>>> GetDetailsByPO<T>(string PONumber, bool includeItem = true);

        Task<string> AddDetail(string PLNumber, PL_Detail_Collection PL_Detail);

        Task<string> RemoveDetail(string PLNumber, string PONumber, string removedBy);

        Task<string> UpdateDetail(string PLNumber, string PONumber, PL_Detail_Collection PL_Detail);

        Task<string> UpdateDetailStatus(string PLNumber, string PONumber, int status, string updatedBy, bool locked);

        Task<ServiceReadResponse<T>> ReadDetail<T>(IEnumerable<BsonDocument> stages = null, int pageSkip = 0, int pageLimit = int.MaxValue);

        Task<ServiceResponse<T>> GetDetail<T>(string PLNumber, string PONumber, bool includeItem = true);
    }
}
