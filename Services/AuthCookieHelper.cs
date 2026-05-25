using System;
using Microsoft.AspNetCore.Http;
using Models;

namespace Services
{
    public static class AuthCookieHelper
    {
        public static void SetRememberMeCookies(HttpResponse response, string accessToken, string refreshToken, AppSettings settings)
        {
            var accessMinutes = ParseInt(settings.AccessTokenExpiresMinutes, 15);
            var refreshDays = ParseInt(settings.RefreshTokenExpiresDays, 30);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true
            };

            response.Cookies.Append(AuthConstants.AccessTokenCookieName, accessToken,
                new CookieOptions
                {
                    HttpOnly = cookieOptions.HttpOnly,
                    Secure = cookieOptions.Secure,
                    SameSite = cookieOptions.SameSite,
                    Path = cookieOptions.Path,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(accessMinutes)
                });

            response.Cookies.Append(AuthConstants.RefreshTokenCookieName, refreshToken,
                new CookieOptions
                {
                    HttpOnly = cookieOptions.HttpOnly,
                    Secure = cookieOptions.Secure,
                    SameSite = cookieOptions.SameSite,
                    Path = cookieOptions.Path,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(refreshDays)
                });
        }

        public static void ClearRememberMeCookies(HttpResponse response)
        {
            response.Cookies.Delete(AuthConstants.AccessTokenCookieName, new CookieOptions { Path = "/" });
            response.Cookies.Delete(AuthConstants.RefreshTokenCookieName, new CookieOptions { Path = "/" });
        }

        private static int ParseInt(string value, int fallback) =>
            int.TryParse(value, out var n) && n > 0 ? n : fallback;
    }
}
