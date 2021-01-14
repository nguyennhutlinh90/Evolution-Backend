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
    [Authorize(Roles = Constants.Role.Admin)]
    [ApiController]
    [Route("api/item")]
    public class ItemController : ApiControllerBase
    {
        private readonly IItemService _itemService;
        public ItemController(IItemService itemService)
        {
            _itemService = itemService;
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost]
        [Route("upload")]
        public async Task<ActionResult> Upload([FromBody] List<ItemBarcode_Request> request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null || !request.Any())
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                var errorLine = 0;
                var errors = new List<string>();
                var saveds = new List<Item_Collection>();
                foreach (var ib in request)
                {
                    errorLine++;

                    if (string.IsNullOrEmpty(ib.ItemNumber))
                    {
                        errors.Add(ApiError.Required.GetDecription(string.Format("Line {0}: Item number", errorLine)));
                        continue;
                    }

                    if (string.IsNullOrEmpty(ib.ItemDescription))
                    {
                        errors.Add(ApiError.Required.GetDecription(string.Format("Line {0}: Item '{1}' description", errorLine, ib.ItemNumber)));
                        continue;
                    }

                    if (string.IsNullOrEmpty(ib.ColorNumber))
                    {
                        errors.Add(ApiError.Required.GetDecription(string.Format("Line {0}: Item '{1}' - Color number", errorLine, ib.ItemNumber)));
                        continue;
                    }

                    if (string.IsNullOrEmpty(ib.ColorDescription))
                    {
                        errors.Add(ApiError.Required.GetDecription(string.Format("Line {0}: Item '{1}' - Color '{2}' description", errorLine, ib.ItemNumber, ib.ColorNumber)));
                        continue;
                    }

                    if (string.IsNullOrEmpty(ib.Inseam))
                    {
                        errors.Add(ApiError.Required.GetDecription(string.Format("Line {0}: Item '{1}' - Color '{2}' - Inseam", errorLine, ib.ItemNumber, ib.ColorNumber)));
                        continue;
                    }

                    if (string.IsNullOrEmpty(ib.Size))
                    {
                        errors.Add(ApiError.Required.GetDecription(string.Format("Line {0}: Item '{1}' - Color '{2}' - Inseam '{3}' - Size", errorLine, ib.ItemNumber, ib.ColorNumber, ib.Inseam)));
                        continue;
                    }

                    if (saveds.Any(s => s.item_number == ib.ItemNumber && s.color_number == ib.ColorNumber && s.size == ib.Size && s.inseam == ib.Inseam))
                    {
                        errors.Add(ApiError.Dupplicated.GetDecription(string.Format("Line {0}: Item '{1}' - color '{2}' - inseam '{3}' - size '{4}'", errorLine, ib.ItemNumber, ib.ColorNumber, ib.Inseam, ib.Size)));
                        continue;
                    }

                    if (string.IsNullOrEmpty(ib.Barcode))
                    {
                        errors.Add(ApiError.Required.GetDecription(string.Format("Line {0}: Item '{1}' - Color '{2}' - Inseam '{3}' - Size '{4}': Barcode", errorLine, ib.ItemNumber, ib.ColorNumber, ib.Inseam, ib.Size)));
                        continue;
                    }

                    if (saveds.Any(s => s.barcode == ib.Barcode))
                    {
                        errors.Add(ApiError.Dupplicated.GetDecription(string.Format("Line {0}: Barcode '{1}'", errorLine, ib.Barcode)));
                        continue;
                    }

                    var srGet = await _itemService.Get(ib.ItemNumber, ib.ColorNumber, ib.Inseam, ib.Size);
                    if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    {
                        errors.Add(string.Format("Get item '{0}' - color '{1}' - inseam '{2}' - size '{3}' error: {4}", ib.ItemNumber, ib.ColorNumber, ib.Inseam, ib.Size, srGet.ErrorMessage));
                        continue;
                    }

                    //if (srGet.Data != null)
                    //{
                    //    errors.Add(ApiError.Existed.GetDecription(string.Format("Item '{0}' - color '{1}' - inseam '{2}' - size '{3}'", ib.ItemNumber, ib.ColorNumber, ib.Inseam, ib.Size)));
                    //    continue;
                    //}

                    if (srGet.Data == null || (srGet.Data != null && srGet.Data.barcode != ib.Barcode))
                    {
                        var srGetByBarcode = await _itemService.GetBarcode(ib.Barcode);
                        if (!string.IsNullOrEmpty(srGetByBarcode.ErrorMessage))
                        {
                            errors.Add(string.Format("Get barcode '{0}' error: {1}", ib.Barcode, srGetByBarcode.ErrorMessage));
                            continue;
                        }

                        if (srGetByBarcode.Data != null)
                        {
                            errors.Add(ApiError.Existed.GetDecription(string.Format("Line {0}: Barcode '{1}'", errorLine, ib.Barcode)));
                            continue;
                        }
                    }

                    if (srGet.Data != null)
                    {
                        if (!string.IsNullOrEmpty(ib.Barcode))
                            srGet.Data.barcode = ib.Barcode;

                        if (!string.IsNullOrEmpty(ib.ItemDescription))
                            srGet.Data.item_description = ib.ItemDescription;

                        if (!string.IsNullOrEmpty(ib.ItemGroup))
                            srGet.Data.item_group = ib.ItemGroup;

                        if (!string.IsNullOrEmpty(ib.Fit))
                            srGet.Data.fit = ib.Fit;

                        if (!string.IsNullOrEmpty(ib.Style))
                            srGet.Data.style = ib.Style;

                        if (!string.IsNullOrEmpty(ib.Season))
                            srGet.Data.season = ib.Season;

                        if (!string.IsNullOrEmpty(ib.Quality))
                            srGet.Data.quality = ib.Quality;

                        if (!string.IsNullOrEmpty(ib.Material))
                            srGet.Data.material = ib.Material;

                        if (!string.IsNullOrEmpty(ib.ColorDescription))
                            srGet.Data.color_description = ib.ColorDescription;

                        if (!string.IsNullOrEmpty(ib.TariffNumber))
                            srGet.Data.tariff_number = ib.TariffNumber;

                        srGet.Data.updated_by = identity.UserId;

                        var msgUpdate = await _itemService.Update(ib.ItemNumber, ib.ColorNumber, ib.Inseam, ib.Size, srGet.Data);
                        if (!string.IsNullOrEmpty(msgUpdate))
                        {
                            errors.Add(string.Format("Update item '{0}' - color '{1}' - inseam '{2}' - size '{3}' error: {4}", ib.ItemNumber, ib.ColorNumber, ib.Inseam, ib.Size, msgUpdate));
                            continue;
                        }

                        saveds.Add(srGet.Data);
                    }
                    else
                    {
                        var itemBarcode = new Item_Collection
                        {
                            barcode = ib.Barcode,
                            item_number = ib.ItemNumber,
                            item_description = ib.ItemDescription,
                            item_group = ib.ItemGroup,
                            fit = ib.Fit,
                            style = ib.Style,
                            season = ib.Season,
                            quality = ib.Quality,
                            material = ib.Material,
                            color_number = ib.ColorNumber,
                            color_description = ib.ColorDescription,
                            size = ib.Size,
                            inseam = ib.Inseam,
                            tariff_number = ib.TariffNumber,
                            created_by = identity.UserId,
                            updated_by = identity.UserId
                        };

                        var msgCreate = await _itemService.Create(itemBarcode);
                        if (!string.IsNullOrEmpty(msgCreate))
                        {
                            errors.Add(string.Format("Create item '{0}' - color '{1}' - inseam '{2}' - size '{3}' error: {4}", ib.ItemNumber, ib.ColorNumber, ib.Inseam, ib.Size, msgCreate));
                            continue;
                        }

                        saveds.Add(itemBarcode);
                    }
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
        public async Task<ActionResult> Create([FromBody] ItemBarcode_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                //if (string.IsNullOrEmpty(request.Barcode))
                //    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Barcode"));

                if (string.IsNullOrEmpty(request.ItemNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Item number"));

                if (string.IsNullOrEmpty(request.ItemDescription))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Item description"));

                if (string.IsNullOrEmpty(request.ColorNumber))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Color number"));

                if (string.IsNullOrEmpty(request.ColorDescription))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Color description"));

                if (string.IsNullOrEmpty(request.Size))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Size"));

                if (string.IsNullOrEmpty(request.Inseam))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Inseam"));

                var srGet = await _itemService.Get(request.ItemNumber, request.ColorNumber, request.Inseam, request.Size);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get item '{0}' - color '{1}' - inseam '{2}' - size '{3}' error: {4}", request.ItemNumber, request.ColorNumber, request.Inseam, request.Size, srGet.ErrorMessage));

                if (srGet.Data != null)
                    return new ApiResponse((int)ApiError.Existed, ApiError.Existed.GetDecription(string.Format("Item '{0}' - color '{1}' - inseam '{2}' - size '{3}'", request.ItemNumber, request.ColorNumber, request.Inseam, request.Size)));

                srGet = await _itemService.GetBarcode(request.Barcode);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get barcode '{0}' error: {1}", request.Barcode, srGet.ErrorMessage));

                if (srGet.Data != null)
                    return new ApiResponse((int)ApiError.Existed, ApiError.Existed.GetDecription(string.Format("Barcode '{0}'", request.Barcode)));

                var itemBarcode = new Item_Collection
                {
                    barcode = request.Barcode,
                    item_number = request.ItemNumber,
                    item_description = request.ItemDescription,
                    item_group = request.ItemGroup,
                    fit = request.Fit,
                    style = request.Style,
                    season = request.Season,
                    quality = request.Quality,
                    material = request.Material,
                    color_number = request.ColorNumber,
                    color_description = request.ColorDescription,
                    size = request.Size,
                    inseam = request.Inseam,
                    tariff_number = request.TariffNumber,
                    created_by = identity.UserId,
                    updated_by = identity.UserId
                };

                var msgCreate = await _itemService.Create(itemBarcode);
                if (!string.IsNullOrEmpty(msgCreate))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Create item '{0}' - color '{1}' - inseam '{2}' - size '{3}' error: {4}", request.ItemNumber, request.ColorNumber, request.Inseam, request.Size, msgCreate));

                return new ApiResponse(itemBarcode);
            });
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
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

                var stages = new BsonDocument[] { request.Filters.CreateFilter(), request.Sorts.CreateSort() };
                var srRead = await _itemService.Read<object>(stages, request.PageSkip, request.PageLimit);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiReadResponse((int)ApiError.DbError, string.Format("Get item(s) error: {0}", srRead.ErrorMessage));

                return new ApiReadResponse(srRead.Datas, srRead.Total);
            });
        }

        #region PDA actions

        [HttpPost]
        [Route("pda_get")]
        public async Task<ActionResult> PDA_Get([FromBody] ItemBarcode_GetByBarcode_Request request)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(request.Barcode))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Barcode"));

                var srGet = await _itemService.GetBarcode(request.Barcode);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get barcode '{0}' error: {1}", request.Barcode, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Barcode '{0}'", request.Barcode)));

                return new ApiResponse(srGet.Data);
            });
        }

        #endregion
    }
}
