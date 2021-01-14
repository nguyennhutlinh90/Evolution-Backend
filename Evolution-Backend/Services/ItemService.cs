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
    public class ItemService : ServiceBase, IItemService
    {
        private readonly DbContext _dbContext;
        public ItemService(IOptions<Configuration> config)
        {
            _dbContext = new DbContext(config);
        }

        public async Task<string> Create(Item_Collection item)
        {
            return await ExecuteAsync(async () =>
            {
                item.created_on = item.updated_on = DateTime.Now;

                await _dbContext.item.InsertOneAsync(item);
            });
        }

        public async Task<string> Delete(string itemNumber, string colorNumber, string inseam, string size)
        {
            return await ExecuteAsync(async () =>
            {
                await _dbContext.item.DeleteOneAsync(ib => ib.item_number == itemNumber && ib.color_number == colorNumber && ib.inseam == inseam && ib.size == size);
            });
        }

        public async Task<string> Update(string itemNumber, string colorNumber, string inseam, string size, Item_Collection item)
        {
            return await ExecuteAsync(async () =>
            {
                item.updated_on = DateTime.Now;

                await _dbContext.item.ReplaceOneAsync(ib => ib.item_number == itemNumber && ib.color_number == colorNumber && ib.inseam == inseam && ib.size == size, item);
            });
        }

        public async Task<ServiceReadResponse<T>> Read<T>(IEnumerable<BsonDocument> stages = null, int pageSkip = 0, int pageLimit = int.MaxValue)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.item.Read<Item_Collection, T>(stages, pageSkip, pageLimit);
            });
        }

        public async Task<ServiceResponse<Item_Collection>> Get(string itemNumber, string colorNumber, string inseam, string size)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.item.FindOneAsync(ib => ib.item_number == itemNumber && ib.color_number == colorNumber && ib.inseam == inseam && ib.size == size);
            });
        }

        public async Task<ServiceResponse<Item_Collection>> GetBarcode(string barcode)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.item.FindOneAsync(ib => ib.barcode == barcode);
            });
        }
    }
}
