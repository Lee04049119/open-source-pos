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

namespace ChowChoice.Api.Controllers.BSS_ERP
{
    [EnableCors("corsGlobalPolicy")]
    [Authorize]
    
    [Produces("application/json")]
    [Route("api/POS")]
    public class POSController : Controller
    {
        private readonly IPOSService _POSService;
        private readonly IUserService _userService;

        public POSController(IPOSService POSService, IUserService userService)
        {
            _POSService = POSService;
            _userService = userService;
        }
        

        //[Route("add")]
        //[HttpPost]
        //public async Task<IActionResult> AddData([FromBody]Models.EmployeeTimeLog employeeTimeLogAdd)
        //{
        //    ServiceResponse response = await _POSService.AddDataAsync(employeeTimeLogAdd);
        //    return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
        //}

        //[Route("update")]
        //[HttpPost]
        //public async Task<IActionResult> UpdData([FromBody]Models.EmployeeTimeLog employeeTimeLogUpdate)
        //{
        //    ServiceResponse response = await _POSService.UpdDataAsync(employeeTimeLogUpdate);
        //    return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
        //}

        

        

        

        ///////// new working ///////

        /// <summary>
        /// used to get items in search.
        /// </summary>
        /// <param name="jEmployeeTimeLog"></param>
        /// <returns></returns>
        [Route("GetSearchItems")]
        [HttpPost]
        public async Task<IActionResult> GetSearchItems([FromBody] PosSearchRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Request body is required." });

                var query = request.GetSearchText();
                var companyId = request.companyId;

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

                ServiceResponse response = await _POSService.GetSearchItemsAsync(query, companyId);
                return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// insert pos transaction record.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]POS pos)
        {
            var userIdClaim = HttpContext.User.Claims.Where(c => c.Type == ClaimTypes.Name).First();
            var userID = int.Parse(userIdClaim.Value);

            if (pos.objInvoiceMaster != null)
            {
                pos.objInvoiceMaster.CreateUser = userID;
            }
            if (pos.objInvoiceDetailItems != null)
            {
                pos.objInvoiceDetailItems.CreateUser = userID;
            }
            if (pos.listInvoiceDetailItems != null && pos.listInvoiceDetailItems.Count > 0)
            {
                for (int i = 0; i < pos.listInvoiceDetailItems.Count; i++)
                {
                    pos.listInvoiceDetailItems[i].CreateUser = userID;
                }                
            }


            ServiceResponse response = await _POSService.AddDataAsync(pos);
            return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
        }
        /// <summary>
        /// Update pos transaction records
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<IActionResult> Put([FromBody]POS pos)
        {
            ServiceResponse response = await _POSService.UpdDataAsync(pos);
            return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
        }

        /// <summary>
        /// Delete whole invoice including all details if it is not posted.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] InvoiceMaster pos)
        {
            try
            {
                ServiceResponse response = await _POSService.DeleteInvoice(pos);
                return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
            
        }
        /// <summary>
        /// Deletes a list of whole invoices including all details if it is not posted.
        /// </summary>
        /// <param name="invoicesMst">list of invoices to delete</param>
        /// <returns></returns>
        [Route("DeleteList")]
        [HttpDelete]
        public async Task<IActionResult> DeleteList([FromBody] InvoiceMaster[] invoicesMst)
        {
            try
            {
                ServiceResponse response = await _POSService.DeleteInvoicesList(invoicesMst);
                return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }

        }
        /// <summary>
        /// used to get invoices master for invoices list view.
        /// </summary>
        /// <param name="jSearchItems"></param>
        /// <returns></returns>
        [Route("getinvoices")]
        [HttpPost]
        public async Task<IActionResult> GetInvoices([FromBody] ItemSearchRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Request body is required." });

                return await GetInvoicesResult(request.query, request.companyId, request.limit, request.offset);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }

        }

        /// <summary>
        /// GET version of invoice listing for direct/browser/API-client calls.
        /// </summary>
        [Route("getinvoices")]
        [HttpGet]
        public async Task<IActionResult> GetInvoices([FromQuery] string query = "", [FromQuery] int companyId = 0, [FromQuery] int limit = 0, [FromQuery] int offset = 0)
        {
            try
            {
                return await GetInvoicesResult(query, companyId, limit, offset);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private async Task<IActionResult> GetInvoicesResult(string query, int companyId, int limit, int offset)
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

            ServiceResponse response = await _POSService.GetInvoicesAsync(query ?? "", companyId, limit, offset);
            return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
        }

        /// <summary>
        /// used to get details (items) of an invoice 
        /// </summary>
        /// <param name="invDetail"></param>
        /// <returns></returns>
        [Route("getinvoicedetails")]
        [HttpPost]
        public async Task<IActionResult> GetInvoiceDetails([FromBody] InvoiceMasterListing invDetail)
        {
            try
            {
                return await GetInvoiceDetailsResult(invDetail.InvoiceNo.ToString(), invDetail.InvoiceType, (int)invDetail.FiscalYearID, invDetail.CompanyID);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }

        }

        /// <summary>
        /// GET version of invoice details for direct/browser/API-client calls.
        /// </summary>
        [Route("getinvoicedetails")]
        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetails([FromQuery] string invoiceNo, [FromQuery] string invoiceType, [FromQuery] int fiscalYearId, [FromQuery] int companyId)
        {
            try
            {
                return await GetInvoiceDetailsResult(invoiceNo, invoiceType, fiscalYearId, companyId);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private async Task<IActionResult> GetInvoiceDetailsResult(string invoiceNo, string invoiceType, int fiscalYearId, int companyId)
        {
            ServiceResponse response = await _POSService.GetInvoiceDetailsAsync(invoiceNo, invoiceType, fiscalYearId, companyId);
            return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
        }

        /// <summary>
        /// Deletes a single, particular item row from the details of invoice.
        /// </summary>
        /// <param name="invDetail"></param>
        /// <returns></returns>
        [Route("DeleteInvDetail")]
        [HttpPost]
        public async Task<IActionResult> DeleteInvDetail([FromBody] InvoiceDetailItems invDetail)
        {
            try
            {


                ServiceResponse response = await _POSService.DeleteInvDetail(invDetail);

                return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }

        }
    }
}
