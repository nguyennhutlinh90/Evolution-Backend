using Evolution_Backend.DbModels;
using Evolution_Backend.Models;
using Evolution_Backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MongoDB.Bson;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Evolution_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/packinglist")]
    public class PackingListController : ApiControllerBase
    {
        private readonly INumGenService _numGenService;
        private readonly IActionService _actionService;
        private readonly ICustomerService _customerService;
        private readonly IUserService _userService;
        private readonly IPOService _POService;
        private readonly IPLService _PLService;
        public PackingListController(INumGenService numGenService, IActionService actionService, ICustomerService customerService, IUserService userService, IPOService POService, IPLService PLService)
        {
            _numGenService = numGenService;
            _actionService = actionService;
            _customerService = customerService;
            _userService = userService;
            _POService = POService;
            _PLService = PLService;
        }

        [HttpPost]
        [Route("upload")]
        public async Task<ActionResult> Upload([FromBody] List<PL_Request> request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null || !request.Any())
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (request.Any(pl => string.IsNullOrEmpty(pl.PLNumber)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("PL number(s)"));

                if (request.Any(pl => string.IsNullOrEmpty(pl.PONumber)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("PO number(s)"));

                //var srGetNumGenBox = await _numGenService.Get(Constants.NumGenType.Box);
                //if (!string.IsNullOrEmpty(srGetNumGenBox.ErrorMessage))
                //    return new ApiResponse((int)ApiError.DbError, string.Format("Get box number generator error: {0}", srGetNumGenBox.ErrorMessage));

                var errors = new List<string>();
                var saveds = new List<PL_Detail_Collection>();
                foreach (var pl in request)
                {
                    if (saveds.Any(s => s.pl_number == pl.PLNumber && s.po_number == pl.PONumber))
                    {
                        errors.Add(ApiError.Dupplicated.GetDecription(string.Format("PL '{0}' - PO '{1}'", pl.PLNumber, pl.PONumber)));
                        continue;
                    }

                    if (!string.IsNullOrEmpty(pl.CustomerCode))
                    {
                        var srGetCustomer = await _customerService.Get(pl.CustomerCode);
                        if (!string.IsNullOrEmpty(srGetCustomer.ErrorMessage))
                        {
                            errors.Add(string.Format("PL '{0}' - PO '{1}': {2}", pl.PLNumber, pl.PONumber, string.Format("Get customer '{0}' error: {1}", pl.CustomerCode, srGetCustomer.ErrorMessage)));
                            continue;
                        }

                        if (srGetCustomer.Data == null)
                        {
                            errors.Add(string.Format("PL '{0}' - PO '{1}': {2}", pl.PLNumber, pl.PONumber, ApiError.NotFound.GetDecription(string.Format("Customer '{0}'", pl.CustomerCode))));
                            continue;
                        }
                    }

                    int? status = null;
                    if (!string.IsNullOrEmpty(pl.StatusCode))
                    {
                        status = pl.StatusCode.GetValue<StatusEnums.PL_PO>();
                        if (!status.HasValue)
                        {
                            errors.Add(string.Format("PL '{0}' - PO '{1}': {2}", pl.PLNumber, pl.PONumber, ApiError.NotFound.GetDecription(string.Format("Status '{0}'", pl.StatusCode))));
                            continue;
                        }

                        if (!new int[] { (int)StatusEnums.PL_PO.Open, (int)StatusEnums.PL_PO.Ready }.Contains(status.Value))
                        {
                            errors.Add(string.Format("PL '{0}' - PO '{1}': Status is {2}. Only allow upload packing PO with status 'open' or 'ready'", pl.PLNumber, pl.PONumber, pl.StatusCode));
                            continue;
                        }
                    }

                    if (!status.HasValue)
                        status = (int)StatusEnums.PL_PO.Open;

                    if (pl.Details == null || !pl.Details.Any())
                    {
                        errors.Add(string.Format("PL '{0}' - PO '{1}': {2}", pl.PLNumber, pl.PONumber, ApiError.Requireds.GetDecription("Detail(s)")));
                        continue;
                    }

                    if (pl.Details.Any(i => string.IsNullOrEmpty(i.BoxNumber)))
                    {
                        errors.Add(string.Format("PL '{0}' - PO '{1}': {2}", pl.PLNumber, pl.PONumber, ApiError.Requireds.GetDecription("Box number(s)")));
                        continue;
                    }

                    if (pl.Details.Any(i => string.IsNullOrEmpty(i.BoxDimension)))
                    {
                        errors.Add(string.Format("PL '{0}' - PO '{1}': {2}", pl.PLNumber, pl.PONumber, ApiError.Requireds.GetDecription("Box dimension(s)")));
                        continue;
                    }

                    if (pl.Details.Any(i => i.BoxWeight <= 0))
                    {
                        errors.Add(string.Format("PL '{0}' - PO '{1}': {2}", pl.PLNumber, pl.PONumber, "Box weight(s) must be greater than 0"));
                        continue;
                    }

                    if (pl.Details.Any(i => string.IsNullOrEmpty(i.ItemNumber)))
                    {
                        errors.Add(string.Format("PL '{0}' - PO '{1}': {2}", pl.PLNumber, pl.PONumber, ApiError.Requireds.GetDecription("Item number(s)")));
                        continue;
                    }

                    if (pl.Details.Any(i => string.IsNullOrEmpty(i.ColorNumber)))
                    {
                        errors.Add(string.Format("PL '{0}' - PO '{1}': {2}", pl.PLNumber, pl.PONumber, ApiError.Requireds.GetDecription("Color number(s)")));
                        continue;
                    }

                    if (pl.Details.Any(i => string.IsNullOrEmpty(i.Inseam)))
                    {
                        errors.Add(string.Format("PL '{0}' - PO '{1}': {2}", pl.PLNumber, pl.PONumber, ApiError.Requireds.GetDecription("Inseam(s)")));
                        continue;
                    }

                    if (pl.Details.Any(i => string.IsNullOrEmpty(i.Size)))
                    {
                        errors.Add(string.Format("PL '{0}' - PO '{1}': {2}", pl.PLNumber, pl.PONumber, ApiError.Requireds.GetDecription("Size(s)")));
                        continue;
                    }

                    //if (pl.Details.Any(i => i.ItemWeight <= 0))
                    //{
                    //    errors.Add(string.Format("PL '{0}' - PO '{1}': {2}", pl.PLNumber, pl.PONumber, "Item weight(s) must be greater than 0"));
                    //    continue;
                    //}

                    if (pl.Details.Any(i => i.ExpectedQty <= 0))
                    {
                        errors.Add(string.Format("PL '{0}' - PO '{1}': {2}", pl.PLNumber, pl.PONumber, "Expected qt(s) must be greater than 0"));
                        continue;
                    }

                    var srGetPO = await _POService.Get(pl.PONumber);
                    if (!string.IsNullOrEmpty(srGetPO.ErrorMessage))
                    {
                        errors.Add(string.Format("Get PO '{0}' error: {1}", pl.PONumber, srGetPO.ErrorMessage));
                        continue;
                    }

                    if (srGetPO.Data == null)
                    {
                        errors.Add(ApiError.NotFound.GetDecription(string.Format("PO '{0}'", pl.PONumber)));
                        continue;
                    }

                    if (new int[] { (int)StatusEnums.PO.Packed, (int)StatusEnums.PO.Shipped }.Contains(srGetPO.Data.status))
                    {
                        var POStatusCode = srGetPO.Data.status.GetName<StatusEnums.PO>();
                        errors.Add(string.Format("PO '{0}' status is '{1}', can not add PO to PL '{2}'", pl.PONumber, POStatusCode, pl.PLNumber));
                        continue;
                    }

                    var srGet = await _PLService.Get(pl.PLNumber);
                    if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    {
                        errors.Add(string.Format("Get PL '{0}' error: {1}", pl.PLNumber, srGet.ErrorMessage));
                        continue;
                    }

                    if (srGet.Data != null)
                    {
                        var srGetDetail = await _PLService.GetDetail<object>(pl.PLNumber, pl.PONumber, false);
                        if (!string.IsNullOrEmpty(srGetDetail.ErrorMessage))
                        {
                            errors.Add(string.Format("Get PL '{0}' - PO '{1} error: {2}", pl.PLNumber, pl.PONumber, srGetDetail.ErrorMessage));
                            continue;
                        }

                        if (srGetDetail.Data != null)
                        {
                            errors.Add(ApiError.Existed.GetDecription(string.Format("PL '{0}' - PO '{1}'", pl.PLNumber, pl.PONumber)));
                            continue;
                        }
                    }

                    var srGetPODetail = await _POService.GetDetails(pl.PONumber);
                    if (!string.IsNullOrEmpty(srGetPODetail.ErrorMessage))
                    {
                        errors.Add(string.Format("Get PO '{0}' details error: {1}", pl.PONumber, srGetPODetail.ErrorMessage));
                        continue;
                    }

                    var srGetDetailsByPO = await _PLService.GetDetailsByPO<PL_Detail_Collection>(pl.PONumber);
                    if (!string.IsNullOrEmpty(srGetDetailsByPO.ErrorMessage))
                    {
                        errors.Add(string.Format("Get all PO '{0}' packing error: {1}", pl.PONumber, srGetDetailsByPO.ErrorMessage));
                        continue;
                    }

                    var msgCheckQty = checkQuantity(srGetPODetail.Data, srGetDetailsByPO.Data, pl);
                    if (!string.IsNullOrEmpty(msgCheckQty))
                    {
                        errors.Add(string.Format("PO '{0}': {1}", pl.PONumber, msgCheckQty));
                        continue;
                    }

                    #region Get definies

                    var PL_Item_Definies = new List<PL_Item_Define_Collection>();
                    var hasDefine = pl.Definies != null && pl.Definies.Any();
                    if (hasDefine)
                    {
                        foreach (var define in pl.Definies)
                        {
                            if (PL_Item_Definies.Any(d => d.item_number == define.ItemNumber && d.color_number == define.ColorNumber && d.inseam == define.Inseam && d.size == define.Size))
                            {
                                errors.Add(string.Format("PL '{0}' - PO '{1}' define: Item '{2}' - Color '{3}' - Inseam '{4}' - Size '{5}' is dupplicated in list", pl.PLNumber, pl.PONumber, define.ItemNumber, define.ColorNumber, define.Inseam, define.Size));
                                break;
                            }

                            var POItem = srGetPODetail.Data.FirstOrDefault(d => d.item_number == define.ItemNumber && d.color_number == define.ColorNumber && d.inseam == define.Inseam && d.size == define.Size);
                            if (POItem == null)
                            {
                                errors.Add(string.Format("PL '{0}' - PO '{1}' define: Item '{2}' - Color '{3}' - Inseam '{4}' - Size '{5}' was not found in PO '{6}'", pl.PLNumber, pl.PONumber, define.ItemNumber, define.ColorNumber, define.Inseam, define.Size, pl.PONumber));
                                break;
                            }

                            PL_Item_Definies.Add(new PL_Item_Define_Collection
                            {
                                item_number = define.ItemNumber,
                                item_description = POItem.item_description,
                                color_number = define.ColorNumber,
                                color_description = POItem.color_description,
                                inseam = define.Inseam,
                                size = define.Size,
                                barcode = POItem.barcode,
                                item_weight = define.ItemWeight,
                                po_qty = pl.UseProduceQty ? POItem.additional_qty : POItem.original_qty
                            });
                        }
                    }
                    else
                    {
                        foreach (var item in srGetPODetail.Data)
                        {
                            PL_Item_Definies.Add(new PL_Item_Define_Collection
                            {
                                item_number = item.item_number,
                                item_description = item.item_description,
                                color_number = item.color_number,
                                color_description = item.color_description,
                                inseam = item.inseam,
                                size = item.size,
                                barcode = item.barcode,
                                po_qty = pl.UseProduceQty ? item.additional_qty : item.original_qty
                            });
                        }
                    }

                    if (errors.Any())
                        continue;

                    #endregion

                    var PL_Items = new List<PL_Item_Collection>();
                    var PL_Item_Details = new List<PL_Item_Detail_Collection>();
                    foreach (var GBItem in pl.Details.GroupBy(i => new { i.ItemNumber, i.ColorNumber, i.Inseam, i.Size }))
                    {
                        var defineItem = PL_Item_Definies.FirstOrDefault(d => d.item_number == GBItem.Key.ItemNumber && d.color_number == GBItem.Key.ColorNumber && d.inseam == GBItem.Key.Inseam && d.size == GBItem.Key.Size);
                        if (defineItem == null)
                        {
                            errors.Add(string.Format("PL '{0}' - PO '{1}': Item '{2}' - Color '{3}' - Inseam '{4}' - Size '{5}' was not found in PO '{6}'", pl.PLNumber, pl.PONumber, GBItem.Key.ItemNumber, GBItem.Key.ColorNumber, GBItem.Key.Inseam, GBItem.Key.Size, pl.PONumber));
                            break;
                        }

                        var expectedQty = GBItem.Sum(i => i.ExpectedQty);
                        if (expectedQty > defineItem.po_qty)
                        {
                            errors.Add(string.Format("PL '{0}' - PO '{1}': Item '{2}' - Color '{3}' - Inseam '{4}' - Size '{5}': Expected qty ({6}) is out of PO '{7}' qty ({8})", pl.PLNumber, pl.PONumber, GBItem.Key.ItemNumber, GBItem.Key.ColorNumber, GBItem.Key.Inseam, GBItem.Key.Size, expectedQty, pl.PONumber, defineItem.po_qty));
                            break;
                        }

                        if (!PL_Items.Any(i => i.item_number == defineItem.item_number))
                            PL_Items.Add(new PL_Item_Collection { item_number = defineItem.item_number, item_description = defineItem.item_description });

                        foreach (var item in GBItem)
                        {
                            if (GBItem.Count(i => i.BoxNumber == item.BoxNumber) > 1)
                            {
                                errors.Add(string.Format("PL '{0}' - PO '{1}': Box '{2}' - Item '{3}' - Color '{4}' - Inseam '{5}' - Size '{6}' is dupplicated in list", pl.PLNumber, pl.PONumber, item.BoxNumber, GBItem.Key.ItemNumber, GBItem.Key.ColorNumber, GBItem.Key.Inseam, GBItem.Key.Size));
                                break;
                            }

                            PL_Item_Details.Add(new PL_Item_Detail_Collection
                            {
                                box_number = item.BoxNumber,
                                box_dimension = item.BoxDimension,
                                box_weight = item.BoxWeight,
                                box_status = (int)StatusEnums.PL_Box.Open,
                                item_number = item.ItemNumber,
                                item_description = defineItem.item_description,
                                color_number = item.ColorNumber,
                                color_description = defineItem.color_description,
                                inseam = item.Inseam,
                                size = item.Size,
                                barcode = defineItem.barcode,
                                item_weight = item.ItemWeight,
                                expected_qty = item.ExpectedQty,
                                packed_qty = 0,
                                qty_status = (int)StatusEnums.PL_Qty.Diff,
                                note = ""
                            });

                            //var boxNum = getBoxNumber(item.BoxNumber, srGetNumGenBox.Data.gen_length);
                            //if (boxNum > srGetNumGenBox.Data.gen_number)
                            //    srGetNumGenBox.Data.gen_number = boxNum;
                        }

                        if (errors.Any())
                            break;
                    }

                    if (errors.Any())
                        continue;

                    var PL_Detail = new PL_Detail_Collection
                    {
                        pl_number = pl.PLNumber,
                        po_number = pl.PONumber,
                        customer_code = pl.CustomerCode,
                        status = status.Value,
                        process_manual = pl.ProcessManual,
                        use_produce_qty = pl.UseProduceQty,
                        created_by = identity.UserId,
                        updated_by = identity.UserId,
                        items = PL_Items,
                        item_details = PL_Item_Details
                    };
                    if (hasDefine)
                        PL_Detail.item_definies = PL_Item_Definies;

                    if (srGet.Data == null)
                    {
                        var PL = new PL_Collection
                        {
                            pl_number = pl.PLNumber,
                            status = (int)StatusEnums.PL.Draft,
                            created_by = identity.UserId,
                            updated_by = identity.UserId
                        };
                        var msgCreate = await _PLService.Create(PL, new[] { PL_Detail });
                        if (!string.IsNullOrEmpty(msgCreate))
                            return new ApiResponse((int)ApiError.DbError, string.Format("Create PL '{0}' with PO '{1}' error: {2}", pl.PLNumber, pl.PONumber, msgCreate));

                        saveds.Add(PL_Detail);

                        await _actionService.Create(new Action_Collection
                        {
                            action_type = (int)TypeEnums.Action.PLCreate,
                            action_content = string.Format("Created new PL '{0}' with PO '{1}'", pl.PLNumber, pl.PONumber),
                            created_by = identity.UserId
                        });
                    }
                    else
                    {
                        var msgAddDetail = await _PLService.AddDetail(pl.PLNumber, PL_Detail);
                        if (!string.IsNullOrEmpty(msgAddDetail))
                            return new ApiResponse((int)ApiError.DbError, string.Format("Add PO '{0}' to PL '{1}' error: {2}", pl.PONumber, pl.PLNumber, msgAddDetail));

                        saveds.Add(PL_Detail);

                        await _actionService.Create(new Action_Collection
                        {
                            action_type = (int)TypeEnums.Action.PLCreate,
                            action_content = string.Format("Added PO '{0}' to PL '{1}'", pl.PONumber, pl.PLNumber),
                            created_by = identity.UserId
                        });
                    }

                    var msgUpdateStatus = await updatePLStatus(pl.PLNumber, identity.UserId);
                    if (!string.IsNullOrEmpty(msgUpdateStatus))
                        return new ApiResponse((int)ApiError.DbError, msgUpdateStatus);

                    if (PL_Detail.status == (int)StatusEnums.PL_PO.Ready && srGetPO.Data.status == (int)StatusEnums.PO.Open)
                    {
                        var msgUpdatePOStatus = await _POService.UpdateStatus(pl.PONumber, (int)StatusEnums.PO.Ready, identity.UserId);
                        if (!string.IsNullOrEmpty(msgUpdatePOStatus))
                            return new ApiResponse((int)ApiError.DbError, string.Format("Update PO '{0}' status to ready error: {1}", pl.PONumber, msgUpdatePOStatus));
                    }

                    //srGetNumGenBox.Data.updated_by = identity.UserId;
                    //await _numGenService.Update(Constants.NumGenType.Box, srGetNumGenBox.Data);
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

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost]
        [Route("create")]
        public async Task<ActionResult> Create([FromBody] PL_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.PLNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PL number"));

                if (string.IsNullOrEmpty(request.PONumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PO number"));

                if (!string.IsNullOrEmpty(request.CustomerCode))
                {
                    var srGetCustomer = await _customerService.Get(request.CustomerCode);
                    if (!string.IsNullOrEmpty(srGetCustomer.ErrorMessage))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Get PO '{0}' error: {1}", request.CustomerCode, srGetCustomer.ErrorMessage));

                    if (srGetCustomer.Data == null)
                        return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Customer '{0}'", request.CustomerCode)));
                }

                int? status = null;
                if (!string.IsNullOrEmpty(request.StatusCode))
                {
                    status = request.StatusCode.GetValue<StatusEnums.PL_PO>();
                    if (!status.HasValue)
                        return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Status '{0}'", request.StatusCode)));

                    if (!new int[] { (int)StatusEnums.PL_PO.Open, (int)StatusEnums.PL_PO.Ready }.Contains(status.Value))
                        return new ApiResponse((int)ApiError.SystemError, string.Format("Status is '{0}'. Only allow create packing PO with status 'open' or 'ready'", request.StatusCode));
                }

                if (!status.HasValue)
                    status = (int)StatusEnums.PL_PO.Open;

                if (!request.Details.Any())
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("Detail(s)"));

                if (request.Details.Any(i => string.IsNullOrEmpty(i.BoxNumber)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("Box number(s)"));

                if (request.Details.Any(i => string.IsNullOrEmpty(i.BoxDimension)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("Box dimension(s)"));

                if (request.Details.Any(i => i.BoxWeight <= 0))
                    return new ApiResponse((int)ApiError.Requireds, "Box weight(s) must be greater than 0");

                if (request.Details.Any(i => string.IsNullOrEmpty(i.ItemNumber)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("Item number(s)"));

                if (request.Details.Any(i => string.IsNullOrEmpty(i.ColorNumber)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("Color number(s)"));

                if (request.Details.Any(i => string.IsNullOrEmpty(i.Inseam)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("Inseam(s)"));

                if (request.Details.Any(i => string.IsNullOrEmpty(i.Size)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("Size(s)"));

                //if (request.Details.Any(i => i.ItemWeight <= 0))
                //    return new ApiResponse((int)ApiError.Requireds, "Item weight(s) must be greater than 0");

                if (request.Details.Any(i => i.ExpectedQty <= 0))
                    return new ApiResponse((int)ApiError.Requireds, "Expected qty(s) must be greater than 0");

                var srGetPO = await _POService.Get(request.PONumber);
                if (!string.IsNullOrEmpty(srGetPO.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PO '{0}' error: {1}", request.PONumber, srGetPO.ErrorMessage));

                if (srGetPO.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("PO '{0}'", request.PONumber)));

                if (new int[] { (int)StatusEnums.PO.Packed, (int)StatusEnums.PO.Shipped }.Contains(srGetPO.Data.status))
                {
                    var POStatusCode = srGetPO.Data.status.GetName<StatusEnums.PO>();
                    return new ApiResponse((int)ApiError.SystemError, string.Format("PO '{0}' status is '{1}', can not add PO to PL '{2}'", request.PONumber, POStatusCode, request.PLNumber));
                }

                var srGet = await _PLService.Get(request.PLNumber);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' error: {1}", request.PLNumber, srGet.ErrorMessage));

                if (srGet.Data != null)
                {
                    var srGetDetail = await _PLService.GetDetail<object>(request.PLNumber, request.PONumber, false);
                    if (!string.IsNullOrEmpty(srGetDetail.ErrorMessage))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Get PO '{0}' packing error: {1}", request.PONumber, srGetDetail.ErrorMessage));

                    if (srGetDetail.Data != null)
                        return new ApiResponse((int)ApiError.Existed, ApiError.Existed.GetDecription(string.Format("PL '{0}' - PO '{1}'", request.PLNumber, request.PONumber)));
                }

                var srGetPODetail = await _POService.GetDetails(request.PONumber);
                if (!string.IsNullOrEmpty(srGetPODetail.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PO '{0}' details error: {1}", request.PONumber, srGetPODetail.ErrorMessage));

                var srGetDetailsByPO = await _PLService.GetDetailsByPO<PL_Detail_Collection>(request.PONumber);
                if (!string.IsNullOrEmpty(srGetDetailsByPO.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get all PO '{0}' packing error: {1}", request.PONumber, srGetDetailsByPO.ErrorMessage));

                var msgCheckQty = checkQuantity(srGetPODetail.Data, srGetDetailsByPO.Data, request);
                if (!string.IsNullOrEmpty(msgCheckQty))
                    return new ApiResponse((int)ApiError.SystemError, msgCheckQty);

                #region Get definies

                var PL_Item_Definies = new List<PL_Item_Define_Collection>();
                var hasDefine = request.Definies != null && request.Definies.Any();
                if (hasDefine)
                {
                    foreach (var define in request.Definies)
                    {
                        if (PL_Item_Definies.Any(d => d.item_number == define.ItemNumber && d.color_number == define.ColorNumber && d.inseam == define.Inseam && d.size == define.Size))
                            return new ApiResponse((int)ApiError.Dupplicated, string.Format("Packing define: Item '{0}' - Color '{1}' - Inseam '{2}' - Size '{3}' is dupplicated in list", define.ItemNumber, define.ColorNumber, define.Inseam, define.Size));

                        var POItem = srGetPODetail.Data.FirstOrDefault(d => d.item_number == define.ItemNumber && d.color_number == define.ColorNumber && d.inseam == define.Inseam && d.size == define.Size);
                        if (POItem == null)
                            return new ApiResponse((int)ApiError.NotFound, string.Format("Packing define: Item '{0}' - Color '{1}' - Inseam '{2}' - Size '{3}' was not found in PO '{4}'", define.ItemNumber, define.ColorNumber, define.Inseam, define.Size, request.PONumber));

                        PL_Item_Definies.Add(new PL_Item_Define_Collection
                        {
                            item_number = define.ItemNumber,
                            item_description = POItem.item_description,
                            color_number = define.ColorNumber,
                            color_description = POItem.color_description,
                            inseam = define.Inseam,
                            size = define.Size,
                            barcode = POItem.barcode,
                            item_weight = define.ItemWeight,
                            po_qty = request.UseProduceQty ? POItem.additional_qty : POItem.original_qty
                        });
                    }
                }
                else
                {
                    foreach (var item in srGetPODetail.Data)
                    {
                        PL_Item_Definies.Add(new PL_Item_Define_Collection
                        {
                            item_number = item.item_number,
                            item_description = item.item_description,
                            color_number = item.color_number,
                            color_description = item.color_description,
                            inseam = item.inseam,
                            size = item.size,
                            barcode = item.barcode,
                            po_qty = request.UseProduceQty ? item.additional_qty : item.original_qty
                        });
                    }
                }

                #endregion

                //var srGetNumGenBox = await _numGenService.Get(Constants.NumGenType.Box);
                //if (!string.IsNullOrEmpty(srGetNumGenBox.ErrorMessage))
                //    return new ApiResponse((int)ApiError.DbError, string.Format("Get box number generator error: {0}", srGetNumGenBox.ErrorMessage));

                var PL_Items = new List<PL_Item_Collection>();
                var PL_Item_Details = new List<PL_Item_Detail_Collection>();
                foreach (var GBItem in request.Details.GroupBy(i => new { i.ItemNumber, i.ColorNumber, i.Inseam, i.Size }))
                {
                    var defineItem = PL_Item_Definies.FirstOrDefault(d => d.item_number == GBItem.Key.ItemNumber && d.color_number == GBItem.Key.ColorNumber && d.inseam == GBItem.Key.Inseam && d.size == GBItem.Key.Size);
                    if (defineItem == null)
                        return new ApiResponse((int)ApiError.NotFound, string.Format("Item '{0}' - Color '{1}' - Inseam '{2}' - Size '{3}' was not found in PO '{4}'", GBItem.Key.ItemNumber, GBItem.Key.ColorNumber, GBItem.Key.Inseam, GBItem.Key.Size, request.PONumber));

                    var expectedQty = GBItem.Sum(i => i.ExpectedQty);
                    if (expectedQty > defineItem.po_qty)
                        return new ApiResponse((int)ApiError.NotFound, string.Format("Item '{0}' - Color '{1}' - Inseam '{2}' - Size '{3}': Expected qty ({4}) is out of PO '{5}' qty ({6})", GBItem.Key.ItemNumber, GBItem.Key.ColorNumber, GBItem.Key.Inseam, GBItem.Key.Size, expectedQty, request.PONumber, defineItem.po_qty));

                    if (!PL_Items.Any(i => i.item_number == defineItem.item_number))
                        PL_Items.Add(new PL_Item_Collection { item_number = defineItem.item_number, item_description = defineItem.item_description });

                    foreach (var item in GBItem)
                    {
                        if (GBItem.Count(i => i.BoxNumber == item.BoxNumber) > 1)
                            return new ApiResponse((int)ApiError.Dupplicated, string.Format("Box '{0}' - Item '{1}' - Color '{2}' - Inseam '{3}' - Size '{4}' is dupplicated in list", item.BoxNumber, GBItem.Key.ItemNumber, GBItem.Key.ColorNumber, GBItem.Key.Inseam, GBItem.Key.Size));

                        PL_Item_Details.Add(new PL_Item_Detail_Collection
                        {
                            box_number = item.BoxNumber,
                            box_dimension = item.BoxDimension,
                            box_weight = item.BoxWeight,
                            box_status = (int)StatusEnums.PL_Box.Open,
                            item_number = item.ItemNumber,
                            item_description = defineItem.item_description,
                            color_number = item.ColorNumber,
                            color_description = defineItem.color_description,
                            inseam = item.Inseam,
                            size = item.Size,
                            barcode = defineItem.barcode,
                            item_weight = item.ItemWeight,
                            expected_qty = item.ExpectedQty,
                            packed_qty = 0,
                            qty_status = (int)StatusEnums.PL_Qty.Diff,
                            note = ""
                        });

                        //var boxNum = getBoxNumber(item.BoxNumber, srGetNumGenBox.Data.gen_length);
                        //if (boxNum > srGetNumGenBox.Data.gen_number)
                        //    srGetNumGenBox.Data.gen_number = boxNum;
                    }
                }

                var PL_Detail = new PL_Detail_Collection
                {
                    pl_number = request.PLNumber,
                    po_number = request.PONumber,
                    customer_code = request.CustomerCode,
                    status = status.Value,
                    process_manual = request.ProcessManual,
                    use_produce_qty = request.UseProduceQty,
                    created_by = identity.UserId,
                    updated_by = identity.UserId,
                    items = PL_Items,
                    item_details = PL_Item_Details
                };
                if (hasDefine)
                    PL_Detail.item_definies = PL_Item_Definies;

                if (srGet.Data == null)
                {
                    var PL = new PL_Collection
                    {
                        pl_number = request.PLNumber,
                        status = (int)StatusEnums.PL.Draft,
                        created_by = identity.UserId,
                        updated_by = identity.UserId
                    };
                    var msgCreate = await _PLService.Create(PL, new[] { PL_Detail });
                    if (!string.IsNullOrEmpty(msgCreate))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Create PL '{0}' with PO '{1}' error: {2}", request.PLNumber, request.PONumber, msgCreate));

                    await _actionService.Create(new Action_Collection
                    {
                        action_type = (int)TypeEnums.Action.PLCreate,
                        action_content = string.Format("Created new PL '{0}' with PO '{1}'", request.PLNumber, request.PONumber),
                        created_by = identity.UserId
                    });
                }
                else
                {
                    var msgAddDetail = await _PLService.AddDetail(request.PLNumber, PL_Detail);
                    if (!string.IsNullOrEmpty(msgAddDetail))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Add PO '{0}' to PL '{1}' error: {2}", request.PONumber, request.PLNumber, msgAddDetail));

                    await _actionService.Create(new Action_Collection
                    {
                        action_type = (int)TypeEnums.Action.PLCreate,
                        action_content = string.Format("Added PO '{0}' to PL '{1}'", request.PONumber, request.PLNumber),
                        created_by = identity.UserId
                    });
                }

                var msgUpdateStatus = await updatePLStatus(request.PLNumber, identity.UserId);
                if (!string.IsNullOrEmpty(msgUpdateStatus))
                    return new ApiResponse((int)ApiError.DbError, msgUpdateStatus);

                if (PL_Detail.status == (int)StatusEnums.PL_PO.Ready && srGetPO.Data.status == (int)StatusEnums.PO.Open)
                {
                    var msgUpdatePOStatus = await _POService.UpdateStatus(request.PONumber, (int)StatusEnums.PO.Ready, identity.UserId);
                    if (!string.IsNullOrEmpty(msgUpdatePOStatus))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Update PO '{0}' status to ready error: {1}", request.PONumber, msgUpdatePOStatus));
                }

                //srGetNumGenBox.Data.updated_by = identity.UserId;
                //await _numGenService.Update(Constants.NumGenType.Box, srGetNumGenBox.Data);

                return new ApiResponse(PL_Detail);
            });
        }

        [HttpPost]
        [Route("delete")]
        public async Task<ActionResult> Delete(string PLNumber)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (string.IsNullOrEmpty(PLNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PL number"));

                var srGet = await _PLService.Get(PLNumber);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' error: {1}", PLNumber, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("PL '{0}'", PLNumber)));

                if (!new int[] { (int)StatusEnums.PL.Draft, (int)StatusEnums.PL.Ready }.Contains(srGet.Data.status))
                {
                    var statusCode = srGet.Data.status.GetName<StatusEnums.PL>();
                    return new ApiResponse((int)ApiError.SystemError, string.Format("PL '{0}' status is '{1}', can not delete PL", PLNumber, statusCode));
                }

                var srGetDetails = await _PLService.GetDetails<PL_Detail_Collection>(PLNumber, false);
                if (!string.IsNullOrEmpty(srGetDetails.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' PO(s) error: {1}", PLNumber, srGetDetails.ErrorMessage));

                var msgDelete = await _PLService.Delete(PLNumber);
                if (!string.IsNullOrEmpty(msgDelete))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Delete PL '{0}' error: {1}", PLNumber, msgDelete));

                await _actionService.Create(new Action_Collection
                {
                    action_type = (int)TypeEnums.Action.PLDelete,
                    action_content = string.Format("Deleted PL '{0}'", PLNumber),
                    created_by = identity.UserId
                });

                foreach (var detail in srGetDetails.Data)
                {
                    var srGetDetailsByPO = await _PLService.GetDetailsByPO<PL_Detail_Collection>(detail.po_number, false);
                    if (!string.IsNullOrEmpty(srGetDetailsByPO.ErrorMessage))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Get PO '{0}' error: {1}", detail.po_number, srGetDetailsByPO.ErrorMessage));

                    if (!srGetDetailsByPO.Data.Any(d => d.status != (int)StatusEnums.PL_PO.Open))
                    {
                        var msgUpdatePOStatus = await _POService.UpdateStatus(detail.po_number, (int)StatusEnums.PO.Open, identity.UserId);
                        if (!string.IsNullOrEmpty(msgUpdatePOStatus))
                            return new ApiResponse((int)ApiError.DbError, string.Format("Update PO '{0}' status to open error: {1}", detail.po_number, msgUpdatePOStatus));
                    }
                }

                return new ApiResponse { is_success = true };
            });
        }

        [HttpPost]
        [Route("setready")]
        public async Task<ActionResult> SetReady(string PLNumber)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (string.IsNullOrEmpty(PLNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PL number"));

                var srGet = await _PLService.Get(PLNumber);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' error: {1}", PLNumber, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("PL '{0}'", PLNumber)));

                if (srGet.Data.status != (int)StatusEnums.PL.Draft)
                {
                    var statusCode = srGet.Data.status.GetName<StatusEnums.PL>();
                    return new ApiResponse((int)ApiError.SystemError, string.Format("PL '{0}' status is '{1}', can not set ready for PL", PLNumber, statusCode));
                }

                var srGetDetails = await _PLService.GetDetails<PL_Detail_Collection>(PLNumber, false);
                if (!string.IsNullOrEmpty(srGetDetails.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' PO(s) error: {1}", PLNumber, srGetDetails.ErrorMessage));

                foreach (var detail in srGetDetails.Data)
                {
                    var srGetPODetail = await _POService.GetDetails(detail.po_number);
                    if (!string.IsNullOrEmpty(srGetPODetail.ErrorMessage))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Get PO '{0}' details error: {1}", detail.po_number, srGetPODetail.ErrorMessage));

                    var srGetDetailsByPO = await _PLService.GetDetailsByPO<PL_Detail_Collection>(detail.po_number);
                    if (!string.IsNullOrEmpty(srGetDetailsByPO.ErrorMessage))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' PO(s) error: {1}", PLNumber, srGetDetailsByPO.ErrorMessage));

                    var msgCheckQty = checkQuantity(srGetPODetail.Data, srGetDetailsByPO.Data.Where(d => d.pl_number != PLNumber), srGetDetailsByPO.Data.FirstOrDefault(d => d.pl_number == PLNumber));
                    if (!string.IsNullOrEmpty(msgCheckQty))
                        return new ApiResponse((int)ApiError.SystemError, string.Format("PO '{0}': {1}", detail.po_number, msgCheckQty));
                }

                var msgUpdate = await _PLService.SetReady(PLNumber, identity.UserId);
                if (!string.IsNullOrEmpty(msgUpdate))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Set ready for PL '{0}' error: {1}", PLNumber, msgUpdate));

                await _actionService.Create(new Action_Collection
                {
                    action_type = (int)TypeEnums.Action.PLUpdate,
                    action_content = string.Format("Set ready for PL '{0}'", PLNumber),
                    created_by = identity.UserId
                });

                foreach (var detail in srGetDetails.Data)
                {
                    var srGetPO = await _POService.Get(detail.po_number);
                    if (!string.IsNullOrEmpty(srGetPO.ErrorMessage))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Get PO '{0}' error: {1}", detail.po_number, srGetPO.ErrorMessage));

                    if (srGetPO.Data.status == (int)StatusEnums.PO.Open)
                    {
                        var msgUpdatePOStatus = await _POService.UpdateStatus(detail.po_number, (int)StatusEnums.PO.Ready, identity.UserId);
                        if (!string.IsNullOrEmpty(msgUpdatePOStatus))
                            return new ApiResponse((int)ApiError.DbError, string.Format("Update PO '{0}' status to ready error: {1}", detail.po_number, msgUpdatePOStatus));
                    }
                }

                srGet.Data.status = (int)StatusEnums.PL.Ready;
                srGet.Data.updated_by = identity.UserId;
                srGet.Data.updated_on = DateTime.Now;

                return new ApiResponse(srGet.Data);
            });
        }

        [HttpPost]
        [Route("unlock")]
        public async Task<ActionResult> UnLock([FromBody] PL_Lock_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.PLNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PL number"));

                if (string.IsNullOrEmpty(request.PONumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PO number"));

                var srGet = await _PLService.Get(request.PLNumber);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' error: {1}", request.PLNumber, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("PL '{0}'", request.PLNumber)));

                var srGetDetail = await _PLService.GetDetail<PL_Detail_Collection>(request.PLNumber, request.PONumber, false);
                if (!string.IsNullOrEmpty(srGetDetail.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' - PO '{1} error: {2}", request.PLNumber, request.PONumber, srGetDetail.ErrorMessage));

                if (srGetDetail.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, string.Format("PO '{0}' was not found on PL '{1}'", request.PONumber, request.PLNumber));

                var msgUpdateDetailStatus = await _PLService.UpdateDetailStatus(request.PLNumber, request.PONumber, srGetDetail.Data.status, identity.UserId, false);
                if (!string.IsNullOrEmpty(msgUpdateDetailStatus))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Lock PL '{0}' - PO '{1}' error: {2}", request.PLNumber, request.PONumber, msgUpdateDetailStatus));

                await _actionService.Create(new Action_Collection
                {
                    action_type = (int)TypeEnums.Action.PLUpdate,
                    action_content = string.Format("UnLocked PL '{0}' - PO '{1}'", request.PLNumber, request.PONumber),
                    created_by = identity.UserId
                });

                srGetDetail.Data.locked_by = "";
                srGetDetail.Data.locked_on = null;

                return new ApiResponse(lockedResponse(srGetDetail.Data));
            });
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost]
        [Route("changestatus")]
        public async Task<ActionResult> ChangeStatus([FromBody] PL_ChangeStatus_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                return new ApiResponse((int)ApiError.SystemError, "Method is locked");

                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.PLNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PL number"));

                if (string.IsNullOrEmpty(request.PONumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PO number"));

                if (string.IsNullOrEmpty(request.StatusCode))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Status code"));

                var srGet = await _PLService.Get(request.PLNumber);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' error: {1}", request.PLNumber, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("PL '{0}'", request.PLNumber)));

                if (srGet.Data.status == (int)StatusEnums.PL.Done)
                {
                    var PLStatusCode = srGet.Data.status.GetName<StatusEnums.PL>();
                    return new ApiResponse((int)ApiError.SystemError, string.Format("PL '{0}' status is '{1}', can not update PO status", request.PLNumber, PLStatusCode));
                }

                var srGetDetail = await _PLService.GetDetail<PL_Detail_Collection>(request.PLNumber, request.PONumber, false);
                if (!string.IsNullOrEmpty(srGetDetail.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PO '{0}' packing error: {1}", request.PONumber, srGetDetail.ErrorMessage));

                if (srGetDetail.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, string.Format("PO '{0}' was not found on PL '{1}'", request.PONumber, request.PLNumber));

                if (srGetDetail.Data.status == (int)StatusEnums.PL_PO.Shipped)
                {
                    var POStatusCode = srGetDetail.Data.status.GetName<StatusEnums.PL_PO>();
                    return new ApiResponse((int)ApiError.SystemError, string.Format(" PO '{0}' packing status is '{1}', can not update status", request.PLNumber, request.PONumber, POStatusCode));
                }

                var status = request.StatusCode.GetValue<StatusEnums.PL_PO>();
                if (!status.HasValue)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Status '{0}'", request.StatusCode)));

                if (srGetDetail.Data.status != status.Value)
                {
                    if (status.Value != (int)StatusEnums.PL_PO.Open)
                    {
                        var srGetPODetail = await _POService.GetDetails(request.PONumber);
                        if (!string.IsNullOrEmpty(srGetPODetail.ErrorMessage))
                            return new ApiResponse((int)ApiError.DbError, string.Format("Get PO '{0}' details error: {1}", request.PONumber, srGetPODetail.ErrorMessage));

                        var srGetDetailsByPO = await _PLService.GetDetailsByPO<PL_Detail_Collection>(request.PONumber);
                        if (!string.IsNullOrEmpty(srGetDetailsByPO.ErrorMessage))
                            return new ApiResponse((int)ApiError.DbError, string.Format("Get all PO '{0}' packing error: {1}", request.PONumber, srGetDetailsByPO.ErrorMessage));

                        var msgCheckQty = checkQuantity(srGetPODetail.Data, srGetDetailsByPO.Data.Where(d => d.pl_number != request.PLNumber), srGetDetailsByPO.Data.FirstOrDefault(d => d.pl_number == request.PLNumber));
                        if (!string.IsNullOrEmpty(msgCheckQty))
                            return new ApiResponse((int)ApiError.SystemError, string.Format("PO '{0}': {1}", request.PONumber, msgCheckQty));
                    }

                    var msgUpdateDetailStatus = await _PLService.UpdateDetailStatus(request.PLNumber, request.PONumber, status.Value, identity.UserId, false);
                    if (!string.IsNullOrEmpty(msgUpdateDetailStatus))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Update PL '{0}' - PO '{1}' status error: {2}", request.PLNumber, request.PONumber, msgUpdateDetailStatus));

                    await _actionService.Create(new Action_Collection
                    {
                        action_type = (int)TypeEnums.Action.PLUpload,
                        action_content = string.Format("Updated PL '{0}' - PO '{1}' status from '{2}' to '{3}'", request.PLNumber, request.PONumber, srGetDetail.Data.status, status.Value),
                        created_by = identity.UserId
                    });

                    var msgUpdateStatus = await updatePLStatus(request.PLNumber, identity.UserId);
                    if (!string.IsNullOrEmpty(msgUpdateStatus))
                        return new ApiResponse((int)ApiError.DbError, msgUpdateStatus);

                    srGetDetail.Data.status = status.Value;
                }

                return new ApiResponse(srGetDetail.Data);
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

                var srCount = await _PLService.Count(request.Filters.CreateFilter(), countInfos);
                if (!string.IsNullOrEmpty(srCount.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Count PL(s) error: {0}", srCount.ErrorMessage));

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

                var filterAfters = new List<FilterRequest>();
                var filterBefores = new List<FilterRequest>();
                if (request.Filters != null && request.Filters.Any())
                {
                    foreach (var filter in request.Filters)
                    {
                        if (filter.Names.Any(n => n.StartsWith("pl.")))
                            filterAfters.Add(filter);
                        else
                            filterBefores.Add(filter);
                    }
                }

                var stages = new BsonDocument[] {
                    filterBefores.CreateFilter(),
                    new BsonDocument("$lookup",
                        new BsonDocument {
                            { "from", "pl" },
                            { "localField", "pl_number" },
                            { "foreignField", "pl_number" },
                            { "as", "pl" }
                        }
                    ),
                    new BsonDocument("$lookup",
                        new BsonDocument
                        {
                            { "from", "user" },
                            { "let", new BsonDocument("locked_id", "$locked_by") },
                            { "pipeline",
                                new BsonArray
                                {
                                    new BsonDocument("$addFields", new BsonDocument("user_id", new BsonDocument("$toString", "$_id"))),
                                    new BsonDocument("$match",
                                        new BsonDocument("$expr",
                                            new BsonDocument("$eq", new BsonArray { "$user_id", "$$locked_id" })
                                        )
                                    )
                                }
                            },
                            { "as", "user" }
                        }
                    ),
                    new BsonDocument("$unwind", "$pl"),
                    new BsonDocument("$unwind",
                        new BsonDocument
                        {
                            { "path", "$user" },
                            { "preserveNullAndEmptyArrays", true }
                        }
                    ),
                    filterAfters.CreateFilter(),
                    new BsonDocument("$group",
                        new BsonDocument {
                            { "_id",
                                new BsonDocument {
                                    { "pl_number", "$pl.pl_number" },
                                    { "status", "$pl.status" },
                                    { "created_on", "$pl.created_on" },
                                    { "updated_on", "$pl.updated_on" }
                                }
                            },
                            { "pos",
                                new BsonDocument("$push",
                                    new BsonDocument {
                                        { "po_number", "$po_number" },
                                        { "status", "$status" },
                                        { "created_on", "$created_on" },
                                        { "total_boxes", "$total_boxes" },
                                        { "total_packed_boxes", "$total_packed_boxes" },
                                        { "total_expected_qty", "$total_expected_qty" },
                                        { "total_packed_qty", "$total_packed_qty" },
                                        { "locked_by", "$locked_by" },
                                        { "locked_name", new BsonDocument("$ifNull", new BsonArray { "$user.user_name", "" }) },
                                        { "items", "$items" }
                                    }
                                )
                            }
                        }
                    ),
                    new BsonDocument("$project",
                        new BsonDocument {
                            { "_id", 0 },
                            { "pl_number", "$_id.pl_number" },
                            { "status", "$_id.status" },
                            { "total_boxes", new BsonDocument("$sum", "$pos.total_boxes") },
                            { "total_packed_boxes", new BsonDocument("$sum", "$pos.total_packed_boxes") },
                            { "total_expected_qty", new BsonDocument("$sum", "$pos.total_expected_qty") },
                            { "total_packed_qty", new BsonDocument("$sum", "$pos.total_packed_qty") },
                            { "created_on", "$_id.created_on" },
                            { "updated_on", "$_id.updated_on" },
                            { "pos", "$pos" }
                        }
                    ),
                    request.Sorts.CreateSort()
                };

                var srRead = await _PLService.ReadDetail<object>(stages, request.PageSkip, request.PageLimit);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiReadResponse((int)ApiError.DbError, string.Format("Get PL(s) error: {0}", srRead.ErrorMessage));

                return new ApiReadResponse(srRead.Datas, srRead.Total);
            });
        }

        [HttpGet]
        [Route("getdetail")]
        public async Task<ActionResult> GetDetail(string PLNumber)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(PLNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PL number"));

                var stages = new BsonDocument[] {
                    new BsonDocument("$match", new BsonDocument("pl_number", PLNumber)),
                    new BsonDocument("$lookup", new BsonDocument {
                        { "from", "pl_detail" },
                        { "localField", "pl_number" },
                        { "foreignField", "pl_number" },
                        { "as", "po_details" }
                    })
                };

                var srRead = await _PLService.Read<object>(stages);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL(s) error: {0}", srRead.ErrorMessage));

                var data = srRead.Datas != null && srRead.Datas.Any() ? srRead.Datas.First() : new { };

                return new ApiResponse(data);
            });
        }

        [HttpGet]
        [Route("getmaxboxnumber")]
        public async Task<ActionResult> GetMaxBoxNumber()
        {
            return await ExecuteAsync(async () =>
            {
                var stages = new BsonDocument[] {
                    new BsonDocument("$unwind", "$item_details"),
                    new BsonDocument("$project", new BsonDocument {
                        { "_id", 0 },
                        { "boxNumber",
                            new BsonDocument("$convert", new BsonDocument {
                                { "input", "$item_details.box_number" },
                                { "to", "int" },
                                { "onError", 0 },
                                { "onNull", 0 }
                            })
                        }
                    }),
                    new BsonDocument("$sort", new BsonDocument("boxNumber", -1)),
                    new BsonDocument("$limit", 1)
                };

                var srRead = await _PLService.ReadDetail<object>(stages);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get max box number error: {0}", srRead.ErrorMessage));

                var maxBoxNumber = 0;
                if (srRead.Datas != null && srRead.Datas.Any())
                {
                    var data = srRead.Datas[0].MapTo<Dictionary<string, int>>();
                    if (data.ContainsKey("boxNumber"))
                        maxBoxNumber = data["boxNumber"];
                }

                return new ApiResponse(maxBoxNumber);
            });
        }

        [HttpGet]
        [Route("getallpostatus")]
        public ActionResult GetAllPOStatus()
        {
            return Execute(() =>
            {
                var status = ConvertEnumToDictionary<StatusEnums.PL_PO>();
                return new ApiResponse(status);
            });
        }

        [HttpGet]
        [Route("getallboxstatus")]
        public ActionResult GetAllBoxStatus()
        {
            return Execute(() =>
            {
                var status = ConvertEnumToDictionary<StatusEnums.PL_Box>();
                return new ApiResponse(status);
            });
        }

        #region PDA actions

        [HttpPost]
        [Route("pda_upload")]
        public async Task<ActionResult> PDA_Upload([FromBody] PL_Upload_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.PLNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PL number"));

                if (string.IsNullOrEmpty(request.PONumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PO number"));

                if (!request.Details.Any())
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("Detail(s)"));

                if (request.Details.Any(i => string.IsNullOrEmpty(i.BoxNumber)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("Box number(s)"));

                if (request.Details.Any(i => string.IsNullOrEmpty(i.ItemNumber)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("Item number(s)"));

                if (request.Details.Any(i => string.IsNullOrEmpty(i.ColorNumber)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("Color number(s)"));

                if (request.Details.Any(i => string.IsNullOrEmpty(i.Inseam)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("Inseam(s)"));

                if (request.Details.Any(i => string.IsNullOrEmpty(i.Size)))
                    return new ApiResponse((int)ApiError.Requireds, ApiError.Requireds.GetDecription("Size(s)"));

                //if (request.Details.Any(i => i.ExpectedQty <= 0))
                //    return new ApiResponse((int)ApiError.Requireds, "Expected qty(s) must be greater than 0");

                //if (request.Details.Any(i => i.PackedQty <= 0))
                //    return new ApiResponse((int)ApiError.Requireds, "Packed qty(s) must be greater than 0");

                var srGet = await _PLService.Get(request.PLNumber);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' error: {1}", request.PLNumber, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("PL '{0}'", request.PLNumber)));

                if (srGet.Data.status == (int)StatusEnums.PL.Done)
                {
                    var PLStatusCode = srGet.Data.status.GetName<StatusEnums.PL>();
                    return new ApiResponse((int)ApiError.SystemError, string.Format("PL '{0}' status is '{1}', can not upload PO", request.PLNumber, PLStatusCode));
                }

                var srGetDetail = await _PLService.GetDetail<PL_Detail_Collection>(request.PLNumber, request.PONumber);
                if (!string.IsNullOrEmpty(srGetDetail.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' - PO '{1}' error: {2}", request.PLNumber, request.PONumber, srGetDetail.ErrorMessage));

                if (srGetDetail.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, string.Format("PO '{0}' was not found on PL '{1}'", request.PONumber, request.PLNumber));

                if (new int[] { (int)StatusEnums.PL_PO.Packed, (int)StatusEnums.PL_PO.Shipped }.Contains(srGetDetail.Data.status))
                {
                    var POStatusCode = srGetDetail.Data.status.GetName<StatusEnums.PL_PO>();
                    return new ApiResponse((int)ApiError.SystemError, string.Format("PL '{0}' - PO '{1}' status is '{2}', can not upload", request.PLNumber, request.PONumber, POStatusCode));
                }

                foreach (var GBItem in request.Details.GroupBy(d => new { d.ItemNumber, d.ColorNumber, d.Inseam, d.Size }))
                {
                    var existedItems = srGetDetail.Data.item_details.Where(i => i.item_number == GBItem.Key.ItemNumber && i.color_number == GBItem.Key.ColorNumber && i.inseam == GBItem.Key.Inseam && i.size == GBItem.Key.Size);
                    if (!existedItems.Any())
                        return new ApiResponse((int)ApiError.NotFound, string.Format("Item '{0}' - Color '{1}' - Inseam '{2}' - Size '{3}' was not found on PL '{4}' - PO '{5}'", GBItem.Key.ItemNumber, GBItem.Key.ColorNumber, GBItem.Key.Inseam, GBItem.Key.Size, request.PLNumber, request.PONumber));

                    foreach (var item in GBItem)
                    {
                        if (GBItem.Count(i => i.BoxNumber == item.BoxNumber) > 1)
                            return new ApiResponse((int)ApiError.Dupplicated, string.Format("Box '{0}' - Item '{1}' - Color '{2}' - Inseam '{3}' - Size '{4}' is dupplicated in list", item.BoxNumber, GBItem.Key.ItemNumber, GBItem.Key.ColorNumber, GBItem.Key.Inseam, GBItem.Key.Size));

                        if (string.IsNullOrEmpty(item.BoxStatusCode))
                            return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Box status code"));

                        var boxStatus = item.BoxStatusCode.GetValue<StatusEnums.PL_Box>();
                        if (!boxStatus.HasValue)
                            return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Box status '{0}'", item.BoxStatusCode)));

                        var existedItem = existedItems.FirstOrDefault(i => i.box_number == item.BoxNumber);
                        if (existedItem == null)
                        {
                            var boxInfo = srGetDetail.Data.item_details.FirstOrDefault(i => i.box_number == item.BoxNumber);
                            if (boxInfo == null)
                                return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Box '{0}'", item.BoxNumber)));

                            var itemInfo = existedItems.FirstOrDefault();

                            srGetDetail.Data.item_details.Add(new PL_Item_Detail_Collection
                            {
                                box_number = item.BoxNumber,
                                box_dimension = boxInfo.box_dimension,
                                box_weight = boxInfo.box_weight,
                                box_status = boxStatus.Value,
                                item_number = item.ItemNumber,
                                item_description = itemInfo.item_description,
                                color_number = item.ColorNumber,
                                color_description = itemInfo.color_description,
                                inseam = item.Inseam,
                                size = item.Size,
                                barcode = itemInfo.barcode,
                                item_weight = itemInfo.item_weight,
                                expected_qty = item.ExpectedQty,
                                packed_qty = item.PackedQty,
                                qty_status = item.ExpectedQty == item.PackedQty ? (int)StatusEnums.PL_Qty.Match : (int)StatusEnums.PL_Qty.Diff,
                                note = item.Note
                            });
                        }
                        else
                        {
                            if (existedItem.box_status == (int)StatusEnums.PL_Box.Done)
                                return new ApiResponse((int)ApiError.SystemError, string.Format("Box '{0}' status is done, can not upload this box", item.BoxNumber));

                            existedItem.box_status = boxStatus.Value;
                            existedItem.packed_qty = item.PackedQty;
                            existedItem.qty_status = existedItem.expected_qty == existedItem.packed_qty ? (int)StatusEnums.PL_Qty.Match : (int)StatusEnums.PL_Qty.Diff;
                            existedItem.note = item.Note;
                        }
                    }
                }

                foreach (var GBItem in srGetDetail.Data.item_details.GroupBy(d => new { d.item_number, d.color_number, d.inseam, d.size }))
                {
                    var expectedQty = GBItem.Sum(i => i.expected_qty);
                    var packedQty = GBItem.Sum(i => i.packed_qty);
                    if (packedQty > expectedQty)
                        return new ApiResponse((int)ApiError.NotFound, string.Format("Item '{0}' - Color '{1}' - Inseam '{2}' - Size '{3}': Packed qty ({4}) is out of expected qty ({5})", GBItem.Key.item_number, GBItem.Key.color_number, GBItem.Key.inseam, GBItem.Key.size, packedQty, expectedQty));
                }

                var currentStatus = srGetDetail.Data.status;

                if (!srGetDetail.Data.item_details.Any(id => id.box_status != (int)StatusEnums.PL_Box.Open))
                    srGetDetail.Data.status = (int)StatusEnums.PL_PO.Active;
                else if (!srGetDetail.Data.item_details.Any(id => id.box_status != (int)StatusEnums.PL_Box.Done))
                    srGetDetail.Data.status = (int)StatusEnums.PL_PO.Packed;
                else
                    srGetDetail.Data.status = (int)StatusEnums.PL_PO.PartialPacked;

                srGetDetail.Data.updated_by = identity.UserId;

                var msgUpdateDetail = await _PLService.UpdateDetail(request.PLNumber, request.PONumber, srGetDetail.Data);
                if (!string.IsNullOrEmpty(msgUpdateDetail))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Update PL '{0}' - PO '{1}' error: {2}", request.PLNumber, request.PONumber, msgUpdateDetail));

                await _actionService.Create(new Action_Collection
                {
                    action_type = (int)TypeEnums.Action.PLUpload,
                    action_content = string.Format("Uploaded PL '{0}' - PO '{1}'", request.PLNumber, request.PONumber),
                    created_by = identity.UserId
                });

                if (srGetDetail.Data.status != currentStatus)
                {
                    var msgUpdateStatus = await updatePLStatus(request.PLNumber, identity.UserId);
                    if (!string.IsNullOrEmpty(msgUpdateStatus))
                        return new ApiResponse((int)ApiError.DbError, msgUpdateStatus);

                    var msgUpdatePOStatus = await PDAUpload_UpdatePOStatus(request.PONumber, identity.UserId);
                    if (!string.IsNullOrEmpty(msgUpdatePOStatus))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Update PO '{0}' status error: {1}", request.PONumber, msgUpdatePOStatus));
                }

                return new ApiResponse(srGetDetail.Data);
            });
        }

        [HttpPost]
        [Route("pda_lock")]
        public async Task<ActionResult> PDA_Lock([FromBody] PL_Lock_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.PLNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PL number"));

                if (string.IsNullOrEmpty(request.PONumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PO number"));

                var srGet = await _PLService.Get(request.PLNumber);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' error: {1}", request.PLNumber, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("PL '{0}'", request.PLNumber)));

                if (srGet.Data.status == (int)StatusEnums.PL.Done)
                {
                    var PLStatusCode = srGet.Data.status.GetName<StatusEnums.PL>();
                    return new ApiResponse((int)ApiError.SystemError, string.Format("PL '{0}' status is '{1}', can not upload PL", request.PLNumber, PLStatusCode));
                }

                var srGetDetail = await _PLService.GetDetail<PL_Detail_Collection>(request.PLNumber, request.PONumber, false);
                if (!string.IsNullOrEmpty(srGetDetail.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' - PO '{1} error: {2}", request.PLNumber, request.PONumber, srGetDetail.ErrorMessage));

                if (srGetDetail.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, string.Format("PO '{0}' was not found on PL '{1}'", request.PONumber, request.PLNumber));

                if (!string.IsNullOrEmpty(srGetDetail.Data.locked_by) && srGetDetail.Data.locked_by != identity.UserId)
                {
                    var response = new ApiResponse((int)ApiError.Locked, ApiError.Locked.GetDecription(string.Format("PL '{0}' - PO '{1}'", request.PLNumber, request.PONumber)) + " by another user");
                    response.data = lockedResponse(srGetDetail.Data);
                    return response;
                }

                var currentStatus = srGetDetail.Data.status;

                if (srGetDetail.Data.status == (int)StatusEnums.PL_PO.Ready)
                    srGetDetail.Data.status = (int)StatusEnums.PL_PO.Active;

                var msgUpdateDetailStatus = await _PLService.UpdateDetailStatus(request.PLNumber, request.PONumber, srGetDetail.Data.status, identity.UserId, true);
                if (!string.IsNullOrEmpty(msgUpdateDetailStatus))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Lock PL '{0}' - PO '{1}' error: {2}", request.PLNumber, request.PONumber, msgUpdateDetailStatus));

                await _actionService.Create(new Action_Collection
                {
                    action_type = (int)TypeEnums.Action.PLUpdate,
                    action_content = string.Format("Locked PL '{0}' - PO '{1}'", request.PLNumber, request.PONumber),
                    created_by = identity.UserId
                });

                if (srGetDetail.Data.status != currentStatus)
                {
                    var msgUpdateStatus = await updatePLStatus(request.PLNumber, identity.UserId);
                    if (!string.IsNullOrEmpty(msgUpdateStatus))
                        return new ApiResponse((int)ApiError.DbError, msgUpdateStatus);

                    var srGetPO = await _POService.Get(request.PONumber);
                    if (!string.IsNullOrEmpty(srGetPO.ErrorMessage))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Get PO '{0}' error: {1}", request.PONumber, srGetPO.ErrorMessage));

                    if (srGetPO.Data.status == (int)StatusEnums.PO.Ready)
                    {
                        var msgUpdatePOStatus = await _POService.UpdateStatus(request.PONumber, (int)StatusEnums.PO.Active, identity.UserId);
                        if (!string.IsNullOrEmpty(msgUpdatePOStatus))
                            return new ApiResponse((int)ApiError.DbError, string.Format("Update PO '{0}' status to ready error: {1}", request.PONumber, msgUpdatePOStatus));
                    }
                }

                srGetDetail.Data.locked_by = identity.UserId;
                srGetDetail.Data.locked_on = DateTime.Now;

                return new ApiResponse(lockedResponse(srGetDetail.Data));
            });
        }

        [HttpPost]
        [Route("pda_unlock")]
        public async Task<ActionResult> PDA_UnLock([FromBody] PL_Lock_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.PLNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PL number"));

                if (string.IsNullOrEmpty(request.PONumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PO number"));

                var srGet = await _PLService.Get(request.PLNumber);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' error: {1}", request.PLNumber, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("PL '{0}'", request.PLNumber)));

                if (srGet.Data.status == (int)StatusEnums.PL.Done)
                {
                    var PLStatusCode = srGet.Data.status.GetName<StatusEnums.PL>();
                    return new ApiResponse((int)ApiError.SystemError, string.Format("PL '{0}' status is '{1}', can not upload PL", request.PLNumber, PLStatusCode));
                }

                var srGetDetail = await _PLService.GetDetail<PL_Detail_Collection>(request.PLNumber, request.PONumber, false);
                if (!string.IsNullOrEmpty(srGetDetail.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' - PO '{1} error: {2}", request.PLNumber, request.PONumber, srGetDetail.ErrorMessage));

                if (srGetDetail.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, string.Format("PO '{0}' was not found on PL '{1}'", request.PONumber, request.PLNumber));

                if (!string.IsNullOrEmpty(srGetDetail.Data.locked_by) && srGetDetail.Data.locked_by != identity.UserId)
                {
                    var response = new ApiResponse((int)ApiError.Locked, ApiError.Locked.GetDecription(string.Format("PL '{0}' - PO '{1}'", request.PLNumber, request.PONumber)) + " by another user");
                    response.data = lockedResponse(srGetDetail.Data);
                    return response;
                }

                var msgUpdateDetailStatus = await _PLService.UpdateDetailStatus(request.PLNumber, request.PONumber, srGetDetail.Data.status, identity.UserId, false);
                if (!string.IsNullOrEmpty(msgUpdateDetailStatus))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Lock PL '{0}' - PO '{1}' error: {2}", request.PLNumber, request.PONumber, msgUpdateDetailStatus));

                await _actionService.Create(new Action_Collection
                {
                    action_type = (int)TypeEnums.Action.PLUpdate,
                    action_content = string.Format("UnLocked PL '{0}' - PO '{1}'", request.PLNumber, request.PONumber),
                    created_by = identity.UserId
                });

                srGetDetail.Data.locked_by = "";
                srGetDetail.Data.locked_on = null;

                return new ApiResponse(lockedResponse(srGetDetail.Data));
            });
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost]
        [Route("pda_changestatus")]
        public async Task<ActionResult> PDA_ChangeStatus([FromBody] PL_ChangeStatus_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                return new ApiResponse((int)ApiError.SystemError, "Method is locked");

                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.PLNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PL number"));

                if (string.IsNullOrEmpty(request.PONumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PO number"));

                if (string.IsNullOrEmpty(request.StatusCode))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Status code"));

                var srGet = await _PLService.Get(request.PLNumber);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' error: {1}", request.PLNumber, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("PL '{0}'", request.PLNumber)));

                if (srGet.Data.status == (int)StatusEnums.PL.Done)
                {
                    var PLStatusCode = srGet.Data.status.GetName<StatusEnums.PL>();
                    return new ApiResponse((int)ApiError.SystemError, string.Format("PL '{0}' status is '{1}', can not PO status", request.PLNumber, PLStatusCode));
                }

                var srGetDetail = await _PLService.GetDetail<PL_Detail_Collection>(request.PLNumber, request.PONumber);
                if (!string.IsNullOrEmpty(srGetDetail.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL '{0}' - PO '{1} error: {2}", request.PLNumber, request.PONumber, srGetDetail.ErrorMessage));

                if (srGetDetail.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, string.Format("PO '{0}' was not found on PL '{1}'", request.PONumber, request.PLNumber));

                if (new int[] { (int)StatusEnums.PL_PO.Packed, (int)StatusEnums.PL_PO.Shipped }.Contains(srGetDetail.Data.status))
                {
                    var POStatusCode = srGetDetail.Data.status.GetName<StatusEnums.PL_PO>();
                    return new ApiResponse((int)ApiError.SystemError, string.Format("PL '{0}' - PO '{1}' status is '{2}', can not update status", request.PLNumber, request.PONumber, POStatusCode));
                }

                var status = request.StatusCode.GetValue<StatusEnums.PL_PO>();
                if (!status.HasValue)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Status '{0}'", request.StatusCode)));

                if (!new int[] { (int)StatusEnums.PL_PO.PartialPacked, (int)StatusEnums.PL_PO.Packed }.Contains(status.Value))
                    return new ApiResponse((int)ApiError.SystemError, string.Format("New status is '{0}'. Only allow update status to 'Partial' or 'Packed'", request.StatusCode));

                if (srGetDetail.Data.status != status.Value)
                {
                    if (status.Value == (int)StatusEnums.PL_PO.Packed)
                    {
                        var boxNotDones = srGetDetail.Data.item_details.Where(i => i.box_status != (int)StatusEnums.PL_Box.Done);
                        if (boxNotDones.Any())
                            return new ApiResponse((int)ApiError.SystemError, string.Format("Box '{0}' is open. can not update PL '{1}' - PO '{2}' status to packed", boxNotDones.FirstOrDefault().box_number, request.PLNumber, request.PONumber));
                    }

                    var msgUpdateDetailStatus = await _PLService.UpdateDetailStatus(request.PLNumber, request.PONumber, status.Value, identity.UserId, false);
                    if (!string.IsNullOrEmpty(msgUpdateDetailStatus))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Update PL '{0}' - PO '{1}' status error: {2}", request.PLNumber, request.PONumber, msgUpdateDetailStatus));

                    await _actionService.Create(new Action_Collection
                    {
                        action_type = (int)TypeEnums.Action.PLUpload,
                        action_content = string.Format("Updated PL '{0}' - PO '{1}' status from '{2}' to '{3}'", request.PLNumber, request.PONumber, srGetDetail.Data.status, status.Value),
                        created_by = identity.UserId
                    });

                    var msgUpdateStatus = await updatePLStatus(request.PLNumber, identity.UserId);
                    if (!string.IsNullOrEmpty(msgUpdateStatus))
                        return new ApiResponse((int)ApiError.DbError, msgUpdateStatus);

                    var msgUpdatePOStatus = await PDAUpload_UpdatePOStatus(request.PONumber, identity.UserId);
                    if (!string.IsNullOrEmpty(msgUpdatePOStatus))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Update PO '{0}' status error: {1}", request.PONumber, msgUpdatePOStatus));

                    srGetDetail.Data.status = status.Value;
                }

                return new ApiResponse(srGetDetail.Data);
            });
        }

        [HttpPost]
        [Route("pda_gets")]
        public async Task<ActionResult> PDA_Gets([FromBody] ReadRequest request)
        {
            return await ExecuteReadAsync(async () =>
            {
                if (request == null)
                    request = new ReadRequest();

                if (request.Sorts == null || !request.Sorts.Any())
                    request.Sorts.Add(new SortRequest { Name = "created_on", Type = "desc" });

                var stages = new BsonDocument[] {
                    request.Filters.CreateFilter(),
                    new BsonDocument("$match",
                        new BsonDocument("status",
                            new BsonDocument("$nin", new BsonArray { (int)StatusEnums.PL_PO.Open, (int)StatusEnums.PL_PO.Packed, (int)StatusEnums.PL_PO.Shipped })
                        )
                    ),
                    new BsonDocument("$lookup",
                        new BsonDocument {
                            { "from", "pl" },
                            { "localField", "pl_number" },
                            { "foreignField", "pl_number" },
                            { "as", "pl" }
                        }
                    ),
                    new BsonDocument("$lookup",
                        new BsonDocument
                        {
                            { "from", "user" },
                            { "let", new BsonDocument("locked_id", "$locked_by") },
                            { "pipeline",
                                new BsonArray
                                {
                                    new BsonDocument("$addFields", new BsonDocument("user_id", new BsonDocument("$toString", "$_id"))),
                                    new BsonDocument("$match",
                                        new BsonDocument("$expr",
                                            new BsonDocument("$eq", new BsonArray { "$user_id", "$$locked_id" })
                                        )
                                    )
                                }
                            },
                            { "as", "user" }
                        }
                    ),
                    new BsonDocument("$unwind", "$pl"),
                    new BsonDocument("$unwind",
                        new BsonDocument
                        {
                            { "path", "$user" },
                            { "preserveNullAndEmptyArrays", true }
                        }
                    ),
                    new BsonDocument("$group",
                        new BsonDocument {
                            { "_id",
                                new BsonDocument {
                                    { "pl_number", "$pl.pl_number" },
                                    { "created_on", "$pl.created_on" }
                                }
                            },
                            { "po_items",
                                new BsonDocument("$push",
                                    new BsonDocument {
                                        { "po_number", "$po_number" },
                                        { "po_status", "$status" },
                                        { "locked_by", new BsonDocument("$ifNull", new BsonArray { "$locked_by", "" }) },
                                        { "locked_name", new BsonDocument("$ifNull", new BsonArray { "$user.user_name", "" }) },
                                        { "items", "$items" }
                                    }
                                )
                            },
                            { "total_boxes", new BsonDocument("$sum", "$total_boxes") },
                            { "total_packed_boxes", new BsonDocument("$sum", "$total_packed_boxes") },
                            { "total_expected_qty", new BsonDocument("$sum", "$total_expected_qty") },
                            { "total_packed_qty", new BsonDocument("$sum", "$total_packed_qty") }
                        }
                    ),
                    new BsonDocument("$unwind", "$po_items"),
                    new BsonDocument("$unwind", "$po_items.items"),
                    new BsonDocument("$group",
                        new BsonDocument {
                            { "_id",
                                new BsonDocument {
                                    { "pl_number", "$_id.pl_number" },
                                    { "created_on", "$_id.created_on" },
                                    { "total_boxes", "$total_boxes" },
                                    { "total_packed_boxes", "$total_packed_boxes" },
                                    { "total_expected_qty", "$total_expected_qty" },
                                    { "total_packed_qty", "$total_packed_qty" }
                                }
                            },
                            { "po_items",
                                new BsonDocument("$push",
                                    new BsonDocument {
                                        { "po_number", "$po_items.po_number" },
                                        { "po_status", "$po_items.po_status" },
                                        { "locked_by", "$po_items.locked_by" },
                                        { "locked_name", "$po_items.locked_name" },
                                        { "item_number", "$po_items.items.item_number" }
                                    }
                                )
                            }
                        }
                    ),
                    new BsonDocument("$project",
                        new BsonDocument {
                            { "_id", 0 },
                            { "pl_number", "$_id.pl_number" },
                            { "created_on", "$_id.created_on" },
                            { "total_boxes", "$_id.total_boxes" },
                            { "total_packed_boxes", "$_id.total_packed_boxes" },
                            { "total_expected_qty", "$_id.total_expected_qty" },
                            { "total_packed_qty", "$_id.total_packed_qty" },
                            { "po_items", "$po_items" }
                        }
                    ),
                    request.Sorts.CreateSort()
                };

                var srRead = await _PLService.ReadDetail<object>(stages, request.PageSkip, request.PageLimit);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiReadResponse((int)ApiError.DbError, string.Format("Get PL(s) error: {0}", srRead.ErrorMessage));

                return new ApiReadResponse(srRead.Datas, srRead.Total);
            });
        }

        [HttpPost]
        [Route("pda_getitems")]
        public async Task<ActionResult> PDA_GetItems([FromBody] PL_GetItem_Request request)
        {
            return await ExecuteAsync(async () =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.PLNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PL number"));

                if (string.IsNullOrEmpty(request.PONumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("PO number"));

                var stages = new BsonDocument[] {
                    new BsonDocument("$match",
                        new BsonDocument {
                            { "pl_number", request.PLNumber },
                            { "po_number", request.PONumber }
                        }
                    ),
                    new BsonDocument("$unwind", "$item_details"),
                    new BsonDocument("$group",
                        new BsonDocument {
                            { "_id",
                                new BsonDocument
                                {
                                    { "item_number", "$item_details.item_number" },
                                    { "item_description", "$item_details.item_description" },
                                    { "box_number", "$item_details.box_number" },
                                    { "box_dimension", "$item_details.box_dimension" },
                                    { "box_status", "$item_details.box_status" }
                                }
                            },
                            { "extras",
                                new BsonDocument("$push", new BsonDocument
                                {
                                    { "color_number", "$item_details.color_number" },
                                    { "color_description", "$item_details.color_description" },
                                    { "inseam", "$item_details.inseam" },
                                    { "size", "$item_details.size" },
                                    { "barcode", "$item_details.barcode" },
                                    { "expected_qty", "$item_details.expected_qty" },
                                    { "packed_qty", "$item_details.packed_qty" }
                                })
                            }
                        }
                    ),
                    new BsonDocument("$group",
                        new BsonDocument {
                            { "_id",
                                new BsonDocument
                                {
                                    { "item_number", "$_id.item_number" },
                                    { "item_description", "$_id.item_description" }
                                }
                            },
                            { "boxes",
                                new BsonDocument("$push", new BsonDocument {
                                    { "box_number", "$_id.box_number" },
                                    { "box_dimension", "$_id.box_dimension" },
                                    { "box_status", "$_id.box_status" },
                                    { "extras", "$extras" }
                                })
                            }
                        }
                    ),
                    new BsonDocument("$project",
                        new BsonDocument
                        {
                            { "_id", 0 },
                            { "item_number", "$_id.item_number" },
                            { "item_description", "$_id.item_description" },
                            { "boxes", "$boxes" }
                        }
                    )
                };

                var srRead = await _PLService.ReadDetail<object>(stages);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get PL items error: {0}", srRead.ErrorMessage));

                return new ApiResponse(srRead.Datas);
            });
        }

        #region Private methods

        object lockedResponse(PL_Detail_Collection pl_detail)
        {
            var locked_name = "";
            var sgGetUser = _userService.GetById(pl_detail.locked_by);
            sgGetUser.Wait();
            if (sgGetUser.Result.Data != null)
                locked_name = sgGetUser.Result.Data.user_name;

            return new
            {
                pl_detail.id,
                pl_detail.pl_number,
                pl_detail.po_number,
                pl_detail.customer_code,
                pl_detail.status,
                pl_detail.total_boxes,
                pl_detail.total_packed_boxes,
                pl_detail.total_expected_qty,
                pl_detail.total_packed_qty,
                pl_detail.locked_by,
                locked_name,
                pl_detail.locked_on
            };
        }

        #endregion

        #endregion

        #region Private methods

        int getBoxNumber(string boxNumber, int length)
        {
            try
            {
                boxNumber = boxNumber.Remove(0, boxNumber.Length - length);
                return int.Parse(boxNumber);
            }
            catch
            {
                return 0;
            }
        }

        string checkQuantity(IEnumerable<PO_Detail_Collection> PODetails, IEnumerable<PL_Detail_Collection> PLDetails, PL_Request newPLDetail)
        {
            var message = "";
            foreach (var PODetail in PODetails)
            {
                var POQty = newPLDetail.UseProduceQty ? PODetail.additional_qty : PODetail.original_qty;
                var expectedQty = PLDetails.Sum(d => d.item_details.Where(i => i.barcode == PODetail.barcode && i.box_status == (int)StatusEnums.PL_Box.Open).Sum(i => i.expected_qty));
                var packedQty = PLDetails.Sum(d => d.item_details.Where(i => i.barcode == PODetail.barcode && i.box_status == (int)StatusEnums.PL_Box.Done).Sum(i => i.packed_qty));
                var newPLQty = newPLDetail.Details.Where(i => i.ItemNumber == PODetail.item_number && i.ColorNumber == PODetail.color_number && i.Inseam == PODetail.inseam && i.Size == PODetail.size).Sum(i => i.ExpectedQty);
                if (expectedQty + packedQty + newPLQty > POQty)
                {
                    message = string.Format("Item '{0}' - Color '{1}' - Inseam '{2}' - Size '{3}' - Packing qty ({4}) is out of expected qty ({5})", PODetail.item_number, PODetail.color_number, PODetail.inseam, PODetail.size, expectedQty + packedQty + newPLQty, POQty);
                    break;
                }
            }
            return message;
        }

        string checkQuantity(IEnumerable<PO_Detail_Collection> PODetails, IEnumerable<PL_Detail_Collection> PLDetails, PL_Detail_Collection currentPLDetail)
        {
            var message = "";
            foreach (var PODetail in PODetails)
            {
                var POQty = currentPLDetail.use_produce_qty ? PODetail.additional_qty : PODetail.original_qty;
                var expectedQty = PLDetails.Sum(d => d.item_details.Where(i => i.barcode == PODetail.barcode && i.box_status == (int)StatusEnums.PL_Box.Open).Sum(i => i.expected_qty));
                var packedQty = PLDetails.Sum(d => d.item_details.Where(i => i.barcode == PODetail.barcode && i.box_status == (int)StatusEnums.PL_Box.Done).Sum(i => i.packed_qty));
                var currentPLQty = currentPLDetail.item_details.Where(i => i.barcode == PODetail.barcode).Sum(i => i.expected_qty);
                if (expectedQty + packedQty + currentPLQty > POQty)
                {
                    message = string.Format("Item '{0}' - Color '{1}' - Inseam '{2}' - Size '{3}' - Packing qty ({4}) is out of expected qty ({5})", PODetail.item_number, PODetail.color_number, PODetail.inseam, PODetail.size, expectedQty + packedQty + currentPLQty, POQty);
                    break;
                }
            }
            return message;
        }

        async Task<string> updatePLStatus(string PLNumber, string userId)
        {
            var srGetDetails = await _PLService.GetDetails<PL_Detail_Collection>(PLNumber, false);
            if (!string.IsNullOrEmpty(srGetDetails.ErrorMessage))
                return string.Format("Get PL '{0}' PO(s) error: {1}", PLNumber, srGetDetails.ErrorMessage);

            var newPLStatus = (int)StatusEnums.PL.InProgress;
            if (!srGetDetails.Data.Any(po => po.status != (int)StatusEnums.PL_PO.Open))
                newPLStatus = (int)StatusEnums.PL.Draft;
            else if (!srGetDetails.Data.Any(po => po.status != (int)StatusEnums.PL_PO.Ready))
                newPLStatus = (int)StatusEnums.PL.Ready;
            else if (!srGetDetails.Data.Any(po => po.status != (int)StatusEnums.PL_PO.Active))
                newPLStatus = (int)StatusEnums.PL.Active;
            else if (!srGetDetails.Data.Any(po => po.status != (int)StatusEnums.PL_PO.Packed))
                newPLStatus = (int)StatusEnums.PL.Done;
            else if (!srGetDetails.Data.Any(po => po.status != (int)StatusEnums.PL_PO.Shipped))
                newPLStatus = (int)StatusEnums.PL.Done;

            var msgUpdateStatus = await _PLService.UpdateStatus(PLNumber, newPLStatus, userId);
            if (!string.IsNullOrEmpty(msgUpdateStatus))
                return string.Format("Update PL '{0}' status error: {1}", PLNumber, msgUpdateStatus);

            return "";
        }

        async Task<string> PDAUpload_UpdatePOStatus(string PONumber, string userId)
        {
            var srGetPODetail = await _POService.GetDetails(PONumber);
            if (!string.IsNullOrEmpty(srGetPODetail.ErrorMessage))
                return string.Format("Get PO '{0}' details error: {1}", PONumber, srGetPODetail.ErrorMessage);

            var srGetDetailsByPO = await _PLService.GetDetailsByPO<PL_Detail_Collection>(PONumber);
            if (!string.IsNullOrEmpty(srGetDetailsByPO.ErrorMessage))
                return string.Format("Get all PO '{0}' packing error: {1}", PONumber, srGetDetailsByPO.ErrorMessage);

            var packedDetails = srGetDetailsByPO.Data.Where(d => new int[] { (int)StatusEnums.PL_PO.PartialPacked, (int)StatusEnums.PL_PO.Packed }.Contains(d.status));

            var newPOStatus = (int)StatusEnums.PO.Packed;
            foreach (var PODetail in srGetPODetail.Data)
            {
                var POQty = packedDetails.FirstOrDefault().use_produce_qty ? PODetail.additional_qty : PODetail.original_qty;
                var packedQty = packedDetails.Sum(d => d.item_details.Where(i => i.barcode == PODetail.barcode).Sum(i => i.packed_qty));
                if (packedQty > POQty)
                    return string.Format("Item '{0}' - Color '{1}' - Inseam '{2}' - Size '{3}' - Packed qty ({4}) is out of expected qty ({5})", PODetail.item_number, PODetail.color_number, PODetail.inseam, PODetail.size, packedQty, POQty);

                if (packedQty < POQty)
                {
                    newPOStatus = (int)StatusEnums.PO.PartialPacked;
                    break;
                }
            }

            var msgUpdatePOStatus = await _POService.UpdateStatus(PONumber, newPOStatus, userId);
            if (!string.IsNullOrEmpty(msgUpdatePOStatus))
                return string.Format("Update PO '{0}' status error: {1}", PONumber, msgUpdatePOStatus);

            return "";
        }

        #endregion
    }
}
