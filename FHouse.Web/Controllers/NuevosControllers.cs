using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using FHouse.Core.Enums;
using FHouse.Services.Contracts;
using FHouse.Services.DTOs;

namespace FHouse.Web.Controllers
{
    public class ReporteController : BaseController
    {
        private readonly IReporteService _reporteService;

        public ReporteController(IReporteService reporteService)
        {
            _reporteService = reporteService ?? throw new ArgumentNullException(nameof(reporteService));
        }

        [HttpGet]
        public async Task<ActionResult> Index(DateTime? desde, DateTime? hasta)
        {
            int familiaId = GetFamiliaId();
            var ahora = DateTime.UtcNow;
            var fechaDesde = desde ?? new DateTime(ahora.Year, ahora.Month, 1);
            var fechaHasta = hasta ?? fechaDesde.AddMonths(1).AddDays(-1);

            var reporte = await _reporteService.GenerarReporteAsync(familiaId, fechaDesde, fechaHasta);
            return View(reporte.Datos);
        }

        [HttpGet]
        public async Task<ActionResult> ExportarCsv(DateTime? desde, DateTime? hasta)
        {
            int familiaId = GetFamiliaId();
            var ahora = DateTime.UtcNow;
            var fechaDesde = desde ?? new DateTime(ahora.Year, ahora.Month, 1);
            var fechaHasta = hasta ?? fechaDesde.AddMonths(1).AddDays(-1);

            // SECURITY/RESILIENCE: Limit date range to 1 year max to prevent DoS via large queries
            if ((fechaHasta - fechaDesde).TotalDays > 366)
            {
                TempData["Error"] = "El rango máximo de exportación es 1 año.";
                return RedirectToAction("Index");
            }

            var csv = await _reporteService.ExportarCsvAsync(familiaId, fechaDesde, fechaHasta);
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"FHouse_Reporte_{fechaDesde:yyyyMMdd}_{fechaHasta:yyyyMMdd}.csv");
        }
    }

    public class UsuarioController : BaseController
    {
        private readonly IUsuarioFamiliaService _usuarioService;

        public UsuarioController(IUsuarioFamiliaService usuarioService)
        {
            _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var familia = await _usuarioService.ObtenerFamiliaAsync(familiaId);
            var miembros = await _usuarioService.ObtenerMiembrosFamiliaAsync(familiaId);

            // Sincronizar el rol del usuario actual autenticado directamente desde la base de datos
            var miembroActual = miembros.Datos?.FirstOrDefault(m => string.Equals(m.UsuarioId, usuarioId, StringComparison.OrdinalIgnoreCase));
            if (miembroActual != null)
            {
                Session["Rol"] = miembroActual.Rol.ToString();
            }

            if (familia.Exitoso && familia.Datos != null)
            {
                Session["FamiliaNombre"] = familia.Datos.Nombre;
                ViewBag.FamiliaNombreActual = familia.Datos.Nombre;
            }

            ViewBag.Familia = familia.Datos;
            ViewBag.UsuarioIdActual = usuarioId;
            ViewBag.EsAdmin = EsAdminFamilia();
            return View(miembros.Datos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ActualizarFamilia(ActualizarFamiliaDto dto)
        {
            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _usuarioService.ActualizarNombreFamiliaAsync(familiaId, dto.Nombre, usuarioId);

            if (resultado.Exitoso)
            {
                Session["FamiliaNombre"] = dto.Nombre;
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RegenerarCodigo()
        {
            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _usuarioService.RegenerarCodigoInvitacionAsync(familiaId, usuarioId);

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Invitar(InvitarMiembroDto dto)
        {
            dto.FamiliaId = GetFamiliaId();
            var resultado = await _usuarioService.InvitarMiembroAsync(dto);

            if (IsHtmxRequest())
            {
                if (resultado.Exitoso)
                {
                    Response.Headers.Add("HX-Trigger", "miembroAgregado");
                    var miembros = await _usuarioService.ObtenerMiembrosFamiliaAsync(dto.FamiliaId);
                    return PartialView("_ListaMiembros", miembros.Datos);
                }
                Response.StatusCode = 422;
                return PartialView("_ErroresValidacion", resultado.Errores);
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CambiarRol(CambiarRolDto dto)
        {
            // SECURITY: Only Admins can change roles within the family
            if (!EsAdminFamilia())
            {
                TempData["Error"] = "No tienes permisos de administrador para cambiar roles de miembros.";
                return RedirectToAction("Index");
            }
            dto.UsuarioIdSolicitante = GetUsuarioId();
            var resultado = await _usuarioService.CambiarRolAsync(dto);
            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ActualizarMiembro(ActualizarMiembroDto dto)
        {
            string usuarioId = GetUsuarioId();
            var resultado = await _usuarioService.ActualizarMiembroAsync(dto, usuarioId);

            if (IsHtmxRequest())
            {
                int familiaId = GetFamiliaId();
                var miembros = await _usuarioService.ObtenerMiembrosFamiliaAsync(familiaId);
                return PartialView("_ListaMiembros", miembros.Datos);
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Inhabilitar(int id)
        {
            if (!EsAdminFamilia())
            {
                TempData["Error"] = "No tienes permisos de administrador para inhabilitar miembros.";
                return RedirectToAction("Index");
            }

            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _usuarioService.InhabilitarMiembroAsync(id, usuarioId, familiaId);

            if (IsHtmxRequest())
            {
                var miembros = await _usuarioService.ObtenerMiembrosFamiliaAsync(familiaId);
                return PartialView("_ListaMiembros", miembros.Datos);
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Habilitar(int id)
        {
            if (!EsAdminFamilia())
            {
                TempData["Error"] = "No tienes permisos de administrador para habilitar miembros.";
                return RedirectToAction("Index");
            }

            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _usuarioService.HabilitarMiembroAsync(id, usuarioId, familiaId);

            if (IsHtmxRequest())
            {
                var miembros = await _usuarioService.ObtenerMiembrosFamiliaAsync(familiaId);
                return PartialView("_ListaMiembros", miembros.Datos);
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Eliminar(int id)
        {
            if (!EsAdminFamilia())
            {
                TempData["Error"] = "No tienes permisos de administrador para eliminar miembros.";
                return RedirectToAction("Index");
            }

            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _usuarioService.EliminarMiembroAsync(id, usuarioId, familiaId);

            if (IsHtmxRequest())
            {
                var miembros = await _usuarioService.ObtenerMiembrosFamiliaAsync(familiaId);
                return PartialView("_ListaMiembros", miembros.Datos);
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Aprobar(int id, RolFamilia rol = RolFamilia.Miembro)
        {
            if (!EsAdminFamilia())
            {
                TempData["Error"] = "Solo los usuarios administradores pueden autorizar y aprobar el acceso a nuevos integrantes.";
                return RedirectToAction("Index");
            }

            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _usuarioService.AprobarMiembroAsync(id, rol, usuarioId, familiaId);

            if (IsHtmxRequest())
            {
                var miembros = await _usuarioService.ObtenerMiembrosFamiliaAsync(familiaId);
                return PartialView("_ListaMiembros", miembros.Datos);
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Rechazar(int id)
        {
            if (!EsAdminFamilia())
            {
                TempData["Error"] = "Solo los usuarios administradores pueden rechazar solicitudes de acceso.";
                return RedirectToAction("Index");
            }

            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _usuarioService.RechazarMiembroAsync(id, usuarioId, familiaId);

            if (IsHtmxRequest())
            {
                var miembros = await _usuarioService.ObtenerMiembrosFamiliaAsync(familiaId);
                return PartialView("_ListaMiembros", miembros.Datos);
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }
    }

    public class CategoriaController : BaseController
    {
        private readonly ICategoriaService _categoriaService;

        public CategoriaController(ICategoriaService categoriaService)
        {
            _categoriaService = categoriaService ?? throw new ArgumentNullException(nameof(categoriaService));
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            int familiaId = GetFamiliaId();
            var categorias = await _categoriaService.ObtenerPorFamiliaAsync(familiaId);
            return View(categorias.Datos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Crear(CrearCategoriaDto dto)
        {
            dto.FamiliaId = GetFamiliaId();
            var resultado = await _categoriaService.CrearCategoriaAsync(dto);
            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Eliminar(int id)
        {
            int familiaId = GetFamiliaId();
            var resultado = await _categoriaService.EliminarCategoriaAsync(id, familiaId);
            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }
    }
}
