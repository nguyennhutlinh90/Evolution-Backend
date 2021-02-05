using Evolution_Backend.Models;
using Evolution_Backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MongoDB.Bson;

using System.Linq;
using System.Threading.Tasks;

namespace Evolution_Backend.Controllers
{
    [Authorize(Roles = Constants.Role.Admin)]
    [ApiController]
    [Route("api/stockcheck")]
    public class StockCheckController : ApiControllerBase
    {
        private readonly IItemService _itemService;
        private readonly IPOService _POService;
        public StockCheckController(IItemService itemService, IPOService POService)
        {
            _itemService = itemService;
            _POService = POService;
        }

        [HttpPost]
        [Route("read")]
        public async Task<ActionResult> Read([FromBody] ReadRequest request)
        {
            return await ExecuteReadAsync(async () =>
            {
                if (request == null)
                    request = new ReadRequest();

                if (request.Sorts == null || !request.Sorts.Any())
                    request.Sorts.Add(new SortRequest { Name = "created_on", Type = "desc" });

                var stages = new BsonDocument[] {
                    request.Filters.CreateFilter(),
                    new BsonDocument("$unwind", "$items"),
                    new BsonDocument("$group", new BsonDocument {
                        { "_id",
                            new BsonDocument {
                                { "item_number", "$items.item_number" },
                                { "item_description", "$items.item_description" }
                            }
                        },
                        { "pos",
                            new BsonDocument("$push", new BsonDocument {
                                { "po_number", "$po_number" },
                                { "created_on", "$created_on" }
                            })
                        }
                    }),
                    new BsonDocument("$project", new BsonDocument {
                        { "item_number", "$_id.item_number" },
                        { "item_description", "$_id.item_description" },
                        { "created_on", new BsonDocument("$max", "$pos.created_on") },
                        { "pos", "$pos" }
                    }),
                    request.Sorts.CreateSort()
                };

                var srRead = await _POService.Read<object>(stages, request.PageSkip, request.PageLimit);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiReadResponse((int)ApiError.DbError, string.Format("Get stock check error: {0}", srRead.ErrorMessage));

                return new ApiReadResponse(srRead.Datas, srRead.Total);
            });
        }

        [HttpPost]
        [Route("readdetail")]
        public async Task<ActionResult> ReadDetail([FromBody] StockDetailReadRequest request)
        {
            return await ExecuteReadAsync(async () =>
            {
                if (request == null)
                    request = new StockDetailReadRequest();

                if (string.IsNullOrEmpty(request.ItemNumber))
                    return new ApiReadResponse((int)ApiError.Required, ApiError.Required.GetDecription("Item number"));

                if (request.Sorts == null || !request.Sorts.Any())
                {
                    request.Sorts.Add(new SortRequest { Name = "po_number", Type = "asc" });
                    request.Sorts.Add(new SortRequest { Name = "color_description", Type = "asc" });
                    request.Sorts.Add(new SortRequest { Name = "inseam", Type = "asc" });
                    request.Sorts.Add(new SortRequest { Name = "size", Type = "asc" });
                }

                var stages = new BsonDocument[] {
                    new BsonDocument("$match", new BsonDocument("item_number", request.ItemNumber)),
                    request.Filters.CreateFilter(),
                    request.Sorts.CreateSort()
                };

                var stageAfters = new BsonDocument[] {
                    new BsonDocument("$lookup", new BsonDocument {
                        { "from", "pl_detail" },
                        { "let",
                            new BsonDocument {
                                { "po_number", "$po_number" },
                                { "barcode", "$barcode" }
                            }
                        },
                        { "pipeline",
                            new BsonArray {
                                new BsonDocument("$match",
                                    new BsonDocument("$expr",
                                        new BsonDocument("$and", new BsonArray {
                                            new BsonDocument("$in", new BsonArray {
                                                "$status",
                                                new BsonArray { (int)StatusEnums.PL_PO.PartialPacked, (int)StatusEnums.PL_PO.Packed, (int)StatusEnums.PL_PO.Shipped }
                                            }),
                                            new BsonDocument("$eq", new BsonArray { "$use_produce_qty", request.ShowProduceQty })
                                        })
                                    )
                                ),
                                new BsonDocument("$unwind", "$item_details"),
                                new BsonDocument("$match",
                                    new BsonDocument("$expr",
                                        new BsonDocument("$and", new BsonArray {
                                            new BsonDocument("$eq", new BsonArray { "$po_number", "$$po_number" }),
                                            new BsonDocument("$eq", new BsonArray { "$item_details.barcode", "$$barcode" }),
                                            new BsonDocument("$eq", new BsonArray { "$item_details.box_status", (int)StatusEnums.PL_Box.Done })
                                        })
                                    )
                                ),
                                new BsonDocument("$project", new BsonDocument {
                                    { "_id", 0 },
                                    { "scanned_qty", new BsonDocument("$sum", "$item_details.packed_qty") }
                                })
                            }
                        },
                        { "as", "scanned_qtys" }
                    }),
                    new BsonDocument("$project", new BsonDocument {
                        { "_id", 0 },
                        { "po_number", 1 },
                        { "item_number", 1 },
                        { "item_description", 1 },
                        { "color_number", 1 },
                        { "color_description", 1 },
                        { "inseam", 1 },
                        { "size", 1 },
                        { "barcode", 1 },
                        { "original_qty", 1 },
                        { "additional_qty", 1 },
                        { "scanned_qty", new BsonDocument("$sum", "$scanned_qtys.scanned_qty") }
                    })
                };

                var srRead = await _POService.ReadDetail<object>(stages, request.PageSkip, request.PageLimit, stageAfters);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiReadResponse((int)ApiError.DbError, string.Format("Get stock detail error: {0}", srRead.ErrorMessage));

                var itemStages = new BsonDocument[] {
                    new BsonDocument("$match", new BsonDocument("item_number", request.ItemNumber)),
                    new BsonDocument("$project", new BsonDocument {
                        { "_id", 0 },
                        { "item_number", 1 },
                        { "item_description", 1 },
                        { "color_number", 1 },
                        { "color_description", 1 },
                        { "inseam", 1 },
                        { "size", 1 },
                        { "barcode", 1 }
                    })
                };

                var srReadItem = await _itemService.Read<object>(itemStages);
                if (!string.IsNullOrEmpty(srReadItem.ErrorMessage))
                    return new ApiReadResponse((int)ApiError.DbError, string.Format("Get item info error: {0}", srReadItem.ErrorMessage));

                return new ApiReadResponse(new { items = srReadItem.Datas, stocks = srRead.Datas }, srRead.Total);
            });
        }
    }
}
