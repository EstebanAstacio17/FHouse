using System;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartup(typeof(FHouse.Web.Startup))]

namespace FHouse.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                LogoutPath = new PathString("/Account/Logout"),
                ExpireTimeSpan = TimeSpan.FromHours(8),
                SlidingExpiration = true,
                CookieName = "FHouse_Auth",
                // SECURITY: JavaScript cannot read this cookie (mitigates XSS session theft)
                CookieHttpOnly = true,
                // SECURITY: Only sent over HTTPS in production; dev uses HTTP so conditional
                CookieSecure = CookieSecureOption.SameAsRequest,
                // SECURITY: Restrict cookie to same-site requests (CSRF mitigation)
                CookieSameSite = SameSiteMode.Strict,
                CookiePath = "/"
            });
        }
    }
}
