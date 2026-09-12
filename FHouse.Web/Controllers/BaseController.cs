using System;
using System.Security.Claims;
using System.Web.Mvc;
using FHouse.Infrastructure.Data;

namespace FHouse.Web.Controllers
{
    [Authorize]
    public abstract class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
            {
                var nombre = GetNombreUsuario();
                var email = GetEmailUsuario();
                var familia = GetFamiliaNombre();
                var rol = GetRolUsuario();

                ViewBag.UsuarioNombreActual = nombre;
                ViewBag.UsuarioEmailActual = email;
                ViewBag.FamiliaNombreActual = familia;
                ViewBag.UsuarioRolActual = rol;
            }
        }

        protected int GetFamiliaId()
        {
            if (Session["FamiliaId"] != null && int.TryParse(Session["FamiliaId"].ToString(), out int sFid))
            {
                return sFid;
            }

            var identity = User?.Identity as ClaimsIdentity;
            var claim = identity?.FindFirst("FamiliaId");
            if (claim != null && int.TryParse(claim.Value, out int familiaId))
            {
                Session["FamiliaId"] = familiaId;
                return familiaId;
            }

            return 1;
        }

        protected string GetFamiliaNombre()
        {
            if (Session["FamiliaNombre"] != null)
            {
                return Session["FamiliaNombre"].ToString();
            }

            var identity = User?.Identity as ClaimsIdentity;
            var claim = identity?.FindFirst("FamiliaNombre");
            if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
            {
                Session["FamiliaNombre"] = claim.Value;
                return claim.Value;
            }

            return "Mi Familia";
        }

        protected string GetUsuarioId()
        {
            var identity = User?.Identity as ClaimsIdentity;
            return identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "usr-admin-001";
        }

        protected string GetNombreUsuario()
        {
            if (Session["NombreCompleto"] != null)
            {
                return Session["NombreCompleto"].ToString();
            }

            var identity = User?.Identity as ClaimsIdentity;
            var claim = identity?.FindFirst("NombreCompleto");
            if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
            {
                Session["NombreCompleto"] = claim.Value;
                return claim.Value;
            }

            return User?.Identity?.Name ?? "Usuario";
        }

        protected string GetEmailUsuario()
        {
            if (Session["Email"] != null)
            {
                return Session["Email"].ToString();
            }

            var identity = User?.Identity as ClaimsIdentity;
            var claim = identity?.FindFirst(ClaimTypes.Email);
            if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
            {
                Session["Email"] = claim.Value;
                return claim.Value;
            }

            return User?.Identity?.Name ?? "admin@fhouse.com";
        }

        protected string GetRolUsuario()
        {
            if (Session["Rol"] != null)
            {
                return Session["Rol"].ToString();
            }

            var identity = User?.Identity as ClaimsIdentity;
            var claim = identity?.FindFirst(ClaimTypes.Role);
            if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
            {
                Session["Rol"] = claim.Value;
                return claim.Value;
            }

            return "Admin";
        }

        protected bool IsHtmxRequest()
        {
            return Request.Headers["HX-Request"] == "true";
        }
    }
}
