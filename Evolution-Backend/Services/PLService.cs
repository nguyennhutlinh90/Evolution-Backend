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
    public class PLService : ServiceBase, IPLService
    {
        private readonly DbContext _dbContext;
        public PLService(IOptions<Configuration> config)
        {
            _dbContext = new DbContext(config);
        }

        public async Task<string> Create(PL_Collection PL, IEnumerable<PL_Detail_Collection> PL_Details)
        {
            return await ExecuteAsync(async () =>
            {
                PL.created_on = PL.updated_on = DateTime.Now;

                foreach (var detail in PL_Details)
                {
                    detail.total_boxes = detail.item_details.GroupBy(i => i.box_number).Count();
                    detail.total_packed_boxes = 0;
                    detail.total_expected_qty = detail.item_details.Sum(i => i.expected_qty);
                    detail.total_packed_qty = detail.item_details.Sum(i => i.packed_qty);
                    detail.created_on = detail.updated_on = PL.created_on;
                }

                await _dbContext.pl.InsertOneAsync(PL);

                await _dbContext.pl_detail.InsertManyAsync(PL_Details);
            });
        }

        public async Task<string> Delete(string PLNumber)
        {
            return await ExecuteAsync(async () =>
            {
                await _dbContext.pl.DeleteOneAsync(pl => pl.pl_number == PLNumber);

                await _dbContext.pl_detail.DeleteManyAsync(d => d.pl_number == PLNumber);
            });
        }

        public async Task<string> Update(string PLNumber, PL_Collection PL)
        {
            return await ExecuteAsync(async () =>
            {
                PL.updated_on = DateTime.Now;

                await _dbContext.pl.ReplaceOneAsync(pl => pl.pl_number == PLNumber, PL);
            });
        }

        public async Task<string> UpdateStatus(string PLNumber, int status, string updatedBy)
        {
            return await ExecuteAsync(async () =>
            {
                var updateSetStatus = Builders<PL_Collection>.Update.Set(pl => pl.status, status);
                var updateSetUpdatedBy = Builders<PL_Collection>.Update.Set(pl => pl.updated_by, updatedBy);
                var updateSetUpdatedOn = Builders<PL_Collection>.Update.Set(pl => pl.updated_on, DateTime.Now);
                var updateCombine = Builders<PL_Collection>.Update.Combine(updateSetStatus, updateSetUpdatedBy, updateSetUpdatedOn);
                await _dbContext.pl.UpdateOneAsync(pl => pl.pl_number == PLNumber, updateCombine);
            });
        }

        public async Task<string> SetReady(string PLNumber, string updatedBy)
        {
            return await ExecuteAsync(async () =>
            {
                var updateSetStatus = Builders<PL_Collection>.Update.Set(pl => pl.status, (int)StatusEnums.PL.Ready);
                var updateSetUpdatedBy = Builders<PL_Collection>.Update.Set(pl => pl.updated_by, updatedBy);
                var updateSetUpdatedOn = Builders<PL_Collection>.Update.Set(pl => pl.updated_on, DateTime.Now);
                var updateCombine = Builders<PL_Collection>.Update.Combine(updateSetStatus, updateSetUpdatedBy, updateSetUpdatedOn);
                await _dbContext.pl.UpdateOneAsync(pl => pl.pl_number == PLNumber, updateCombine);

                var updateDetailSetStatus = Builders<PL_Detail_Collection>.Update.Set(pld => pld.status, (int)StatusEnums.PL_PO.Ready);
                var updateDetailSetUpdatedBy = Builders<PL_Detail_Collection>.Update.Set(pld => pld.updated_by, updatedBy);
                var updateDetailSetUpdatedOn = Builders<PL_Detail_Collection>.Update.Set(pld => pld.updated_on, DateTime.Now);
                var updateDetailCombine = Builders<PL_Detail_Collection>.Update.Combine(updateDetailSetStatus, updateDetailSetUpdatedBy, updateDetailSetUpdatedOn);
                await _dbContext.pl_detail.UpdateManyAsync(pld => pld.pl_number == PLNumber, updateDetailCombine);
            });
        }

        public async Task<ServiceResponse<PL_Collection>> Get(string PLNumber)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.pl.FindOneAsync(po => po.pl_number == PLNumber);
            });
        }

        public async Task<ServiceResponse<Dictionary<string, long>>> Count(BsonDocument filter = null, Dictionary<string, BsonDocument> countInfos = null)
        {
            return await ExecuteAsync(async () =>
            {
                var aggregate = _dbContext.pl_detail.Aggregate().AppendStage<BsonDocument>(new BsonDocument("$match", new BsonDocument()));

                if (filter != null)
                    aggregate = aggregate.AppendStage<BsonDocument>(filter);

                aggregate = aggregate.AppendStage<BsonDocument>(
                    new BsonDocument("$lookup",
                        new BsonDocument {
                            { "from", "pl" },
                            { "localField", "pl_number" },
                            { "foreignField", "pl_number" },
                            { "as", "pl" }
                        }
                    )
                );

                aggregate = aggregate.AppendStage<BsonDocument>(
                    new BsonDocument("$unwind", "$pl")
                );

                aggregate = aggregate.AppendStage<BsonDocument>(
                    new BsonDocument("$group",
                        new BsonDocument("_id",
                            new BsonDocument {
                                { "pl_number", "$pl.pl_number" },
                                { "status", "$pl.status" }
                            }
                        )
                    )
                );

                aggregate = aggregate.AppendStage<BsonDocument>(
                    new BsonDocument("$project",
                        new BsonDocument {
                            { "_id", 0 },
                            { "pl_number", "$_id.pl_number" },
                            { "status", "$_id.status" }
                        }
                    )
                );

                var countData = new Dictionary<string, long>();
                if (countInfos != null && countInfos.Any())
                {
                    foreach (var countInfo in countInfos)
                    {
                        var aggregateCount = countInfo.Value == null ? aggregate.Count() : aggregate.AppendStage<BsonDocument>(countInfo.Value).Count();
                        var countResult = await aggregateCount.ResultAsync();
                        countData.Add(countInfo.Key, countResult);
                    }
                }

                return countData;
            });
        }

        public async Task<ServiceReadResponse<T>> Read<T>(IEnumerable<BsonDocument> stages = null, int pageSkip = 0, int pageLimit = int.MaxValue)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.pl.Read<PL_Collection, T>(stages, pageSkip, pageLimit);
            });
        }

        public async Task<ServiceResponse<List<T>>> GetDetails<T>(string PLNumber, bool includeItem = true)
        {
            return await ExecuteAsync(async () =>
            {
                var aggregate = _dbContext.pl_detail.Aggregate()
                    .AppendStage<T>(
                        new BsonDocument("$match",
                            new BsonDocument
                            {
                                { "pl_number", PLNumber }
                            }
                        )
                    );

                if (!includeItem)
                {
                    aggregate = aggregate.AppendStage<T>(
                        new BsonDocument("$project",
                            new BsonDocument {
                                { "items", 0 },
                                { "item_definies", 0 },
                                { "item_details", 0 }
                            }
                        )
                    );
                }

                return await aggregate.ToListAsync();
            });
        }

        public async Task<ServiceResponse<List<T>>> GetDetailsByPO<T>(string PONumber, bool includeItem = true)
        {
            return await ExecuteAsync(async () =>
            {
                var aggregate = _dbContext.pl_detail.Aggregate()
                    .AppendStage<T>(
                        new BsonDocument("$match",
                            new BsonDocument
                            {
                                { "po_number", PONumber }
                            }
                        )
                    );

                if (!includeItem)
                {
                    aggregate = aggregate.AppendStage<T>(
                        new BsonDocument("$project",
                            new BsonDocument {
                                { "items", 0 },
                                { "item_definies", 0 },
                                { "item_details", 0 }
                            }
                        )
                    );
                }

                return await aggregate.ToListAsync();
            });
        }

        public async Task<string> AddDetail(string PLNumber, PL_Detail_Collection PL_Detail)
        {
            return await ExecuteAsync(async () =>
            {
                PL_Detail.total_boxes = PL_Detail.item_details.GroupBy(i => i.box_number).Count();
                PL_Detail.total_packed_boxes = 0;
                PL_Detail.total_expected_qty = PL_Detail.item_details.Sum(i => i.expected_qty);
                PL_Detail.total_packed_qty = PL_Detail.item_details.Sum(i => i.packed_qty);
                PL_Detail.created_on = PL_Detail.updated_on = DateTime.Now;

                var updateSetUpdatedBy = Builders<PL_Collection>.Update.Set(pl => pl.updated_by, PL_Detail.updated_by);
                var updateSetUpdatedOn = Builders<PL_Collection>.Update.Set(pl => pl.updated_on, PL_Detail.updated_on);
                var updateCombine = Builders<PL_Collection>.Update.Combine(updateSetUpdatedBy, updateSetUpdatedOn);
                await _dbContext.pl.UpdateOneAsync(pl => pl.pl_number == PLNumber, updateCombine);

                await _dbContext.pl_detail.InsertOneAsync(PL_Detail);
            });
        }

        public async Task<string> RemoveDetail(string PLNumber, string PONumber, string removedBy)
        {
            return await ExecuteAsync(async () =>
            {
                var updateSetUpdatedBy = Builders<PL_Collection>.Update.Set(pl => pl.updated_by, removedBy);
                var updateSetUpdatedOn = Builders<PL_Collection>.Update.Set(pl => pl.updated_on, DateTime.Now);
                var updateCombine = Builders<PL_Collection>.Update.Combine(updateSetUpdatedBy, updateSetUpdatedOn);
                await _dbContext.pl.UpdateOneAsync(pl => pl.pl_number == PLNumber, updateCombine);

                await _dbContext.pl_detail.DeleteOneAsync(d => d.pl_number == PLNumber && d.po_number == PONumber);
            });
        }

        public async Task<string> UpdateDetail(string PLNumber, string PONumber, PL_Detail_Collection PL_Detail)
        {
            return await ExecuteAsync(async () =>
            {
                PL_Detail.total_boxes = PL_Detail.item_details.GroupBy(i => i.box_number).Count();
                PL_Detail.total_packed_boxes = PL_Detail.item_details.Where(i => i.box_status == (int)StatusEnums.PL_Box.Done).GroupBy(i => i.box_number).Count();
                PL_Detail.total_expected_qty = PL_Detail.item_details.Sum(i => i.expected_qty);
                PL_Detail.total_packed_qty = PL_Detail.item_details.Sum(i => i.packed_qty);
                PL_Detail.updated_on = DateTime.Now;

                var updateSetUpdatedBy = Builders<PL_Collection>.Update.Set(pl => pl.updated_by, PL_Detail.updated_by);
                var updateSetUpdatedOn = Builders<PL_Collection>.Update.Set(pl => pl.updated_on, PL_Detail.updated_on);
                var updateCombine = Builders<PL_Collection>.Update.Combine(updateSetUpdatedBy, updateSetUpdatedOn);
                await _dbContext.pl.UpdateOneAsync(pl => pl.pl_number == PLNumber, updateCombine);

                await _dbContext.pl_detail.ReplaceOneAsync(d => d.pl_number == PLNumber && d.po_number == PONumber, PL_Detail);
            });
        }

        public async Task<string> UpdateDetailStatus(string PLNumber, string PONumber, int status, string updatedBy, bool locked)
        {
            return await ExecuteAsync(async () =>
            {
                DateTime? updatedOn = DateTime.Now;
                var updateSetUpdatedBy = Builders<PL_Collection>.Update.Set(pl => pl.updated_by, updatedBy);
                var updateSetUpdatedOn = Builders<PL_Collection>.Update.Set(pl => pl.updated_on, updatedOn);
                var updateCombine = Builders<PL_Collection>.Update.Combine(updateSetUpdatedBy, updateSetUpdatedOn);
                await _dbContext.pl.UpdateOneAsync(pl => pl.pl_number == PLNumber, updateCombine);

                var updateDetailSetStatus = Builders<PL_Detail_Collection>.Update.Set(d => d.status, status);
                var updateDetailSetUpdatedBy = Builders<PL_Detail_Collection>.Update.Set(d => d.updated_by, updatedBy);
                var updateDetailSetUpdatedOn = Builders<PL_Detail_Collection>.Update.Set(d => d.updated_on, updatedOn);
                var updateDetailSetLockedBy = Builders<PL_Detail_Collection>.Update.Set(d => d.locked_by, locked ? updatedBy : "");
                var updateDetailSetLockedOn = Builders<PL_Detail_Collection>.Update.Set(d => d.locked_on, locked ? updatedOn : null);
                var updateDetailCombine = Builders<PL_Detail_Collection>.Update.Combine(updateDetailSetStatus, updateDetailSetUpdatedBy, updateDetailSetUpdatedOn, updateDetailSetLockedBy, updateDetailSetLockedOn);
                await _dbContext.pl_detail.UpdateOneAsync(d => d.pl_number == PLNumber && d.po_number == PONumber, updateDetailCombine);
            });
        }

        public async Task<ServiceReadResponse<T>> ReadDetail<T>(IEnumerable<BsonDocument> stages = null, int pageSkip = 0, int pageLimit = int.MaxValue)
        {
            return await ExecuteAsync(async () =>
            {
                return await _dbContext.pl_detail.Read<PL_Detail_Collection, T>(stages, pageSkip, pageLimit);
            });
        }

        public async Task<ServiceResponse<T>> GetDetail<T>(string PLNumber, string PONumber, bool includeItem = true)
        {
            return await ExecuteAsync(async () =>
            {
                var aggregate = _dbContext.pl_detail.Aggregate()
                    .AppendStage<T>(
                        new BsonDocument("$match",
                            new BsonDocument
                            {
                                { "pl_number", PLNumber },
                                { "po_number", PONumber }
                            }
                        )
                    );

                if (!includeItem)
                {
                    aggregate = aggregate.AppendStage<T>(
                        new BsonDocument("$project",
                            new BsonDocument {
                                { "items", 0 },
                                { "item_definies", 0 },
                                { "item_details", 0 }
                            }
                        )
                    );
                }

                return await aggregate.FirstOrDefaultAsync();
            });
        }
    }
}
