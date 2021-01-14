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
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = Constants.Role.Admin)]
    [ApiController]
    [Route("api/customer")]
    public class CustomerController : ApiControllerBase
    {
        private readonly ICustomerService _customerService;
        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpPost]
        [Route("upload")]
        public async Task<ActionResult> Upload([FromBody] List<Customer_Request> request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null || !request.Any())
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                var errors = new List<string>();
                var saveds = new List<Customer_Collection>();
                foreach (var cust in request)
                {
                    if (string.IsNullOrEmpty(cust.CustomerCode))
                    {
                        errors.Add(ApiError.Required.GetDecription("Customer code"));
                        continue;
                    }

                    if (string.IsNullOrEmpty(cust.CustomerName))
                    {
                        errors.Add(ApiError.Required.GetDecription("Customer code"));
                        continue;
                    }

                    if (request.Count(cc => cc.CustomerCode == cust.CustomerCode) > 1)
                        return new ApiResponse((int)ApiError.Dupplicated, ApiError.Dupplicated.GetDecription(string.Format("Customer '{0}'", cust.CustomerCode)));

                    int? status = null;
                    if (!string.IsNullOrEmpty(cust.StatusCode))
                    {
                        status = cust.StatusCode.GetValue<StatusEnums.General>();
                        if (!status.HasValue)
                        {
                            errors.Add(ApiError.NotFound.GetDecription(string.Format("Status '{0}'", cust.StatusCode)));
                            continue;
                        }
                    }

                    var srGet = await _customerService.Get(cust.CustomerCode);
                    if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    {
                        errors.Add(string.Format("Get customer '{0}' error: {1}", cust.CustomerCode, srGet.ErrorMessage));
                        continue;
                    }

                    if (srGet.Data != null)
                    {
                        if (!string.IsNullOrEmpty(cust.CustomerName))
                            srGet.Data.customer_name = cust.CustomerName;

                        if (status.HasValue)
                            srGet.Data.status = status.Value;

                        srGet.Data.updated_by = identity.UserId;

                        var msgUpdate = await _customerService.Update(cust.CustomerCode, srGet.Data);
                        if (!string.IsNullOrEmpty(msgUpdate))
                        {
                            errors.Add(string.Format("Update customer '{0}' error: {1}", cust.CustomerCode, msgUpdate));
                            continue;
                        }

                        saveds.Add(srGet.Data);
                    }
                    else
                    {
                        var customer = new Customer_Collection
                        {
                            customer_code = cust.CustomerCode,
                            customer_name = cust.CustomerName,
                            status = status.HasValue ? status.Value : (int)StatusEnums.General.Active,
                            created_by = identity.UserId,
                            updated_by = identity.UserId
                        };

                        var msgCreate = await _customerService.Create(customer);
                        if (!string.IsNullOrEmpty(msgCreate))
                        {
                            errors.Add(string.Format("Create customer '{0}' error: {1}", cust.CustomerCode, msgCreate));
                            continue;
                        }

                        saveds.Add(customer);
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

        [HttpPost]
        [Route("create")]
        public async Task<ActionResult> Create([FromBody] Customer_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.CustomerCode))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Customer code"));

                if (string.IsNullOrEmpty(request.CustomerName))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Customer name"));

                int? status = null;
                if (!string.IsNullOrEmpty(request.StatusCode))
                {
                    status = request.StatusCode.GetValue<StatusEnums.General>();
                    if (!status.HasValue)
                        return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Status '{0}'", request.StatusCode)));
                }

                var srGet = await _customerService.Get(request.CustomerCode);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get customer '{0}' error: {1}", request.CustomerCode, srGet.ErrorMessage));

                if (srGet.Data != null)
                    return new ApiResponse((int)ApiError.Existed, ApiError.Existed.GetDecription(string.Format("Customer '{0}'", request.CustomerCode)));

                var customer = new Customer_Collection
                {
                    customer_code = request.CustomerCode,
                    customer_name = request.CustomerName,
                    status = status.HasValue ? status.Value : (int)StatusEnums.General.Active,
                    created_by = identity.UserId,
                    updated_by = identity.UserId
                };

                var msgCreate = await _customerService.Create(customer);
                if (!string.IsNullOrEmpty(msgCreate))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Create customer '{0}' error: {1}", request.CustomerCode, msgCreate));

                return new ApiResponse(customer);
            });
        }

        [HttpDelete]
        [Route("delete")]
        public async Task<ActionResult> Delete(string customerCode)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(customerCode))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Customer code"));

                var srGet = await _customerService.Get(customerCode);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get customer '{0}' error: {1}", customerCode, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Customer '{0}'", customerCode)));

                var msgDelete = await _customerService.Delete(customerCode);
                if (!string.IsNullOrEmpty(msgDelete))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Delete customer '{0}' error: {1}", customerCode, msgDelete));

                return new ApiResponse { is_success = true };
            });
        }

        [HttpPost]
        [Route("update")]
        public async Task<ActionResult> Update([FromBody] Customer_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.CustomerCode))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Customer code"));

                int? status = null;
                if (!string.IsNullOrEmpty(request.StatusCode))
                {
                    status = request.StatusCode.GetValue<StatusEnums.General>();
                    if (!status.HasValue)
                        return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Status '{0}'", request.StatusCode)));
                }

                var srGet = await _customerService.Get(request.CustomerCode);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get customer '{0}' error: {1}", request.CustomerCode, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Customer '{0}'", request.CustomerCode)));

                if (!string.IsNullOrEmpty(request.CustomerName))
                    srGet.Data.customer_name = request.CustomerName;

                if (status.HasValue)
                    srGet.Data.status = status.Value;

                srGet.Data.updated_by = identity.UserId;

                var msgUpdate = await _customerService.Update(request.CustomerCode, srGet.Data);
                if (!string.IsNullOrEmpty(msgUpdate))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Update customer '{0}' error: {1}", request.CustomerCode, msgUpdate));

                return new ApiResponse(srGet.Data);
            });
        }

        [HttpPost]
        [Route("changestatus")]
        public async Task<ActionResult> ChangeStatus([FromBody] Customer_ChangeStatus_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.CustomerCode))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Customer code"));

                if (string.IsNullOrEmpty(request.StatusCode))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Status"));

                var status = request.StatusCode.GetValue<StatusEnums.General>();
                if (!status.HasValue)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Status '{0}'", request.StatusCode)));

                var srGet = await _customerService.Get(request.CustomerCode);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get customer '{0}' error: {1}", request.CustomerCode, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Customer '{0}'", request.CustomerCode)));

                srGet.Data.status = status.Value;
                srGet.Data.updated_by = identity.UserId;

                var msgChangeStatus = await _customerService.Update(request.CustomerCode, srGet.Data);
                if (!string.IsNullOrEmpty(msgChangeStatus))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Update customer '{0}' status error: {1}", request.CustomerCode, msgChangeStatus));

                return new ApiResponse(srGet.Data);
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

                var stages = new BsonDocument[] { request.Filters.CreateFilter(), request.Sorts.CreateSort() };
                var srRead = await _customerService.Read<object>(stages, request.PageSkip, request.PageLimit);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiReadResponse((int)ApiError.DbError, string.Format("Get customer(s) error: {0}", srRead.ErrorMessage));

                return new ApiReadResponse(srRead.Datas, srRead.Total);
            });
        }

        [HttpGet]
        [Route("get")]
        public async Task<ActionResult> Get(string customerCode)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(customerCode))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Customer code"));

                var srGet = await _customerService.Get(customerCode);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get customer '{0}' error: {1}", customerCode, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Customer '{0}'", customerCode)));

                return new ApiResponse(srGet.Data);
            });
        }

        [HttpGet]
        [Route("getallstatus")]
        public ActionResult GetAllStatus()
        {
            return Execute(() =>
            {
                var status = ConvertEnumToDictionary<StatusEnums.General>();
                return new ApiResponse(status);
            });
        }
    }
}
