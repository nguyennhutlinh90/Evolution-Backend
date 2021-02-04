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
    [Route("api/stockcheck")]
    public class StockCheckController : ApiControllerBase
    {
        private readonly IPOService _POService;
        public StockCheckController(IPOService POService)
        {
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
                    return new ApiReadResponse((int)ApiError.DbError, string.Format("Get item(s) stock error: {0}", srRead.ErrorMessage));

                return new ApiReadResponse(srRead.Datas, srRead.Total);
            });
        }
    }
}
