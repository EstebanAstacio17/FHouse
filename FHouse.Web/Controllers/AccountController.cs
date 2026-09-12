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
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginDto { ReturnUrl = returnUrl });
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

            // Iniciar sesión con OWIN Cookie
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties
            {
                IsPersistent = dto.Recordarme
            }, resultado.Datos);

            // Almacenar datos en sesión para acceso rápido
            var familiaIdClaim = resultado.Datos.FindFirst("FamiliaId");
            if (familiaIdClaim != null && int.TryParse(familiaIdClaim.Value, out int fid))
            {
                Session["FamiliaId"] = fid;
            }
            Session["NombreCompleto"] = resultado.Datos.FindFirst("NombreCompleto")?.Value;
            Session["Email"] = resultado.Datos.FindFirst(ClaimTypes.Email)?.Value;
            Session["FamiliaNombre"] = resultado.Datos.FindFirst("FamiliaNombre")?.Value;
            Session["Rol"] = resultado.Datos.FindFirst(ClaimTypes.Role)?.Value;

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
            if (User.Identity.IsAuthenticated)
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

            // Iniciar sesión automáticamente
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties
            {
                IsPersistent = true
            }, resultado.Datos);

            var familiaIdClaim = resultado.Datos.FindFirst("FamiliaId");
            if (familiaIdClaim != null && int.TryParse(familiaIdClaim.Value, out int fid))
            {
                Session["FamiliaId"] = fid;
            }
            Session["NombreCompleto"] = resultado.Datos.FindFirst("NombreCompleto")?.Value;
            Session["Email"] = resultado.Datos.FindFirst(ClaimTypes.Email)?.Value;
            Session["FamiliaNombre"] = resultado.Datos.FindFirst("FamiliaNombre")?.Value;
            Session["Rol"] = resultado.Datos.FindFirst(ClaimTypes.Role)?.Value;

            TempData["Exito"] = "¡Cuenta creada exitosamente! Bienvenido a F House.";
            return RedirectToAction("Index", "Dashboard");
        }

        // POST/GET: /Account/Logout
        [Authorize]
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Logout()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/Perfil
        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Perfil()
        {
            var identity = User.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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

            Session["NombreCompleto"] = dto.NombreCompleto.Trim();
            Session["Email"] = dto.Email.Trim();

            // Sincronizar Cookie de Autenticación OWIN
            if (identity != null)
            {
                var newIdentity = new ClaimsIdentity(identity);
                var existingNombreClaim = newIdentity.FindFirst("NombreCompleto");
                if (existingNombreClaim != null) newIdentity.RemoveClaim(existingNombreClaim);
                newIdentity.AddClaim(new Claim("NombreCompleto", dto.NombreCompleto.Trim()));

                var existingEmailClaim = newIdentity.FindFirst(ClaimTypes.Email);
                if (existingEmailClaim != null) newIdentity.RemoveClaim(existingEmailClaim);
                newIdentity.AddClaim(new Claim(ClaimTypes.Email, dto.Email.Trim()));

                var existingNameClaim = newIdentity.FindFirst(ClaimTypes.Name);
                if (existingNameClaim != null) newIdentity.RemoveClaim(existingNameClaim);
                newIdentity.AddClaim(new Claim(ClaimTypes.Name, dto.Email.Trim()));

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
    }
}
