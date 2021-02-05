using Evolution_Backend.DbModels;
using Evolution_Backend.Models;

using MongoDB.Bson;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public interface IPOService
    {
        Task<string> Create(PO_Collection PO, IEnumerable<PO_Detail_Collection> PO_Details);

        Task<string> Delete(string PONumber);

        Task<string> Update(string PONumber, PO_Collection PO);

        Task<string> Update(string PONumber, PO_Collection PO, IEnumerable<PO_Detail_Collection> PO_Details);

        Task<string> UpdateStatus(string PONumber, int status, string updatedBy);

        Task<ServiceResponse<Dictionary<string, long>>> Count(BsonDocument filter = null, Dictionary<string, BsonDocument> countInfos = null);

        Task<ServiceReadResponse<T>> Read<T>(IEnumerable<BsonDocument> stages = null, int pageSkip = 0, int pageLimit = int.MaxValue);

        Task<ServiceResponse<PO_Collection>> Get(string PONumber);

        Task<ServiceResponse<IEnumerable<PO_Detail_Collection>>> GetDetails(string PONumber);

        Task<string> UpdateDetail(string PONumber, string barcode, PO_Detail_Collection PO_Detail, string updatedBy);

        Task<ServiceReadResponse<T>> ReadDetail<T>(IEnumerable<BsonDocument> stages = null, int pageSkip = 0, int pageLimit = int.MaxValue, IEnumerable<BsonDocument> stageAfters = null);

        Task<ServiceResponse<PO_Detail_Collection>> GetDetail(string PONumber, string barcode);
    }
}
