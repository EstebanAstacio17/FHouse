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

            Session["FamiliaId"] = 1;
            return 1;
        }

        protected string GetFamiliaNombre()
        {
            if (Session["FamiliaNombre"] != null && !string.IsNullOrWhiteSpace(Session["FamiliaNombre"].ToString()))
            {
                return Session["FamiliaNombre"].ToString();
            }

            int familiaId = GetFamiliaId();
            try
            {
                var db = DependencyResolver.Current.GetService<FHouseDbContext>() ?? new FHouseDbContext();
                var fam = db.Familias.Find(familiaId);
                if (fam != null && !string.IsNullOrWhiteSpace(fam.Nombre))
                {
                    Session["FamiliaNombre"] = fam.Nombre;
                    return fam.Nombre;
                }
            }
            catch { }

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
            var claim = identity?.FindFirst(ClaimTypes.NameIdentifier);
            return claim?.Value ?? "usr-admin-001";
        }

        protected string GetNombreUsuario()
        {
            if (Session["NombreCompleto"] != null && !string.IsNullOrWhiteSpace(Session["NombreCompleto"].ToString()))
            {
                return Session["NombreCompleto"].ToString();
            }

            var usuarioId = GetUsuarioId();
            try
            {
                var db = DependencyResolver.Current.GetService<FHouseDbContext>() ?? new FHouseDbContext();
                var user = db.Users.Find(usuarioId);
                if (user != null && !string.IsNullOrWhiteSpace(user.NombreCompleto))
                {
                    Session["NombreCompleto"] = user.NombreCompleto;
                    return user.NombreCompleto;
                }
            }
            catch { }

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
            if (Session["Email"] != null && !string.IsNullOrWhiteSpace(Session["Email"].ToString()))
            {
                return Session["Email"].ToString();
            }

            var usuarioId = GetUsuarioId();
            try
            {
                var db = DependencyResolver.Current.GetService<FHouseDbContext>() ?? new FHouseDbContext();
                var user = db.Users.Find(usuarioId);
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    Session["Email"] = user.Email;
                    return user.Email;
                }
            }
            catch { }

            var identity = User?.Identity as ClaimsIdentity;
            var claim = identity?.FindFirst(ClaimTypes.Email);
            return claim?.Value ?? (User?.Identity?.Name ?? "admin@fhouse.com");
        }

        protected string GetRolUsuario()
        {
            if (Session["Rol"] != null && !string.IsNullOrWhiteSpace(Session["Rol"].ToString()))
            {
                return Session["Rol"].ToString();
            }

            var identity = User?.Identity as ClaimsIdentity;
            var claim = identity?.FindFirst(ClaimTypes.Role);
            return claim?.Value ?? "Admin";
        }

        protected bool IsHtmxRequest()
        {
            return Request.Headers["HX-Request"] == "true";
        }
    }
}
