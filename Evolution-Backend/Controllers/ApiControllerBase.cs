using Evolution_Backend.Models;

using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Evolution_Backend.Controllers
{
    public class ApiControllerBase : ControllerBase
    {
        public static Dictionary<int, string> ConvertEnumToDictionary<T>()
        {
            try
            {
                var dic = new Dictionary<int, string>();
                foreach (var name in Enum.GetNames(typeof(T)))
                {
                    dic.Add((int)Enum.Parse(typeof(T), name), name);
                }
                return dic;
            }
            catch (Exception)
            {
                return new Dictionary<int, string>();
            }
        }

        protected OkObjectResult Execute(Func<ApiResponse> responseFunc)
        {
            try
            {
                var response = responseFunc();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse((int)ApiError.SystemError, ex.Message));
            }
        }

        protected OkObjectResult Execute(Func<ApiIdentity, ApiResponse> responseFunc)
        {
            try
            {
                var identity = getIdentity();
                var response = responseFunc(identity);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse((int)ApiError.SystemError, ex.Message));
            }
        }

        protected OkObjectResult ExecuteRead(Func<ApiReadResponse> responseFunc)
        {
            try
            {
                var response = responseFunc();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new ApiReadResponse((int)ApiError.SystemError, ex.Message));
            }
        }

        protected OkObjectResult ExecuteRead(Func<ApiIdentity, ApiReadResponse> responseFunc)
        {
            try
            {
                var identity = getIdentity();
                var response = responseFunc(identity);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new ApiReadResponse((int)ApiError.SystemError, ex.Message));
            }
        }

        protected async Task<OkObjectResult> ExecuteAsync(Func<Task<ApiResponse>> responseFunc)
        {
            try
            {
                var response = await responseFunc();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse((int)ApiError.SystemError, ex.Message));
            }
        }

        protected async Task<OkObjectResult> ExecuteAsync(Func<ApiIdentity, Task<ApiResponse>> responseFunc)
        {
            try
            {
                var identity = getIdentity();
                var response = await responseFunc(identity);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse((int)ApiError.SystemError, ex.Message));
            }
        }

        protected async Task<OkObjectResult> ExecuteReadAsync(Func<Task<ApiReadResponse>> responseFunc)
        {
            try
            {
                var response = await responseFunc();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new ApiReadResponse((int)ApiError.SystemError, ex.Message));
            }
        }

        protected async Task<OkObjectResult> ExecuteReadAsync(Func<ApiIdentity, Task<ApiReadResponse>> responseFunc)
        {
            try
            {
                var identity = getIdentity();
                var response = await responseFunc(identity);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new ApiReadResponse((int)ApiError.SystemError, ex.Message));
            }
        }

        ApiIdentity getIdentity()
        {
            return new ApiIdentity
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                UserName = User.FindFirstValue(ClaimTypes.Name),
                UserRole = User.FindFirstValue(ClaimTypes.Role)
            };
        }
    }
}
