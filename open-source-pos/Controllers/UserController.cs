using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;

using Models;
using Services;
using System.Net;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;

namespace open_source_pos.Controllers
{
    [EnableCors("AllowAll")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEmailSender _emailSender;
        private readonly AppSettings _appSettings;

        public UserController(IUserService userService, IEmailSender emailSender, IOptions<AppSettings> appSettings)
        {
            _userService = userService;
            _emailSender = emailSender;
            _appSettings = appSettings.Value;
        }

        private AppSettings GetAppSettings() => _appSettings;
        /// <summary>
        /// Login. Returns JWT in the <c>Token</c> field — use that value in Swagger Authorize (paste token only).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody]UserCred userParam)
        {
            try
            {
                if (userParam == null || string.IsNullOrWhiteSpace(userParam.UserEmail))
                    return BadRequest(new { message = "Email and password are required." });

                var user = await _userService.Authenticate(userParam.UserEmail, userParam.UserPassword, userParam);

                if (user == null)
                    return BadRequest(new { message = "Username or password is incorrect" });

                if (user.RequiresSessionConfirmation)
                {
                    return StatusCode(409, new
                    {
                        message = "You are already signed in on another device. Confirm to sign out the other session and continue here.",
                        requiresSessionConfirmation = true,
                        existingActiveSession = user.ExistingActiveSession
                    });
                }

                if (user.authenticationResult == null)
                    return StatusCode(500, new { message = "Authentication service returned no result." });

                if (user.authenticationResult.IsLockoutEnabled.GetValueOrDefault(false))
                {
                    return BadRequest(new
                    {
                        message = "Your account is locked for some time because of too many unsuccessful attempts",
                        data = new { IsLockoutEnabled = user.LockoutEnabled, user.LockoutEnd }
                    });
                }

                if (!user.authenticationResult.IsAuthorisedCurrently.GetValueOrDefault(true))
                    return BadRequest(new { message = "Username or password is incorrect" });

                if (string.IsNullOrWhiteSpace(user.Token))
                    return BadRequest(new { message = "Username or password is incorrect" });

                        // ✅ Ensure refresh token exists
                 if (string.IsNullOrEmpty(user.SessionToken))
                     user.SessionToken = Guid.NewGuid().ToString();

                       // ✅ Remember Me
                 if (user.RememberUser == true)
                {
                    AuthCookieHelper.SetRememberMeCookies(
                       Response,
                       user.Token,
                       user.SessionToken,
                       GetAppSettings()
                        );
              }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }

        /// <summary>
        /// Swagger helper: call after Authorize. Returns 200 when the JWT is valid.
        /// </summary>
        /// <summary>Remember Me only: issues a new access token cookie from the refresh token cookie.</summary>
        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refresh = Request.Cookies[AuthConstants.RefreshTokenCookieName];
            if (string.IsNullOrWhiteSpace(refresh))
                return Unauthorized(new { message = "Refresh token missing." });

            var result = await _userService.RefreshRememberedAccessTokenAsync(refresh);
            if (!result.IsValid)
                return Unauthorized(result);

            AuthCookieHelper.SetRememberMeCookies(Response, result.Data?.ToString(), refresh, GetAppSettings());
            return Ok(new { message = result.Message, refreshed = true });
        }

        [Authorize]
        [HttpGet("verify-token")]
        public IActionResult VerifyToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.Name);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "Invalid token: missing user id claim." });

            return Ok(new
            {
                valid = true,
                userId,
                message = "JWT is valid. You can call other protected APIs."
            });
        }

        [Authorize]
        [HttpPost("IsUserLogedInAndRemembered")]
        public async Task<IActionResult> IsUserLogedInAndRemembered([FromBody]UserCred userParam)
        {
            try
            {
                var userIdClaim = HttpContext.User.Claims.Where(c => c.Type == ClaimTypes.Name).First();
                var userID = int.Parse(userIdClaim.Value);
                var serviceResponse = await _userService.IsUserLogedInAndRemembered(userParam, userID);

                if (serviceResponse == null)
                    return BadRequest(new { message = "An error occoured!" });
                return StatusCode((int)(serviceResponse.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), serviceResponse);



            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }

        }

        [Authorize]
        [HttpPost("LogOut")]
        public async Task<IActionResult> LogOut([FromBody]UserCred userParam)
        {
            try
            {
                var userIdClaim = HttpContext.User.Claims.Where(c => c.Type == ClaimTypes.Name).First();
                var userID = int.Parse(userIdClaim.Value);
                var serviceResponse = await _userService.LogOut(userParam, userID);
                AuthCookieHelper.ClearRememberMeCookies(Response);

                if (serviceResponse == null)
                    return BadRequest(new { message = "An error occoured!" });
                return StatusCode((int)(serviceResponse.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), serviceResponse);



            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }

        }

        /// <summary>
        /// Returns the logged-in user profile. User id comes from JWT claim ClaimTypes.Name (set at login).
        /// </summary>
        [Authorize]
        [HttpPost("GetCurrentUser")]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.Name);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                    return Unauthorized(new { message = "Invalid token." });

                var user = _userService.GetById(userId);
                if (user == null)
                    return BadRequest(new { message = "User not found." });

                user.PasswordHash = null;
                user.PasswordSalt = null;

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserCred userCred)
        {
            // map dto to entity
            //var user = _mapper.Map<User>(userDto);
            //userCred.UserPassword = "2";
            try
            {
                var a = Request.Host;
                // The first user will be an Admin user with Users.IsAdmin flag set to true. Further users will not be Admin (Users.IsAdmin flag will be false)
                if (!userCred.IsAdmin.HasValue)
                {
                    userCred.IsAdmin = true;
                }
                // save 
                var x = await _userService.Create(userCred);
                if (x != null)
                {
                    string loginURL = GetLoginUrl(a);

                    string subject = "Email Conformation for open-source-pos";
                    userCred.LinkOrCode = @"<p> Use the following Password To Login to Your open-source-pos Account <br/>";
                    userCred.LinkOrCode += userCred.UserPassword + @"</p><p>login  <a href=""";
                    userCred.LinkOrCode += loginURL;
                    userCred.LinkOrCode += @"""> here </a></p> ";
                    userCred.LinkOrCode += "<p>Or paste the following link in your browser address bar </p> ";
                    userCred.LinkOrCode += "<p>" + loginURL + " </p> ";
                    await _emailSender.SendEmailAsync(userCred.UserEmail, subject, userCred.LinkOrCode);
                    userCred.UserPassword = null;
                }
                return Ok(x);
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }


        [AllowAnonymous]
        [HttpGet("GetHostName")]
        public IActionResult GetHostName()
        {
            //to get host at server
            try
            {
                var a = Request.Host;
                return Ok(a.Host);
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody]UserCred userParam)
        {
            try
            {
                var userIdClaim = HttpContext.User.Claims.Where(c => c.Type == ClaimTypes.Name).First();
                var userID = int.Parse(userIdClaim.Value);
                var serviceResponse = await _userService.ChangePassword(userParam, userID);

                if (serviceResponse == null)
                    return BadRequest(new { message = "An error occoured!" });
                return StatusCode((int)(serviceResponse.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), serviceResponse);



            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }

        }

        [AllowAnonymous]
        [HttpPost("ForgetPassword")]
        public async Task<IActionResult> ForgerPassword([FromBody]UserCred userParam)
        {
            try
            {
                var serviceResponse = await _userService.ForgerPassword(userParam);

                if (serviceResponse == null)
                    return BadRequest(new { message = "An error occoured!" });

                if (serviceResponse.Data != null && serviceResponse.Data is string)
                {
                    var a = Request.Host;
                    string loginURL = GetLoginUrl(a);

                    string subject = "Forget Password Request for open-source-pos Account";
                    string LinkOrCode = @"<p> Use the following Password To Login to Your open-source-pos Account <br/>";
                    LinkOrCode += ((string)serviceResponse.Data) + @"</p><p>login  <a href=""";
                    LinkOrCode += loginURL;
                    LinkOrCode += @"""> here </a></p> ";
                    LinkOrCode += "<p>Or paste the following link in your browser address bar </p> ";
                    LinkOrCode += "<p>" + loginURL + " </p> ";
                    await _emailSender.SendEmailAsync(userParam.UserEmail, subject, LinkOrCode);
                    //this statement is very important it removes user password from response
                    serviceResponse.Data = null;
                }

                return StatusCode((int)(serviceResponse.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), serviceResponse);



            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }

        }

        private static string GetLoginUrl(Microsoft.AspNetCore.Http.HostString hostString)
        {
            string loginURL = "";
            if (hostString.Host == "localhost")
            {
                loginURL = @"http://localhost:4200/#/login";
            }
            else if (hostString.Host == "open-source-pos.alishah.pro")
            {
                loginURL = @"http://open-source-pos.alishah.pro/#/login";
            }
            else if (hostString.Host == "vss-server")
            {
                loginURL = @"http://vss-server:9211/#/home/login";
            }

            return loginURL;
        }

        /// <summary>
        /// Get Users of a particular company for listing
        /// </summary>
        /// <param name="jSearchUsers"></param>
        /// <returns></returns>
        [Authorize]
        [Route("getCompanyUsers")]
        [HttpPost]
        public async Task<IActionResult> GetCompanyUsers([FromBody] JObject jSearchUsers)
        {
            try
            {
                dynamic SearchUsers = jSearchUsers;
                string query = SearchUsers.query;
                int companyId = SearchUsers.companyId;
                int limit = SearchUsers.limit;
                int offset = SearchUsers.offset;

                ServiceResponse response = await _userService.GetUsersAsync(query, companyId, limit, offset);


                return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }

        }

        [Authorize]
        [HttpPost("CreateCompanyUser")]        
        public async Task<IActionResult> CreateCompanyUser([FromBody] UserCred userCred)
        {
            try
            {
                var a = Request.Host;
                // The Company users are not admin.
                userCred.IsAdmin = false;
                // save 
                var x = await _userService.Create(userCred);
                if (x != null)
                {
                    string loginURL = GetLoginUrl(a);

                    string subject = "Email Conformation for open-source-pos";
                    userCred.LinkOrCode = @"<p> Use the following Password To Login to Your open-source-pos Account <br/>";
                    userCred.LinkOrCode += userCred.UserPassword + @"</p><p>login  <a href=""";
                    userCred.LinkOrCode += loginURL;
                    userCred.LinkOrCode += @"""> here </a></p> ";
                    userCred.LinkOrCode += "<p>Or paste the following link in your browser address bar </p> ";
                    userCred.LinkOrCode += "<p>" + loginURL + " </p> ";
                    await _emailSender.SendEmailAsync(userCred.UserEmail, subject, userCred.LinkOrCode);
                    userCred.UserPassword = null;
                }
                ServiceResponse response = new ServiceResponse();
                response.Data = x;
                response.Title = ServiceMessages.TitleSuccess;
                response.Message = ServiceMessages.DataSaved;
                response.Flag = true;
                response.IsValid = true;
                return Ok(response);
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                ServiceResponse response = new ServiceResponse();
                response.Data = null;
                response.Title = ServiceMessages.TitleFailure;
                response.Message = ex.Message;
                response.Flag = false;
                response.IsValid = false;
                return StatusCode((int)HttpStatusCode.InternalServerError, response);
            }
        }

        [Authorize]
        [HttpPost("UpdateUserProfile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserCred userCred)
        {
            try
            {
                
                // update
                var response = await _userService.UpdateUserProfile(userCred);

                return StatusCode((int)(response.IsValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest), response);
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(ex.Message);
            }
        }

    }
    
}
