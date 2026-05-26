using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Models;
using Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers
{
    [EnableCors("corsGlobalPolicy")]
    [Authorize]
    
    [Produces("application/json")]
    [Route("api/item")]
    public class ItemController : Controller
    {
        private readonly IItemService _ItemService;
        private readonly IUserService _userService;

        public ItemController(IItemService ItemService, IUserService userService)
        {
            _ItemService = ItemService;
            _userService = userService;
        }

        /// <summary>
        /// Returns items for the company. companyId in the body is optional — falls back to the logged-in user's CompanyID.
        /// </summary>
        [Route("getitems")]
        [HttpPost]
        public async Task<IActionResult> GetItems([FromBody] ItemSearchRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Request body is required." });

                return await GetItemsResult(request.query, request.companyId, request.limit, request.offset);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
            
        }

        /// <summary>
        /// GET version of item listing for direct/browser/API-client calls.
        /// </summary>
        [Route("getitems")]
        [HttpGet]
        public async Task<IActionResult> GetItems([FromQuery] string query = "", [FromQuery] int companyId = 0, [FromQuery] int limit = 0, [FromQuery] int offset = 0)
        {
            try
            {
                return await GetItemsResult(query, companyId, limit, offset);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private async Task<IActionResult> GetItemsResult(string query, int companyId, int limit, int offset)
        {
            // When CompanyID is missing from the client (undefined in JS), use the JWT user's company.
            if (companyId <= 0)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.Name);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                    return Unauthorized(new { message = "Invalid token." });

                var user = _userService.GetById(userId);
                if (user == null)
                    return Unauthorized(new { message = "User not found." });

                companyId = user.CompanyID;
            }

            ServiceResponse response = await _ItemService.GetItemsAsync(query ?? "", companyId, limit, offset);
            return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
        }
        /// <summary>
        /// insert pos Product / Item record.
        /// </summary>
        /// <param name="posItem"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]PosItem posItem)
        {
            try
            {
                var userIdClaim = HttpContext.User.Claims.Where(c => c.Type == ClaimTypes.Name).First();
                var userID = int.Parse(userIdClaim.Value);

                if (posItem != null)
                {
                    posItem.CreateUser = userID;
                }
                ServiceResponse response = await _ItemService.AddDataAsync(posItem);
                return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
            }
            catch (Exception ex)
            {
                return StatusCode((int) HttpStatusCode.InternalServerError, ex.Message);
            }
            
        }
        /// <summary>
        /// Update pos Product / Item record.
        /// </summary>
        /// <param name="posItem"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] PosItem posItem)
        {
            try
            {
                if (posItem == null)
                    return BadRequest(new ServiceResponse { IsValid = false, Message = "Request body is required." });

                var userIdClaim = User.FindFirst(ClaimTypes.Name);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userID))
                    return Unauthorized(new { message = "Invalid token." });

                posItem.UpdateUser = userID;
                if (posItem.CompanyID <= 0)
                {
                    var user = _userService.GetById(userID);
                    if (user != null)
                        posItem.CompanyID = user.CompanyID;
                }

                ServiceResponse response = await _ItemService.UpdDataAsync(posItem);
                return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Delete a product. Fails if the item appears on any invoice line.
        /// </summary>
        [HttpDelete("{itemId}")]
        public async Task<IActionResult> Delete(string itemId, [FromQuery] int companyId = 0)
        {
            try
            {
                if (companyId <= 0)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.Name);
                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                        return Unauthorized(new { message = "Invalid token." });
                    var user = _userService.GetById(userId);
                    if (user == null)
                        return Unauthorized(new { message = "User not found." });
                    companyId = user.CompanyID;
                }

                var response = await _ItemService.DeleteDataAsync(itemId, companyId);
                return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
