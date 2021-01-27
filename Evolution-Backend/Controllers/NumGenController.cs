using Evolution_Backend.DbModels;
using Evolution_Backend.Models;
using Evolution_Backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Threading.Tasks;

namespace Evolution_Backend.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = Constants.Role.Admin)]
    [ApiController]
    [Route("api/numgen")]
    public class NumGenController : ApiControllerBase
    {
        private readonly INumGenService _numGenService;
        public NumGenController(INumGenService numGenService)
        {
            _numGenService = numGenService;
        }

        [HttpGet]
        [Route("getboxnumber")]
        public async Task<ActionResult> GetBoxNumber()
        {
            return await ExecuteAsync(async () =>
            {
                var srGet = await _numGenService.Get(Constants.NumGenType.Box);
                if (!string.IsNullOrEmpty(srGet.ErrorMessage))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Get box number generator error: {0}", srGet.ErrorMessage));

                if (srGet.Data != null)
                    return new ApiResponse(srGet.Data);

                var numGen = new Num_Gen_Collection
                {
                    gen_type = Constants.NumGenType.Box,
                    gen_prefix = "",
                    gen_length = 7,
                    gen_number = 1
                };

                var msgCreate = await _numGenService.Create(numGen);
                if (!string.IsNullOrEmpty(msgCreate))
                    return new ApiResponse((int)ApiError.DbError, string.Format("Create box number generator error: {0}", msgCreate));

                return new ApiResponse(numGen);
            });
        }
    }
}
