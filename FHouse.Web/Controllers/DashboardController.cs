using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using FHouse.Services.Contracts;
using FHouse.Services.DTOs;

namespace FHouse.Web.Controllers
{
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;
        private readonly IFuenteIngresoService _fuenteService;
        private readonly ICuentaService _cuentaService;
        private readonly ICategoriaService _categoriaService;

        public DashboardController(
            IDashboardService dashboardService,
            IFuenteIngresoService fuenteService,
            ICuentaService cuentaService,
            ICategoriaService categoriaService)
        {
            _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
            _fuenteService = fuenteService ?? throw new ArgumentNullException(nameof(fuenteService));
            _cuentaService = cuentaService ?? throw new ArgumentNullException(nameof(cuentaService));
            _categoriaService = categoriaService ?? throw new ArgumentNullException(nameof(categoriaService));
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            int familiaId = GetFamiliaId();
            var resultado = await _dashboardService.ObtenerResumenDashboardAsync(familiaId);

            if (!resultado.Exitoso)
            {
                ViewBag.Error = resultado.Mensaje;
                return View(new DashboardResumenDto { FamiliaId = familiaId, NombreFamilia = "F House" });
            }

            var fuentes = await _fuenteService.ObtenerPorFamiliaAsync(familiaId);
            var cuentas = await _cuentaService.ObtenerPorFamiliaAsync(familiaId);
            var categorias = await _categoriaService.ObtenerPorFamiliaAsync(familiaId);

            ViewBag.Fuentes = fuentes.Datos;
            ViewBag.Cuentas = cuentas.Datos;
            ViewBag.Categorias = categorias.Datos;

            return View(resultado.Datos);
        }
    }
}
