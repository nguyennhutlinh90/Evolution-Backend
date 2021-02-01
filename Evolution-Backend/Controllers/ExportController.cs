using Evolution_Backend.Models;
using Evolution_Backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MongoDB.Bson;

using System.Threading.Tasks;

namespace Evolution_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/export")]
    public class ExportController : ApiControllerBase
    {
        private readonly IPLService _PLService;
        public ExportController(IPLService PLService)
        {
            _PLService = PLService;
        }

        [HttpGet]
        [Route("getpackinglist")]
        public async Task<ActionResult> GetPackingList(string PLNumber)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(PLNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PL number"));

                var stages = new BsonDocument[] {
                    new BsonDocument("$match", new BsonDocument("pl_number", PLNumber)),
                    new BsonDocument("$project", new BsonDocument {
                        { "_id", 0 },
                        { "po_number", "$po_number" },
                        { "use_produce_qty", "$use_produce_qty" },
                        { "item_number", new BsonDocument("$arrayElemAt", new BsonArray { "$items.item_number", 0 }) },
                        { "item_details", "$item_details" }
                    }),
                    new BsonDocument("$lookup", new BsonDocument {
                        { "from", "item" },
                        { "localField", "item_number" },
                        { "foreignField", "item_number" },
                        { "as", "item" }
                    }),
                    new BsonDocument("$lookup", new BsonDocument {
                        { "from", "po" },
                        { "localField", "po_number" },
                        { "foreignField", "po_number" },
                        { "as", "po_master" }
                    }),
                    new BsonDocument("$lookup", new BsonDocument {
                        { "from", "po_detail" },
                        { "let", new BsonDocument("po_number", "$po_number") },
                        { "pipeline",
                            new BsonArray {
                                new BsonDocument("$match",
                                    new BsonDocument("$expr",
                                        new BsonDocument("$eq", new BsonArray {
                                            "$po_number",
                                            "$$po_number"
                                        })
                                    )
                                ),
                                new BsonDocument("$sort", new BsonDocument {
                                    { "item_number", 1 },
                                    { "color_number", 1 },
                                    { "inseam", 1 },
                                    { "size", 1 }
                                })
                            }
                        },
                        { "as", "po_details" }
                    }),
                    new BsonDocument("$project", new BsonDocument {
                        { "_id", 0 },
                        { "po_number", "$po_number" },
                        { "use_produce_qty", "$use_produce_qty" },
                        { "packing", new BsonDocument("$arrayElemAt", new BsonArray { "$po_master.packing", 0 }) },
                        { "ship", new BsonDocument("$arrayElemAt", new BsonArray { "$po_master.ship", 0 }) },
                        { "season", new BsonDocument("$arrayElemAt", new BsonArray { "$item.season", 0 }) },
                        { "po_details", "$po_details" },
                        { "item_details", "$item_details" }
                    })
                };

                var srRead = await _PLService.ReadDetail<object>(stages);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL items error: {0}", srRead.ErrorMessage));

                return new ApiResponse(srRead.Datas);
            });
        }

        [HttpGet]
        [Route("getshippingmark")]
        public async Task<ActionResult> GetShippingMark(string PLNumber, string PONumber, string boxNumber)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(PLNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PL number"));

                if (string.IsNullOrEmpty(PONumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PO number"));

                if (string.IsNullOrEmpty(boxNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Box number"));

                var stages = new BsonDocument[] {
                    new BsonDocument("$match",
                        new BsonDocument {
                            { "pl_number", PLNumber },
                            { "po_number", PONumber }
                        }
                    ),
                    new BsonDocument("$unwind", "$item_details"),
                    new BsonDocument("$match", new BsonDocument("item_details.box_number", boxNumber)),
                    new BsonDocument("$group",
                        new BsonDocument {
                            { "_id", 
                                new BsonDocument {
                                    { "po_number", "$po_number" },
                                    { "box_number", "$item_details.box_number" },
                                    { "box_dimension", "$item_details.box_dimension" },
                                    { "item_number", "$item_details.item_number" },
                                    { "item_description", "$item_details.item_description" },
                                    { "color_number", "$item_details.color_number" },
                                    { "color_description", "$item_details.color_description" },
                                    { "inseam", "$item_details.inseam" }
                                } 
                            },
                            { "extras", new BsonDocument("$push",
                                    new BsonDocument {
                                        { "size", "$item_details.size" },
                                        { "expected_qty", "$item_details.expected_qty" },
                                        { "packed_qty", "$item_details.packed_qty" }
                                    }
                                ) 
                            }
                        }
                    ),
                    new BsonDocument("$group",
                        new BsonDocument {
                            { "_id",
                                new BsonDocument {
                                    { "po_number", "$_id.po_number" },
                                    { "box_number", "$_id.box_number" },
                                    { "box_dimension", "$_id.box_dimension" }
                                } 
                            },
                            { "item_details", new BsonDocument("$push",
                                    new BsonDocument {
                                        { "item_number", "$_id.item_number" },
                                        { "item_description", "$_id.item_description" },
                                        { "color_number", "$_id.color_number" },
                                        { "color_description", "$_id.color_description" },
                                        { "inseam", "$_id.inseam" },
                                        { "extras", "$extras" }
                                    }
                                ) 
                            }
                        }
                    ),
                    new BsonDocument("$lookup",
                        new BsonDocument
                        {
                            { "from", "po_detail" },
                            { "localField", "_id.po_number" },
                            { "foreignField", "po_number" },
                            { "as", "po_details" }
                        }
                    ),
                    new BsonDocument("$project",
                        new BsonDocument
                        {
                            { "_id", 0 },
                            { "po_number", "$_id.po_number" },
                            { "box_number", "$_id.box_number" },
                            { "box_dimension", "$_id.box_dimension" },
                            { "item_sizes", "$po_details.size" },
                            { "item_details", "$item_details" }
                        }
                    )
                };

                var srRead = await _PLService.ReadDetail<object>(stages);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL items error: {0}", srRead.ErrorMessage));

                return new ApiResponse(srRead.Datas != null && srRead.Datas.Count > 0 ? srRead.Datas[0] : null);
            });
        }
    }
}
