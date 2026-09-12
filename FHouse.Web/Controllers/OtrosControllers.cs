using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using FHouse.Services.Contracts;
using FHouse.Services.DTOs;

namespace FHouse.Web.Controllers
{
    public class FuenteIngresoController : BaseController
    {
        private readonly IFuenteIngresoService _fuenteService;
        private readonly ITransaccionService _transaccionService;

        public FuenteIngresoController(IFuenteIngresoService fuenteService, ITransaccionService transaccionService)
        {
            _fuenteService = fuenteService ?? throw new ArgumentNullException(nameof(fuenteService));
            _transaccionService = transaccionService ?? throw new ArgumentNullException(nameof(transaccionService));
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            int familiaId = GetFamiliaId();
            var fuentes = await _fuenteService.ObtenerPorFamiliaAsync(familiaId);
            return View(fuentes.Datos);
        }

        [HttpGet]
        public async Task<ActionResult> Detalle(int id)
        {
            var fuente = await _fuenteService.ObtenerPorIdAsync(id);
            if (!fuente.Exitoso || fuente.Datos == null || fuente.Datos.FamiliaId != GetFamiliaId())
            {
                TempData["Error"] = "Fuente de ingreso no encontrada o no tiene permisos para acceder.";
                return RedirectToAction("Index");
            }

            var transacciones = await _transaccionService.ObtenerPorFuenteAsync(id, 25);
            ViewBag.Transacciones = transacciones.Datos;

            return View(fuente.Datos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Crear(CrearFuenteIngresoDto dto)
        {
            dto.FamiliaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();

            var resultado = await _fuenteService.CrearFuenteAsync(dto, usuarioId);

            if (IsHtmxRequest())
            {
                if (resultado.Exitoso)
                {
                    Response.Headers.Add("HX-Trigger", "fuenteCreada");
                    var fuentes = await _fuenteService.ObtenerPorFamiliaAsync(dto.FamiliaId);
                    return PartialView("_ListaFuentes", fuentes.Datos);
                }
                Response.StatusCode = 422;
                return PartialView("_ErroresValidacion", resultado.Errores);
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Actualizar(int id, CrearFuenteIngresoDto dto)
        {
            int familiaId = GetFamiliaId();
            dto.FamiliaId = familiaId;
            var resultado = await _fuenteService.ActualizarFuenteAsync(id, dto, familiaId);

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Eliminar(int id)
        {
            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _fuenteService.EliminarFuenteAsync(id, usuarioId, familiaId);

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Inhabilitar(int id)
        {
            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _fuenteService.InhabilitarFuenteAsync(id, usuarioId, familiaId);

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Habilitar(int id)
        {
            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _fuenteService.HabilitarFuenteAsync(id, usuarioId, familiaId);

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }
    }

    public class CuentaController : BaseController
    {
        private readonly ICuentaService _cuentaService;
        private readonly IFuenteIngresoService _fuenteService;

        public CuentaController(ICuentaService cuentaService, IFuenteIngresoService fuenteService)
        {
            _cuentaService = cuentaService ?? throw new ArgumentNullException(nameof(cuentaService));
            _fuenteService = fuenteService ?? throw new ArgumentNullException(nameof(fuenteService));
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            int familiaId = GetFamiliaId();
            var cuentas = await _cuentaService.ObtenerPorFamiliaAsync(familiaId);
            var fuentes = await _fuenteService.ObtenerPorFamiliaAsync(familiaId);

            ViewBag.Fuentes = fuentes.Datos;
            return View(cuentas.Datos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Crear(CrearCuentaDto dto)
        {
            dto.FamiliaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();

            var resultado = await _cuentaService.CrearCuentaAsync(dto, usuarioId);

            if (IsHtmxRequest())
            {
                if (resultado.Exitoso)
                {
                    Response.Headers.Add("HX-Trigger", "cuentaCreada");
                    var cuentas = await _cuentaService.ObtenerPorFamiliaAsync(dto.FamiliaId);
                    return PartialView("_ListaCuentas", cuentas.Datos);
                }
                Response.StatusCode = 422;
                return PartialView("_ErroresValidacion", resultado.Errores);
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Transferir(TransferenciaCuentaDto dto)
        {
            dto.FamiliaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            string nombreUsuario = GetNombreUsuario();

            var resultado = await _cuentaService.RealizarTransferenciaAsync(dto, usuarioId, nombreUsuario);

            if (IsHtmxRequest())
            {
                if (resultado.Exitoso)
                {
                    Response.Headers.Add("HX-Trigger", "transferenciaExitosa");
                    var cuentas = await _cuentaService.ObtenerPorFamiliaAsync(dto.FamiliaId);
                    return PartialView("_ListaCuentas", cuentas.Datos);
                }
                Response.StatusCode = 422;
                return PartialView("_ErroresValidacion", resultado.Errores);
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Actualizar(int id, CrearCuentaDto dto)
        {
            int familiaId = GetFamiliaId();
            dto.FamiliaId = familiaId;
            string usuarioId = GetUsuarioId();
            var resultado = await _cuentaService.ActualizarCuentaAsync(id, dto, usuarioId, familiaId);

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Eliminar(int id)
        {
            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _cuentaService.EliminarCuentaAsync(id, usuarioId, familiaId);

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Inhabilitar(int id)
        {
            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _cuentaService.InhabilitarCuentaAsync(id, usuarioId, familiaId);

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Habilitar(int id)
        {
            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _cuentaService.HabilitarCuentaAsync(id, usuarioId, familiaId);

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }
    }

    public class PresupuestoController : BaseController
    {
        private readonly IPresupuestoService _presupuestoService;
        private readonly ICategoriaService _categoriaService;
        private readonly IFuenteIngresoService _fuenteService;

        public PresupuestoController(IPresupuestoService presupuestoService, ICategoriaService categoriaService, IFuenteIngresoService fuenteService)
        {
            _presupuestoService = presupuestoService ?? throw new ArgumentNullException(nameof(presupuestoService));
            _categoriaService = categoriaService ?? throw new ArgumentNullException(nameof(categoriaService));
            _fuenteService = fuenteService ?? throw new ArgumentNullException(nameof(fuenteService));
        }

        [HttpGet]
        public async Task<ActionResult> Index(int? mes, int? anio)
        {
            int familiaId = GetFamiliaId();
            var ahora = DateTime.UtcNow;
            int mesSel = mes ?? ahora.Month;
            int anioSel = anio ?? ahora.Year;

            var presupuestos = await _presupuestoService.ObtenerPorPeriodoAsync(familiaId, mesSel, anioSel);
            var categorias = await _categoriaService.ObtenerPorFamiliaAsync(familiaId);
            var fuentes = await _fuenteService.ObtenerPorFamiliaAsync(familiaId);

            ViewBag.Mes = mesSel;
            ViewBag.Anio = anioSel;
            ViewBag.Categorias = categorias.Datos;
            ViewBag.Fuentes = fuentes.Datos;

            return View(presupuestos.Datos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Crear(CrearPresupuestoDto dto)
        {
            dto.FamiliaId = GetFamiliaId();
            var resultado = await _presupuestoService.CrearPresupuestoAsync(dto);

            if (IsHtmxRequest())
            {
                if (resultado.Exitoso)
                {
                    Response.Headers.Add("HX-Trigger", "presupuestoCreado");
                    var presupuestos = await _presupuestoService.ObtenerPorPeriodoAsync(dto.FamiliaId, dto.Mes, dto.Anio);
                    return PartialView("_ListaPresupuestos", presupuestos.Datos);
                }
                Response.StatusCode = 422;
                return PartialView("_ErroresValidacion", resultado.Errores);
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index", new { mes = dto.Mes, anio = dto.Anio });
        }
    }

    public class ConfiguracionController : BaseController
    {
        private readonly ITasaCambioService _tasaService;
        private readonly IAuditService _auditService;

        public ConfiguracionController(ITasaCambioService tasaService, IAuditService auditService)
        {
            _tasaService = tasaService ?? throw new ArgumentNullException(nameof(tasaService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            int familiaId = GetFamiliaId();
            var tasa = await _tasaService.ObtenerTasaActualAsync(familiaId);
            var logs = await _auditService.ObtenerLogsFamiliaAsync(familiaId, 50);

            ViewBag.TasaCambio = tasa.Datos;
            return View(logs.Datos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ActualizarTasa(decimal tasa)
        {
            // SECURITY: Validate range to prevent abuse
            if (tasa <= 0 || tasa > 999999)
            {
                if (IsHtmxRequest())
                {
                    Response.StatusCode = 422;
                    return Content("<span class='text-rose-500'>Tasa de cambio inválida.</span>");
                }
                TempData["Error"] = "Tasa de cambio inválida.";
                return RedirectToAction("Index");
            }

            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();

            var resultado = await _tasaService.ActualizarTasaAsync(familiaId, tasa, usuarioId);

            if (IsHtmxRequest())
            {
                if (resultado.Exitoso)
                {
                    // SECURITY: Encode the value — never interpolate user-influenced values directly into HTML
                    var tasaFormateada = System.Web.HttpUtility.HtmlEncode($"RD$ {tasa:N2} por USD (Actualizado)");
                    return Content($"<span class='text-emerald-500 font-semibold'>{tasaFormateada}</span>");
                }
                Response.StatusCode = 422;
                var mensajeSeguro = System.Web.HttpUtility.HtmlEncode(resultado.Mensaje);
                return Content($"<span class='text-rose-500'>{mensajeSeguro}</span>");
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }
    }
}
