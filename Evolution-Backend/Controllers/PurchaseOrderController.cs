using Evolution_Backend.DbModels;
using Evolution_Backend.Models;
using Evolution_Backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MongoDB.Bson;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Evolution_Backend.Controllers
{
    //[ApiExplorerSettings(IgnoreApi = true)]
    [Authorize]
    [ApiController]
    [Route("api/purchaseorder")]
    public class PurchaseOrderController : ApiControllerBase
    {
        private readonly IActionService _actionService;
        private readonly IItemService _itemService;
        private readonly IPOService _POService;
        public PurchaseOrderController(IActionService actionService, IItemService itemService, IPOService POService)
        {
            _actionService = actionService;
            _itemService = itemService;
            _POService = POService;
        }

        [HttpPost]
        [Route("upload")]
        public async Task<ActionResult> Upload([FromBody] List<PO_Upload_Request> request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null || !request.Any())
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (request.Any(po => string.IsNullOrEmpty(po.PONumber)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("PO number(s)"));

                if (request.Any(po => string.IsNullOrEmpty(po.PODate)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("PO date(s)"));

                var errors = new List<string>();
                var saveds = new List<PO_Collection>();
                foreach (var po in request)
                {
                    if (saveds.Any(s => s.po_number == po.PONumber))
                    {
                        errors.Add(ApiError.Dupplicated.GetDecription(string.Format("PO '{0}'", po.PONumber)));
                        continue;
                    }

                    if (po.Details == null || !po.Details.Any())
                    {
                        errors.Add(string.Format("PO '{0}': {1}", po.PONumber, ApiError.Requireds.GetDecription("Detail(s)")));
                        continue;
                    }

                    if (po.Details.Any(i => string.IsNullOrEmpty(i.ItemNumber)))
                    {
                        errors.Add(string.Format("PO '{0}': {1}", po.PONumber, ApiError.Requireds.GetDecription("Item number(s)")));
                        continue;
                    }

                    if (po.Details.Any(i => string.IsNullOrEmpty(i.ItemDescription)))
                    {
                        errors.Add(string.Format("PO '{0}': {1}", po.PONumber, ApiError.Requireds.GetDecription("Item description(s)")));
                        continue;
                    }

                    if (po.Details.Any(i => string.IsNullOrEmpty(i.ColorNumber)))
                    {
                        errors.Add(string.Format("PO '{0}': {1}", po.PONumber, ApiError.Requireds.GetDecription("Color number(s)")));
                        continue;
                    }

                    if (po.Details.Any(i => string.IsNullOrEmpty(i.ColorDescription)))
                    {
                        errors.Add(string.Format("PO '{0}': {1}", po.PONumber, ApiError.Requireds.GetDecription("Color description(s)")));
                        continue;
                    }

                    if (po.Details.Any(i => string.IsNullOrEmpty(i.Inseam)))
                    {
                        errors.Add(string.Format("PO '{0}': {1}", po.PONumber, ApiError.Requireds.GetDecription("Inseam(s)")));
                        continue;
                    }

                    if (po.Details.Any(i => string.IsNullOrEmpty(i.Size)))
                    {
                        errors.Add(string.Format("PO '{0}': {1}", po.PONumber, ApiError.Requireds.GetDecription("Size(s)")));
                        continue;
                    }

                    if (po.Details.Any(i => i.OriginalQty <= 0))
                    {
                        errors.Add(string.Format("PO '{0}': Original qty must be greater than 0", po.PONumber));
                        continue;
                    }

                    if (po.Details.Any(i => i.AdditionalQty <= 0))
                    {
                        errors.Add(string.Format("PO '{0}': Additional qty must be greater than 0", po.PONumber));
                        continue;
                    }

                    if (po.Details.Any(i => i.AdditionalQty < i.OriginalQty))
                    {
                        errors.Add(string.Format("PO '{0}': Additional qty must be greater than original qty", po.PONumber));
                        continue;
                    }

                    if (po.Details.Any(i => i.Price <= 0))
                    {
                        errors.Add(string.Format("PO '{0}': Price must be greater than 0", po.PONumber));
                        continue;
                    }

                    var srGet = await _POService.Get(po.PONumber);
                    if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    {
                        errors.Add(string.Format("Get PO '{0}' error: {1}", po.PONumber, srGet.ErrorMessage));
                        continue;
                    }

                    if (srGet.Data != null)
                    {
                        if (srGet.Data.status != (int)StatusEnums.PO.Open)
                        {
                            var POStatusCode = srGet.Data.status.GetName<StatusEnums.PO>();
                            errors.Add(string.Format("PO '{0}' status is '{1}', can not re-upload", po.PONumber, POStatusCode));
                            continue;
                        }
                    }

                    var GB_Items = po.Details.GroupBy(d => new { d.ItemNumber, d.ItemDescription });
                    var PO_Items = GB_Items.Select(gbi => new PO_Item_Collection
                    {
                        item_number = gbi.Key.ItemNumber,
                        item_description = gbi.Key.ItemDescription
                    }).ToList();

                    var GB_Details = po.Details.GroupBy(d => new { d.ItemNumber, d.ItemDescription, d.ColorNumber, d.ColorDescription, d.Inseam, d.Size, d.Price, d.TariffNumber, d.Quality, d.Material });
                    var PO_Details = new List<PO_Detail_Collection>();
                    foreach (var GB_Detail in GB_Details)
                    {
                        var barcode = getBarcode(GB_Detail.Key.ItemNumber, GB_Detail.Key.ColorNumber, GB_Detail.Key.Inseam, GB_Detail.Key.Size);
                        if (string.IsNullOrEmpty(barcode))
                        {
                            errors.Add(string.Format("Item '{0}' - color '{1}' - inseam '{2}' - size '{3}' does not have barcode", GB_Detail.Key.ItemNumber, GB_Detail.Key.ColorNumber, GB_Detail.Key.Inseam, GB_Detail.Key.Size));
                            break;
                        }

                        PO_Details.Add(new PO_Detail_Collection
                        {
                            po_number = po.PONumber,
                            item_number = GB_Detail.Key.ItemNumber,
                            item_description = GB_Detail.Key.ItemDescription,
                            color_number = GB_Detail.Key.ColorNumber,
                            color_description = GB_Detail.Key.ColorDescription,
                            inseam = GB_Detail.Key.Inseam,
                            size = GB_Detail.Key.Size,
                            barcode = barcode,
                            original_qty = GB_Detail.Sum(d => d.OriginalQty),
                            additional_qty = GB_Detail.Sum(d => d.AdditionalQty),
                            price = GB_Detail.Key.Price,
                            tariff_number = GB_Detail.Key.TariffNumber,
                            quality = GB_Detail.Key.Quality,
                            material = GB_Detail.Key.Material
                        });
                    }

                    if (errors.Any())
                        continue;

                    if (srGet.Data != null)
                    {
                        srGet.Data.po_date = po.PODate;
                        srGet.Data.eta = po.ETA;
                        srGet.Data.etd = po.ETD;
                        srGet.Data.payment_terms = po.PaymentTerms;
                        srGet.Data.packing = po.Packing;
                        srGet.Data.ship = po.Ship;
                        srGet.Data.updated_by = identity.UserId;
                        srGet.Data.items = PO_Items;

                        var msgUpdate = await _POService.Update(po.PONumber, srGet.Data, PO_Details);
                        if (!string.IsNullOrEmpty(msgUpdate))
                        {
                            errors.Add(string.Format("Update PO '{0}' error: {1}", po.PONumber, msgUpdate));
                            continue;
                        }

                        saveds.Add(srGet.Data);
                    }
                    else
                    {
                        var PO = new PO_Collection
                        {
                            po_number = po.PONumber,
                            po_date = po.PODate,
                            eta = po.ETA,
                            etd = po.ETD,
                            payment_terms = po.PaymentTerms,
                            status = (int)StatusEnums.PO.Open,
                            created_by = identity.UserId,
                            updated_by = identity.UserId,
                            items = PO_Items
                        };

                        var msgCreate = await _POService.Create(PO, PO_Details);
                        if (!string.IsNullOrEmpty(msgCreate))
                        {
                            errors.Add(string.Format("Create PO '{0}' error: {1}", po.PONumber, msgCreate));
                            continue;
                        }

                        saveds.Add(PO);
                    }
                }

                if (saveds.Any())
                {
                    await _actionService.Create(new Action_Collection
                    {
                        action_type = (int)TypeEnums.Action.POUpload,
                        action_content = string.Format("Uploaded PO(s): {0}", string.Join(", ", saveds.Select(po => po.po_number))),
                        created_by = identity.UserId
                    });
                }

                var response = new ApiResponse(saveds);
                if (errors.Any())
                {
                    response.error_code = (int)ApiError.SystemError;
                    response.error_message = string.Join("\n", errors);
                }

                return response;
            });
        }

        [HttpPost]
        [Route("update")]
        public async Task<ActionResult> Update([FromBody] PO_Update_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.PONumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PO number"));

                var srGet = await _POService.Get(request.PONumber);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PO '{0}' error: {1}", request.PONumber, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("PO '{0}'", request.PONumber)));

                srGet.Data.eta = request.ETA;
                srGet.Data.etd = request.ETD;
                srGet.Data.packing = request.Packing;
                srGet.Data.ship = request.Ship;

                var msgUpdate = await _POService.Update(request.PONumber, srGet.Data);
                if (!string.IsNullOrEmpty(msgUpdate))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Update PO '{0}' status error: {1}", request.PONumber, msgUpdate));

                await _actionService.Create(new Action_Collection
                {
                    action_type = (int)TypeEnums.Action.POUpdate,
                    action_content = string.Format("Updated PO '{0}' infomation", request.PONumber),
                    created_by = identity.UserId
                });

                return new ApiResponse(srGet.Data);
            });
        }

        [HttpPost]
        [Route("updateitemquantity")]
        public async Task<ActionResult> UpdateItemQuantity([FromBody] PO_Update_ItemQuantity_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.PONumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PO number"));

                if (string.IsNullOrEmpty(request.Barcode))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Barcode"));

                if (request.OriginalQty <= 0)
                    return new ApiResponse((int)ApiError.Required, "Original qty must be greater than 0");

                if (request.AdditionalQty <= 0)
                    return new ApiResponse((int)ApiError.Required, "Additional qty must be greater than 0");

                if (request.AdditionalQty < request.OriginalQty)
                    return new ApiResponse((int)ApiError.Required, "Additional qty must be greater than original qty");

                var srGetItem = await _POService.GetDetail(request.PONumber, request.Barcode);
                if (!string.IsNullOrEmpty(srGetItem.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get item '{0}' error: {1}", request.Barcode, srGetItem.ErrorMessage));

                if (srGetItem.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Barcode '{0}'", request.Barcode)));

                srGetItem.Data.original_qty = request.OriginalQty;
                srGetItem.Data.additional_qty = request.AdditionalQty;

                var msgUpdateItem = await _POService.UpdateDetail(request.PONumber, request.Barcode, srGetItem.Data, identity.UserId);
                if (!string.IsNullOrEmpty(msgUpdateItem))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Update item '{0}' status error: {1}", request.Barcode, msgUpdateItem));

                await _actionService.Create(new Action_Collection
                {
                    action_type = (int)TypeEnums.Action.POUpdate,
                    action_content = string.Format("Updated PO '{0}' - item '{1}' quantity", request.PONumber, request.Barcode),
                    created_by = identity.UserId
                });

                return new ApiResponse(srGetItem.Data);
            });
        }

        [HttpPost]
        [Route("count")]
        public async Task<ActionResult> Count([FromBody] CountRequest request)
        {
            return await ExecuteAsync(async () =>
            {
                if (request == null)
                    request = new CountRequest();

                var countInfos = new Dictionary<string, BsonDocument>();
                if (request.CountInfos != null && request.CountInfos.Any())
                {
                    foreach (var countInfo in request.CountInfos)
                    {
                        countInfos.Add(countInfo.Name, countInfo.Filters.CreateFilter());
                    }
                }

                var srCount = await _POService.Count(request.Filters.CreateFilter(), countInfos);
                if (!string.IsNullOrEmpty(srCount.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Count PO(s) error: {0}", srCount.ErrorMessage));

                return new ApiResponse(srCount.Data);
            });
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
                    //new BsonDocument("$lookup", new BsonDocument {
                    //    { "from", "pl_detail" },
                    //    { "let", new BsonDocument("po_number", "$po_number") },
                    //    { "pipeline",
                    //        new BsonArray {
                    //            new BsonDocument("$unwind", "$item_details"),
                    //            new BsonDocument("$match",
                    //                new BsonDocument("$expr",
                    //                    new BsonDocument("$eq", new BsonArray { "$po_number", "$$po_number" })
                    //                )
                    //            ),
                    //            new BsonDocument("$project", new BsonDocument {
                    //                { "_id", 0 },
                    //                { "use_produce_qty", "$use_produce_qty" },
                    //                { "box_status", "$item_details.box_status" },
                    //                { "expected_qty", "$item_details.expected_qty" },
                    //                { "packed_qty", "$item_details.packed_qty" }
                    //            })
                    //        } 
                    //    },
                    //    { "as", "packed_infos" }
                    //}),
                    request.Sorts.CreateSort()
                };
                var srRead = await _POService.Read<object>(stages, request.PageSkip, request.PageLimit);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiReadResponse((int)ApiError.DbError, string.Format("Get PO(s) error: {0}", srRead.ErrorMessage));

                return new ApiReadResponse(srRead.Datas, srRead.Total);
            });
        }

        [HttpGet]
        [Route("getdetail")]
        public async Task<ActionResult> GetDetail(string PONumber)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(PONumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PO number"));

                var stages = new BsonDocument[] {
                    new BsonDocument("$match", new BsonDocument("po_number", PONumber)),
                    new BsonDocument("$lookup", new BsonDocument {
                        { "from", "po_detail" },
                        { "localField", "po_number" },
                        { "foreignField", "po_number" },
                        { "as", "item_details" }
                    })
                };

                var srRead = await _POService.Read<object>(stages);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PO(s) error: {0}", srRead.ErrorMessage));

                var data = srRead.Datas != null && srRead.Datas.Any() ? srRead.Datas.First() : new { };

                return new ApiResponse(data);
            });
        }

        [HttpGet]
        [Route("getitems")]
        public async Task<ActionResult> GetItems(string PONumber)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(PONumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PO number"));

                var stages = new BsonDocument[] {
                    new BsonDocument("$match", new BsonDocument("po_number", PONumber)),
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
                                new BsonDocument("$unwind", "$item_details"),
                                new BsonDocument("$match",
                                    new BsonDocument("$expr",
                                        new BsonDocument("$and", new BsonArray {
                                            new BsonDocument("$eq", new BsonArray {
                                                "$po_number",
                                                "$$po_number"
                                            }),
                                            new BsonDocument("$eq", new BsonArray {
                                                "$item_details.barcode",
                                                "$$barcode"
                                            })
                                        })
                                    )
                                ),
                                new BsonDocument("$project", new BsonDocument {
                                    { "_id", 0 },
                                    { "use_produce_qty", "$use_produce_qty" },
                                    { "box_status", "$item_details.box_status" },
                                    { "expected_qty", "$item_details.expected_qty" },
                                    { "packed_qty", "$item_details.packed_qty" }
                                })
                            } 
                        },
                        { "as", "packed_infos" }
                    }),
                    new BsonDocument("$sort", new BsonDocument {
                        { "item_number", 1 },
                        { "color_number", 1 },
                        { "inseam", 1 },
                        { "size", 1 }
                    })
                };

                var srRead = await _POService.ReadDetail<object>(stages);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PO '{0}' items error: {1}", PONumber, srRead.ErrorMessage));

                return new ApiResponse(srRead.Datas);
            });
        }

        [HttpGet]
        [Route("getallstatus")]
        public ActionResult GetAllStatus()
        {
            return Execute(() =>
            {
                var status = ConvertEnumToDictionary<StatusEnums.PO>();
                return new ApiResponse(status);
            });
        }

        #region Private methods

        string getBarcode(string itemNumber, string colorNumber, string inseam, string size)
        {
            var srGet = _itemService.Get(itemNumber, colorNumber, inseam, size);
            srGet.Wait();
            return srGet.Result.Data == null ? "" : srGet.Result.Data.barcode;
        }

        #endregion
    }
}
