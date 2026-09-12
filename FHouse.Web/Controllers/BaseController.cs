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
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                // Never expose a fallback ID — fail secure
                throw new UnauthorizedAccessException("Sesión inválida o expirada.");
            }
            return userId;
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

            // Never default to Admin — fail with least privilege
            return "Miembro";
        }

        protected bool IsHtmxRequest()
        {
            return Request.Headers["HX-Request"] == "true";
        }

        /// <summary>
        /// Verifica que el usuario autenticado pertenece a la familia solicitada.
        /// Uso: llama esto antes de exponer recursos por familiaId.
        /// </summary>
        protected bool PerteneceFamilia(int familiaIdSolicitado)
        {
            return GetFamiliaId() == familiaIdSolicitado;
        }

        /// <summary>
        /// Verifica si el usuario tiene rol Admin dentro de la familia.
        /// </summary>
        protected bool EsAdminFamilia()
        {
            var rol = GetRolUsuario();
            return string.Equals(rol, "Admin", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Manejo global de UnauthorizedAccessException — redirige a Login.</summary>
        protected override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.Exception is UnauthorizedAccessException)
            {
                filterContext.ExceptionHandled = true;
                Session.Clear();
                Session.Abandon();
                filterContext.Result = RedirectToAction("Login", "Account");
                return;
            }
            base.OnException(filterContext);
        }
    }
}
