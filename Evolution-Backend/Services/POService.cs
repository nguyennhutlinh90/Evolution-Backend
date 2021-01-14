using Evolution_Backend.DbModels;
using Evolution_Backend.Models;

using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Driver;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Evolution_Backend.Services
{
    public class POService : ServiceBase, IPOService
    {
        private readonly DbContext _dbContext;
        public POService(IOptions<Configuration> config)
        {
            _dbContext = new DbContext(config);
        }

        public async Task<string> Create(PO_Collection PO, IEnumerable<PO_Detail_Collection> PO_Details)
        {
            return await ExecuteAsync(async () =>
            {
                PO.total_items = PO_Details.GroupBy(d => new { d.item_number, d.color_number, d.inseam, d.size }).Count();
                PO.total_original_qty = PO_Details.Sum(d => d.original_qty);
                PO.total_original_amt = PO_Details.Sum(d => d.original_qty * d.price);
                PO.total_additional_qty = PO_Details.Sum(d => d.additional_qty);
                PO.total_additional_amt = PO_Details.Sum(d => d.additional_qty * d.price);
                PO.created_on = PO.updated_on = DateTime.Now;

                await _dbContext.po.InsertOneAsync(PO);

                await _dbContext.po_detail.InsertManyAsync(PO_Details);
            });
        }

        public async Task<string> Delete(string PONumber)
        {
            return await ExecuteAsync(async () =>
            {
                await _dbContext.po.DeleteOneAsync(po => po.po_number == PONumber);

                await _dbContext.po_detail.DeleteManyAsync(d => d.po_number == PONumber);
            });
        }

        public async Task<string> Update(string PONumber, PO_Collection PO)
        {
            return await ExecuteAsync(async () =>
            {
                PO.updated_on = DateTime.Now;

                await _dbContext.po.ReplaceOneAsync(po => po.po_number == PONumber, PO);
            });
        }

        public async Task<string> Update(string PONumber, PO_Collection PO, IEnumerable<PO_Detail_Collection> PO_Details)
        {
            return await ExecuteAsync(async () =>
            {
                PO.total_items = PO_Details.GroupBy(d => new { d.item_number, d.color_number, d.inseam, d.size }).Count();
                PO.total_original_qty = PO_Details.Sum(d => d.original_qty);
                PO.total_original_amt = PO_Details.Sum(d => d.original_qty * d.price);
                PO.total_additional_qty = PO_Details.Sum(d => d.additional_qty);
                PO.total_additional_amt = PO_Details.Sum(d => d.additional_qty * d.price);
                PO.updated_on = DateTime.Now;

                await _dbContext.po.ReplaceOneAsync(po => po.po_number == PONumber, PO);

                await _dbContext.po_detail.DeleteManyAsync(d => d.po_number == PONumber);
                await _dbContext.po_detail.InsertManyAsync(PO_Details);
            });
        }

        public async Task<string> UpdateStatus(string PONumber, int status, string updatedBy)
        {
            return await ExecuteAsync(async () =>
            {
                var updateSetStatus = Builders<PO_Collection>.Update.Set(pl => pl.status, status);
                var updateSetUpdatedBy = Builders<PO_Collection>.Update.Set(pl => pl.updated_by, updatedBy);
                var updateSetUpdatedOn = Builders<PO_Collection>.Update.Set(pl => pl.updated_on, DateTime.Now);
                var updateCombine = Builders<PO_Collection>.Update.Combine(updateSetStatus, updateSetUpdatedBy, updateSetUpdatedOn);
                await _dbContext.po.UpdateOneAsync(po => po.po_number == PONumber, updateCombine);
            });
        }

        public async Task<ServiceResponse<Dictionary<string, long>>> Count(BsonDocument filter = null, Dictionary<string, BsonDocument> countInfos = null)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.po.Count(filter, countInfos);
            });
        }

        public async Task<ServiceReadResponse<T>> Read<T>(IEnumerable<BsonDocument> stages = null, int pageSkip = 0, int pageLimit = int.MaxValue)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.po.Read<PO_Collection, T>(stages, pageSkip, pageLimit);
            });
        }

        public async Task<ServiceResponse<PO_Collection>> Get(string PONumber)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.po.FindOneAsync(po => po.po_number == PONumber);
            });
        }

        public async Task<ServiceResponse<IEnumerable<PO_Detail_Collection>>> GetDetails(string PONumber)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.po_detail.FindManyAsync(d => d.po_number == PONumber);
            });
        }

        public async Task<string> UpdateDetail(string PONumber, string barcode, PO_Detail_Collection PO_Detail, string updatedBy)
        {
            return await ExecuteAsync(async () =>
            {
                await _dbContext.po_detail.ReplaceOneAsync(pod => pod.po_number == PONumber && pod.barcode == barcode, PO_Detail);

                var po_details = await _dbContext.po_detail.FindManyAsync(d => d.po_number == PONumber);
                var updateSetTotalOrginalQty = Builders<PO_Collection>.Update.Set(pl => pl.total_original_qty, po_details.Sum(d => d.original_qty));
                var updateSetTotalOrginalAmt = Builders<PO_Collection>.Update.Set(pl => pl.total_original_amt, po_details.Sum(d => d.original_qty * d.price));
                var updateSetTotalAdditionalQty = Builders<PO_Collection>.Update.Set(pl => pl.total_additional_qty, po_details.Sum(d => d.additional_qty));
                var updateSetTotalAdditionalAmt = Builders<PO_Collection>.Update.Set(pl => pl.total_additional_amt, po_details.Sum(d => d.additional_qty * d.price));
                var updateSetUpdatedBy = Builders<PO_Collection>.Update.Set(pl => pl.updated_by, updatedBy);
                var updateSetUpdatedOn = Builders<PO_Collection>.Update.Set(pl => pl.updated_on, DateTime.Now);
                var updateCombine = Builders<PO_Collection>.Update.Combine(updateSetTotalOrginalQty, updateSetTotalOrginalAmt, updateSetTotalAdditionalQty, updateSetTotalAdditionalAmt, updateSetUpdatedBy, updateSetUpdatedOn);
                await _dbContext.po.UpdateOneAsync(po => po.po_number == PONumber, updateCombine);
            });
        }

        public async Task<ServiceResponse<PO_Detail_Collection>> GetDetail(string PONumber, string barcode)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.po_detail.FindOneAsync(pod => pod.po_number == PONumber && pod.barcode == barcode);
            });
        }
    }
}
