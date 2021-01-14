using Evolution_Backend.DbModels;
using Evolution_Backend.Models;
using Evolution_Backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MongoDB.Bson;

using System.Linq;
using System.Threading.Tasks;

namespace Evolution_Backend.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = Constants.Role.Admin)]
    [ApiController]
    [Route("api/user")]
    public class UserController : ApiControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        [Route("create")]
        public async Task<ActionResult> Create([FromBody] User_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.UserName))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("User name"));

                if (string.IsNullOrEmpty(request.Password))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Password"));

                if (string.IsNullOrEmpty(request.Role))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Role"));

                if (!checkRole(request.Role))
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Role '{0}'", request.Role)));

                int? status = null;
                if (!string.IsNullOrEmpty(request.StatusCode))
                {
                    status = request.StatusCode.GetValue<StatusEnums.General>();
                    if (!status.HasValue)
                        return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Status '{0}'", request.StatusCode)));
                }

                var srGet = await _userService.Get(request.UserName);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get user '{0}' error: {1}", request.UserName, srGet.ErrorMessage));

                if (srGet.Data != null)
                    return new ApiResponse((int)ApiError.Existed, ApiError.Existed.GetDecription(string.Format("User '{0}'", request.UserName)));

                var user = new User_Collection
                {
                    user_name = request.UserName,
                    password = request.Password.HashPassword(),
                    first_name = request.FirstName,
                    last_name = request.LastName,
                    role = request.Role,
                    status = status.HasValue ? status.Value : (int)StatusEnums.General.Active,
                    created_by = identity.UserId,
                    updated_by = identity.UserId
                };

                var msgCreate = await _userService.Create(user);
                if (!string.IsNullOrEmpty(msgCreate))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Create user '{0}' error: {1}", request.UserName, msgCreate));

                return new ApiResponse(user);
            });
        }

        [HttpDelete]
        [Route("delete")]
        public async Task<ActionResult> Delete(string userName)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(userName))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("User name"));

                var srGet = await _userService.Get(userName);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get user '{0}' error: {1}", userName, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("User '{0}'", userName)));

                var msgDelete = await _userService.Delete(userName);
                if (!string.IsNullOrEmpty(msgDelete))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Delete user '{0}' error: {1}", userName, msgDelete));

                return new ApiResponse { is_success = true };
            });
        }

        [HttpPost]
        [Route("update")]
        public async Task<ActionResult> Update([FromBody] User_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.UserName))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("User name"));

                if (!string.IsNullOrEmpty(request.Role))
                {
                    if (!checkRole(request.Role))
                        return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Role '{0}'", request.Role)));
                }

                int? status = null;
                if (!string.IsNullOrEmpty(request.StatusCode))
                {
                    status = request.StatusCode.GetValue<StatusEnums.General>();
                    if (!status.HasValue)
                        return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Status '{0}'", request.StatusCode)));
                }

                var srGet = await _userService.Get(request.UserName);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get user '{0}' error: {1}", request.UserName, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("User '{0}'", request.UserName)));

                if (!string.IsNullOrEmpty(request.Password))
                    srGet.Data.password = request.Password.HashPassword();

                if (!string.IsNullOrEmpty(request.Role))
                    srGet.Data.role = request.Role;

                if (status.HasValue)
                    srGet.Data.status = status.Value;

                srGet.Data.first_name = request.FirstName;
                srGet.Data.last_name = request.LastName;
                srGet.Data.updated_by = identity.UserId;

                var msgUpdate = await _userService.Update(request.UserName, srGet.Data);
                if (!string.IsNullOrEmpty(msgUpdate))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Update user '{0}' error: {1}", request.UserName, msgUpdate));

                return new ApiResponse(srGet.Data);
            });
        }

        [HttpPost]
        [Route("changepassword")]
        public async Task<ActionResult> ChangePassword([FromBody] User_ChangePassword_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.UserName))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("User name"));

                if (string.IsNullOrEmpty(request.Password))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Password"));

                var srGet = await _userService.Get(request.UserName);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get user '{0}' error: {1}", request.UserName, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("User '{0}'", request.UserName)));

                srGet.Data.password = request.Password.HashPassword();
                srGet.Data.updated_by = identity.UserId;

                var msgChangePassword = await _userService.Update(request.UserName, srGet.Data);
                if (!string.IsNullOrEmpty(msgChangePassword))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Update user '{0}' password error: {1}", request.UserName, msgChangePassword));

                return new ApiResponse(srGet.Data);
            });
        }

        [HttpPost]
        [Route("changerole")]
        public async Task<ActionResult> ChangeRole([FromBody] User_ChangeRole_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.UserName))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("User name"));

                if (string.IsNullOrEmpty(request.Role))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Role"));

                if (!checkRole(request.Role))
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Role '{0}'", request.Role)));

                var srGet = await _userService.Get(request.UserName);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get user '{0}' error: {1}", request.UserName, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("User '{0}'", request.UserName)));

                srGet.Data.role = request.Role;
                srGet.Data.updated_by = identity.UserId;

                var msgChangeRole = await _userService.Update(request.UserName, srGet.Data);
                if (!string.IsNullOrEmpty(msgChangeRole))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Update user '{0}' role error: {1}", request.UserName, msgChangeRole));

                return new ApiResponse(srGet.Data);
            });
        }

        [HttpPost]
        [Route("changestatus")]
        public async Task<ActionResult> ChangeStatus([FromBody] User_ChangeStatus_Request request)
        {
            return await ExecuteAsync(async (identity) =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.UserName))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("User name"));

                if (string.IsNullOrEmpty(request.StatusCode))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Status"));

                var status = request.StatusCode.GetValue<StatusEnums.General>();
                if (!status.HasValue)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("Status '{0}'", request.StatusCode)));

                var srGet = await _userService.Get(request.UserName);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get user '{0}' error: {1}", request.UserName, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("User '{0}'", request.UserName)));

                srGet.Data.status = status.Value;
                srGet.Data.updated_by = identity.UserId;

                var msgChangeRole = await _userService.Update(request.UserName, srGet.Data);
                if (!string.IsNullOrEmpty(msgChangeRole))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Update user '{0}' role error: {1}", request.UserName, msgChangeRole));

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
                var srRead = await _userService.Read<object>(stages, request.PageSkip, request.PageLimit);
                if (!string.IsNullOrEmpty(srRead.ErrorMessage))
                    return new ApiReadResponse((int)ApiError.DbError, string.Format("Get user(s) error: {0}", srRead.ErrorMessage));

                return new ApiReadResponse(srRead.Datas, srRead.Total);
            });
        }

        [HttpGet]
        [Route("get")]
        public async Task<ActionResult> Get(string userName)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(userName))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("User name"));

                var srGet = await _userService.Get(userName);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get user '{0}' error: {1}", userName, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("User '{0}'", userName)));

                return new ApiResponse(srGet.Data);
            });
        }

        [HttpGet]
        [Route("getallrole")]
        public ActionResult GetAllRole()
        {
            return Execute(() =>
            {
                return new ApiResponse(new string[] { Constants.Role.Admin, Constants.Role.Manager, Constants.Role.User });
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

        #region Private methods

        bool checkRole(string role)
        {
            return (new string[] { Constants.Role.Admin, Constants.Role.Manager, Constants.Role.User }).Contains(role);
        }

        #endregion
    }
}
