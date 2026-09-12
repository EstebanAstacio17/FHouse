using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using FHouse.Services.Contracts;
using FHouse.Services.DTOs;

namespace FHouse.Web.Controllers
{
    public class TransaccionController : BaseController
    {
        private readonly ITransaccionService _transaccionService;
        private readonly IFuenteIngresoService _fuenteService;
        private readonly ICuentaService _cuentaService;
        private readonly ICategoriaService _categoriaService;

        public TransaccionController(
            ITransaccionService transaccionService,
            IFuenteIngresoService fuenteService,
            ICuentaService cuentaService,
            ICategoriaService categoriaService)
        {
            _transaccionService = transaccionService ?? throw new ArgumentNullException(nameof(transaccionService));
            _fuenteService = fuenteService ?? throw new ArgumentNullException(nameof(fuenteService));
            _cuentaService = cuentaService ?? throw new ArgumentNullException(nameof(cuentaService));
            _categoriaService = categoriaService ?? throw new ArgumentNullException(nameof(categoriaService));
        }

        [HttpGet]
        public async Task<ActionResult> OpcionesModal()
        {
            int familiaId = GetFamiliaId();
            var fuentes = await _fuenteService.ObtenerPorFamiliaAsync(familiaId);
            var cuentas = await _cuentaService.ObtenerPorFamiliaAsync(familiaId);
            var categorias = await _categoriaService.ObtenerPorFamiliaAsync(familiaId);

            ViewBag.Fuentes = fuentes?.Datos;
            ViewBag.Cuentas = cuentas?.Datos;
            ViewBag.Categorias = categorias?.Datos;

            return PartialView("_ModalTransaccionCampos");
        }

        [HttpGet]
        public async Task<ActionResult> Index(FiltroTransaccionDto filtro)
        {
            int familiaId = GetFamiliaId();
            filtro.FamiliaId = familiaId;

            var transacciones = await _transaccionService.ObtenerFiltradasAsync(filtro);
            var fuentes = await _fuenteService.ObtenerPorFamiliaAsync(familiaId);
            var cuentas = await _cuentaService.ObtenerPorFamiliaAsync(familiaId);
            var categorias = await _categoriaService.ObtenerPorFamiliaAsync(familiaId);

            ViewBag.Filtro = filtro;
            ViewBag.Fuentes = fuentes.Datos;
            ViewBag.Cuentas = cuentas.Datos;
            ViewBag.Categorias = categorias.Datos;

            if (IsHtmxRequest())
            {
                return PartialView("_ListaTransacciones", transacciones.Datos);
            }

            return View(transacciones.Datos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Crear(CrearTransaccionDto dto)
        {
            dto.FamiliaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            string nombreUsuario = GetNombreUsuario();

            var resultado = await _transaccionService.RegistrarTransaccionAsync(dto, usuarioId, nombreUsuario);

            if (IsHtmxRequest())
            {
                if (resultado.Exitoso)
                {
                    // Fire event so ALL live sections on any page can react
                    Response.Headers.Add("HX-Trigger", "transaccionCreada");
                    // Return the updated transaction list as the swap target
                    // The tbody in _ListaTransacciones listens for transaccionCreada
                    // and will re-fetch automatically via its own hx-trigger.
                    // We just need to return 200 OK so htmx fires the trigger.
                    return new HttpStatusCodeResult(200);
                }

                Response.StatusCode = 422;
                return PartialView("_ErroresValidacion", resultado.Errores);
            }

            if (!resultado.Exitoso)
            {
                TempData["Error"] = string.Join("<br/>", resultado.Errores);
            }
            else
            {
                TempData["Exito"] = resultado.Mensaje;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Anular(int id)
        {
            int familiaId = GetFamiliaId();
            string usuarioId = GetUsuarioId();
            var resultado = await _transaccionService.AnularTransaccionAsync(id, usuarioId, familiaId);

            if (IsHtmxRequest())
            {
                if (resultado.Exitoso)
                {
                    // Fire event so all live sections react (including Dashboard KPIs)
                    Response.Headers.Add("HX-Trigger", "transaccionAnulada");
                    return new HttpStatusCodeResult(200);
                }

                Response.StatusCode = 400;
                return Content(resultado.Mensaje);
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Dedicated endpoint for htmx to fetch only the recent transactions tbody partial.
        /// Used by _ListaTransacciones hx-trigger on transaccionCreada/transaccionAnulada events.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> ObtenerUltimas(int cantidad = 10)
        {
            int familiaId = GetFamiliaId();
            var ultimas = await _transaccionService.ObtenerTransaccionesFamiliaAsync(familiaId, cantidad);
            return PartialView("_ListaTransacciones", ultimas.Datos);
        }
    }
}
