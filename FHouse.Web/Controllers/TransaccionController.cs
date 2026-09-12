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
                    Response.Headers.Add("HX-Trigger", "transaccionCreada");
                    var ultimas = await _transaccionService.ObtenerTransaccionesFamiliaAsync(dto.FamiliaId, 10);
                    return PartialView("_ListaTransacciones", ultimas.Datos);
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
            string usuarioId = GetUsuarioId();
            var resultado = await _transaccionService.AnularTransaccionAsync(id, usuarioId);

            if (IsHtmxRequest())
            {
                if (resultado.Exitoso)
                {
                    Response.Headers.Add("HX-Trigger", "transaccionAnulada");
                    var ultimas = await _transaccionService.ObtenerTransaccionesFamiliaAsync(GetFamiliaId(), 10);
                    return PartialView("_ListaTransacciones", ultimas.Datos);
                }

                Response.StatusCode = 400;
                return Content(resultado.Mensaje);
            }

            TempData[resultado.Exitoso ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToAction("Index");
        }
    }
}
