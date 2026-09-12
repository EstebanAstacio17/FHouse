using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using FHouse.Services.Contracts;
using FHouse.Services.DTOs;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;

namespace FHouse.Web.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

        // GET: /Account/Login
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            // Sanitizar ReturnUrl contra Open Redirect
            string sanitizedReturnUrl = (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) 
                ? returnUrl 
                : null;

            ViewBag.ReturnUrl = sanitizedReturnUrl;
            return View(new LoginDto { ReturnUrl = sanitizedReturnUrl });
        }

        // POST: /Account/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var resultado = await _authService.ValidarLoginAsync(dto);
            if (!resultado.Exitoso)
            {
                ModelState.AddModelError("", resultado.Mensaje);
                return View(dto);
            }

            // Mitigación de Session Fixation: limpiar estado previo antes de emitir tickets
            Session.Clear();

            // Iniciar sesión con OWIN Cookie
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties
            {
                IsPersistent = dto.Recordarme
            }, resultado.Datos);

            // Almacenar datos en sesión para acceso rápido
            EstablecerDatosDeSesion(resultado.Datos);

            if (!string.IsNullOrEmpty(dto.ReturnUrl) && Url.IsLocalUrl(dto.ReturnUrl))
            {
                return Redirect(dto.ReturnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        // GET: /Account/Register
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Register(string codigo)
        {
            if (User?.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View(new RegisterDto { CodigoInvitacion = codigo });
        }

        // POST: /Account/Register
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var resultado = await _authService.RegistrarUsuarioAsync(dto);
            if (!resultado.Exitoso)
            {
                ModelState.AddModelError("", resultado.Mensaje);
                return View(dto);
            }

            // Mitigación de Session Fixation
            Session.Clear();

            // Si la cuenta requiere aprobación previa por un usuario activo existente
            if (resultado.Datos == null)
            {
                TempData["Exito"] = resultado.Mensaje;
                return RedirectToAction("Login", "Account");
            }

            // Iniciar sesión automáticamente únicamente para el primer usuario administrador
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties
            {
                IsPersistent = true
            }, resultado.Datos);

            EstablecerDatosDeSesion(resultado.Datos);

            TempData["Exito"] = "¡Cuenta creada exitosamente! Bienvenido a F House.";
            return RedirectToAction("Index", "Dashboard");
        }

        // POST/GET: /Account/Logout
        [Authorize]
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Logout()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie, DefaultAuthenticationTypes.ExternalCookie);
            Session.Clear();
            Session.Abandon();

            // Limpieza y expiración de cookies del lado del cliente
            if (Response.Cookies["FHouse_Auth"] != null)
            {
                Response.Cookies["FHouse_Auth"].Expires = DateTime.UtcNow.AddDays(-1);
            }
            if (Response.Cookies["ASP.NET_SessionId"] != null)
            {
                Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.UtcNow.AddDays(-1);
            }

            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/Perfil
        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Perfil()
        {
            var identity = User.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "Sesión no válida o expirada.";
                return RedirectToAction("Login");
            }

            var resultado = await _authService.ObtenerPerfilUsuarioAsync(userId);
            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;
                return RedirectToAction("Index", "Dashboard");
            }

            Session["NombreCompleto"] = resultado.Datos.NombreCompleto;
            Session["Email"] = resultado.Datos.Email;
            Session["FamiliaNombre"] = resultado.Datos.NombreFamilia;
            Session["Rol"] = resultado.Datos.Rol;
            ViewBag.UsuarioNombreActual = resultado.Datos.NombreCompleto;
            ViewBag.UsuarioEmailActual = resultado.Datos.Email;
            ViewBag.FamiliaNombreActual = resultado.Datos.NombreFamilia;
            ViewBag.UsuarioRolActual = resultado.Datos.Rol;

            return View(resultado.Datos);
        }

        // POST: /Account/ActualizarPerfil
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ActualizarPerfil(ActualizarPerfilDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Los datos ingresados para el perfil no son válidos.";
                return RedirectToAction("Perfil");
            }

            var identity = User.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "Sesión inválida.";
                return RedirectToAction("Login");
            }

            var resultado = await _authService.ActualizarPerfilAsync(userId, dto);
            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;
                return RedirectToAction("Perfil");
            }

            Session["NombreCompleto"] = dto.NombreCompleto?.Trim();
            Session["Email"] = dto.Email?.Trim();

            // Sincronizar Cookie de Autenticación OWIN
            if (identity != null)
            {
                var newIdentity = new ClaimsIdentity(identity);
                
                var existingNombreClaim = newIdentity.FindFirst("NombreCompleto");
                if (existingNombreClaim != null) newIdentity.RemoveClaim(existingNombreClaim);
                newIdentity.AddClaim(new Claim("NombreCompleto", dto.NombreCompleto?.Trim() ?? string.Empty));

                var existingEmailClaim = newIdentity.FindFirst(ClaimTypes.Email);
                if (existingEmailClaim != null) newIdentity.RemoveClaim(existingEmailClaim);
                newIdentity.AddClaim(new Claim(ClaimTypes.Email, dto.Email?.Trim() ?? string.Empty));

                var existingNameClaim = newIdentity.FindFirst(ClaimTypes.Name);
                if (existingNameClaim != null) newIdentity.RemoveClaim(existingNameClaim);
                newIdentity.AddClaim(new Claim(ClaimTypes.Name, dto.Email?.Trim() ?? string.Empty));

                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                AuthenticationManager.SignIn(new AuthenticationProperties { IsPersistent = true }, newIdentity);
            }

            TempData["Exito"] = resultado.Mensaje;
            return RedirectToAction("Perfil");
        }

        // POST: /Account/CambiarPassword
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CambiarPassword(CambiarPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Los datos de contraseña no cumplen con los requisitos de seguridad.";
                return RedirectToAction("Perfil");
            }

            var identity = User.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "Sesión inválida.";
                return RedirectToAction("Login");
            }

            var resultado = await _authService.CambiarPasswordAsync(userId, dto);
            if (!resultado.Exitoso)
            {
                TempData["Error"] = resultado.Mensaje;
                return RedirectToAction("Perfil");
            }

            TempData["Exito"] = resultado.Mensaje;
            return RedirectToAction("Perfil");
        }

        #region Helpers
        private void EstablecerDatosDeSesion(ClaimsIdentity identity)
        {
            if (identity == null) return;

            var familiaIdClaim = identity.FindFirst("FamiliaId");
            if (familiaIdClaim != null && int.TryParse(familiaIdClaim.Value, out int fid))
            {
                Session["FamiliaId"] = fid;
            }
            Session["NombreCompleto"] = identity.FindFirst("NombreCompleto")?.Value;
            Session["Email"] = identity.FindFirst(ClaimTypes.Email)?.Value;
            Session["FamiliaNombre"] = identity.FindFirst("FamiliaNombre")?.Value;
            Session["Rol"] = identity.FindFirst(ClaimTypes.Role)?.Value;
        }
        #endregion
    }
}
