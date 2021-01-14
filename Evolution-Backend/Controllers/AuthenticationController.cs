using Evolution_Backend.Models;
using Evolution_Backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Threading.Tasks;

namespace Evolution_Backend.Controllers
{
    [ApiController]
    [Route("api")]
    public class AuthenticationController : ApiControllerBase
    {
        private readonly IUserService _userService;
        public AuthenticationController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        [Route("authentication")]
        public async Task<ActionResult> Authentication([FromBody] Authentication_Request request)
        {
            return await ExecuteAsync(async () =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.UserName))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("User name"));

                if (string.IsNullOrEmpty(request.Password))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Password"));

                var srAuthentication = await _userService.Authentication(request.UserName, request.Password.HashPassword());
                if (!string.IsNullOrEmpty(srAuthentication.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Authentication user '{0}' error: {1}", request.UserName, srAuthentication.ErrorMessage));

                if (srAuthentication.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, "Username or password is incorrect");

                return new ApiResponse(new
                {
                    srAuthentication.Data.id,
                    srAuthentication.Data.user_name,
                    srAuthentication.Data.first_name,
                    srAuthentication.Data.last_name,
                    srAuthentication.Data.role,
                    srAuthentication.Data.token,
                    srAuthentication.Data.expiration
                });
            });
        }

        #region PDA actions

        [HttpPost]
        [Route("pda_signin")]
        public async Task<ActionResult> PDA_SignIn([FromBody] PDA_SignIn_Request request)
        {
            return await ExecuteAsync(async () =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.UserName))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("User name"));

                if (string.IsNullOrEmpty(request.Password))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Password"));

                if (string.IsNullOrEmpty(request.DeviceId))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Device Id"));

                var srAuthentication = await _userService.Authentication(request.UserName, request.Password.HashPassword());
                if (!string.IsNullOrEmpty(srAuthentication.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Authentication user '{0}' error: {1}", request.UserName, srAuthentication.ErrorMessage));

                if (srAuthentication.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, "Username or password is incorrect");

                if (!string.IsNullOrEmpty(srAuthentication.Data.device_id) && srAuthentication.Data.device_id != request.DeviceId)
                    return new ApiResponse((int)ApiError.LoginByOtherDevice, string.Format("User '{0}' is logged in on another device", request.UserName));

                if (string.IsNullOrEmpty(srAuthentication.Data.device_id))
                {
                    var msgUpdateDevice = await _userService.UpdateDeviceId(request.UserName, request.DeviceId);
                    if (!string.IsNullOrEmpty(msgUpdateDevice))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Signin user '{0}' device error: {1}", request.UserName, msgUpdateDevice));
                }

                return new ApiResponse(new
                {
                    srAuthentication.Data.id,
                    srAuthentication.Data.user_name,
                    srAuthentication.Data.first_name,
                    srAuthentication.Data.last_name,
                    srAuthentication.Data.role,
                    srAuthentication.Data.token,
                    srAuthentication.Data.expiration
                });
            });
        }

        [Authorize]
        [HttpPost]
        [Route("pda_signout")]
        public async Task<ActionResult> PDA_SignOut([FromBody] PDA_SignOut_Request request)
        {
            return await ExecuteAsync(async () =>
            {
                if (request == null)
                    return new ApiResponse((int)ApiError.Missing, ApiError.Missing.GetDecription("request param"));

                if (string.IsNullOrEmpty(request.UserName))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("User name"));

                if (string.IsNullOrEmpty(request.DeviceId))
                    return new ApiResponse((int)ApiError.Required, ApiError.Required.GetDecription("Device Id"));

                var srGet = await _userService.Get(request.UserName);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get user '{0}' error: {1}", request.UserName, srGet.ErrorMessage));

                if (srGet.Data == null)
                    return new ApiResponse((int)ApiError.NotFound, ApiError.NotFound.GetDecription(string.Format("User '{0}'", request.UserName)));

                if (!string.IsNullOrEmpty(srGet.Data.device_id))
                {
                    if (srGet.Data.device_id != request.DeviceId)
                        return new ApiResponse((int)ApiError.LoginByOtherDevice, string.Format("User '{0}' is logged in on another device", request.UserName));

                    var msgUpdateDevice = await _userService.UpdateDeviceId(request.UserName, "");
                    if (!string.IsNullOrEmpty(msgUpdateDevice))
                        return new ApiResponse((int)ApiError.DbError, string.Format("Signout user '{0}' device error: {1}", request.UserName, msgUpdateDevice));
                }

                return new ApiResponse(null);
            });
        }

        #endregion
    }
}
