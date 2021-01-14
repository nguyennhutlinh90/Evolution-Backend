using Evolution_Backend.DbModels;
using Evolution_Backend.Models;

using MongoDB.Bson;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public interface IItemService
    {
        Task<string> Create(Item_Collection item);

        Task<string> Delete(string itemNumber, string colorNumber, string inseam, string size);

        Task<string> Update(string itemNumber, string colorNumber, string inseam, string size, Item_Collection item);

        Task<ServiceReadResponse<T>> Read<T>(IEnumerable<BsonDocument> stages = null, int pageSkip = 0, int pageLimit = int.MaxValue);

        Task<ServiceResponse<Item_Collection>> Get(string itemNumber, string colorNumber, string inseam, string size);

        Task<ServiceResponse<Item_Collection>> GetBarcode(string barcode);
    }
}
