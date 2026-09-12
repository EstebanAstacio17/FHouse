using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FHouse.Core.Common;
using FHouse.Services.DTOs;

namespace FHouse.Services.Contracts
{
    public interface ITransaccionService
    {
        Task<ResultadoOperacion<TransaccionDetalleDto>> RegistrarTransaccionAsync(CrearTransaccionDto dto, string usuarioId, string nombreUsuario);
        Task<ResultadoOperacion<TransaccionDetalleDto>> ObtenerPorIdAsync(int id);
        Task<ResultadoOperacion<bool>> EditarTransaccionAsync(EditarTransaccionDto dto, string usuarioId);
        Task<ResultadoOperacion<IEnumerable<TransaccionDetalleDto>>> ObtenerTransaccionesFamiliaAsync(int familiaId, int? limit = null);
        Task<ResultadoOperacion<IEnumerable<TransaccionDetalleDto>>> ObtenerPorFuenteAsync(int fuenteId, int? limit = null);
        Task<ResultadoOperacion<IEnumerable<TransaccionDetalleDto>>> ObtenerFiltradasAsync(FiltroTransaccionDto filtro);
        Task<ResultadoOperacion<bool>> AnularTransaccionAsync(int transaccionId, string usuarioId, int? familiaId = null);
    }

    public interface IFuenteIngresoService
    {
        Task<ResultadoOperacion<FuenteIngresoDetalleDto>> CrearFuenteAsync(CrearFuenteIngresoDto dto, string usuarioId);
        Task<ResultadoOperacion<IEnumerable<FuenteIngresoDetalleDto>>> ObtenerPorFamiliaAsync(int familiaId);
        Task<ResultadoOperacion<FuenteIngresoDetalleDto>> ObtenerPorIdAsync(int id);
        Task<ResultadoOperacion<bool>> ActualizarFuenteAsync(int id, CrearFuenteIngresoDto dto);
        Task<ResultadoOperacion<bool>> EliminarFuenteAsync(int id, string usuarioId);
        Task<ResultadoOperacion<bool>> HabilitarFuenteAsync(int id, string usuarioId);
        Task<ResultadoOperacion<bool>> InhabilitarFuenteAsync(int id, string usuarioId);
    }

    public interface ICuentaService
    {
        Task<ResultadoOperacion<CuentaDetalleDto>> CrearCuentaAsync(CrearCuentaDto dto, string usuarioId);
        Task<ResultadoOperacion<bool>> ActualizarCuentaAsync(int id, CrearCuentaDto dto, string usuarioId);
        Task<ResultadoOperacion<bool>> EliminarCuentaAsync(int id, string usuarioId);
        Task<ResultadoOperacion<bool>> HabilitarCuentaAsync(int id, string usuarioId);
        Task<ResultadoOperacion<bool>> InhabilitarCuentaAsync(int id, string usuarioId);
        Task<ResultadoOperacion<IEnumerable<CuentaDetalleDto>>> ObtenerPorFamiliaAsync(int familiaId);
        Task<ResultadoOperacion<IEnumerable<CuentaDetalleDto>>> ObtenerPorFuenteAsync(int fuenteId);
        Task<ResultadoOperacion<bool>> RealizarTransferenciaAsync(TransferenciaCuentaDto dto, string usuarioId, string nombreUsuario);
    }

    public interface IDashboardService
    {
        Task<ResultadoOperacion<DashboardResumenDto>> ObtenerResumenDashboardAsync(int familiaId);
    }

    public interface ICategoriaService
    {
        Task<ResultadoOperacion<CategoriaDetalleDto>> CrearCategoriaAsync(CrearCategoriaDto dto);
        Task<ResultadoOperacion<IEnumerable<CategoriaDetalleDto>>> ObtenerPorFamiliaAsync(int familiaId);
        Task<ResultadoOperacion<bool>> EliminarCategoriaAsync(int id);
    }

    public interface IPresupuestoService
    {
        Task<ResultadoOperacion<PresupuestoDetalleDto>> CrearPresupuestoAsync(CrearPresupuestoDto dto);
        Task<ResultadoOperacion<IEnumerable<PresupuestoDetalleDto>>> ObtenerPorPeriodoAsync(int familiaId, int mes, int anio);
    }

    public interface ITasaCambioService
    {
        Task<ResultadoOperacion<decimal>> ObtenerTasaActualAsync(int familiaId);
        Task<ResultadoOperacion<bool>> ActualizarTasaAsync(int familiaId, decimal nuevaTasa, string usuarioId);
    }

    public interface IAuditService
    {
        Task<ResultadoOperacion<IEnumerable<AuditLogDetalleDto>>> ObtenerLogsFamiliaAsync(int familiaId, int limit = 100);
    }

    public interface IUsuarioFamiliaService
    {
        Task<ResultadoOperacion<FamiliaDetalleDto>> ObtenerFamiliaAsync(int familiaId);
        Task<ResultadoOperacion<bool>> ActualizarNombreFamiliaAsync(int familiaId, string nuevoNombre, string usuarioId);
        Task<ResultadoOperacion<string>> RegenerarCodigoInvitacionAsync(int familiaId, string usuarioId);
        Task<ResultadoOperacion<IEnumerable<MiembroFamiliaDto>>> ObtenerMiembrosFamiliaAsync(int familiaId);
        Task<ResultadoOperacion<MiembroFamiliaDto>> InvitarMiembroAsync(InvitarMiembroDto dto);
        Task<ResultadoOperacion<bool>> CambiarRolAsync(CambiarRolDto dto);
        Task<ResultadoOperacion<bool>> ActualizarMiembroAsync(ActualizarMiembroDto dto, string usuarioId);
        Task<ResultadoOperacion<bool>> InhabilitarMiembroAsync(int usuarioFamiliaId, string usuarioId);
        Task<ResultadoOperacion<bool>> HabilitarMiembroAsync(int usuarioFamiliaId, string usuarioId);
        Task<ResultadoOperacion<bool>> EliminarMiembroAsync(int usuarioFamiliaId, string usuarioId);
    }

    public interface IReporteService
    {
        Task<ResultadoOperacion<ReporteFinancieroDto>> GenerarReporteAsync(int familiaId, DateTime desde, DateTime hasta);
        Task<string> ExportarCsvAsync(int familiaId, DateTime desde, DateTime hasta);
    }

    public interface IAuthService
    {
        Task<ResultadoOperacion<System.Security.Claims.ClaimsIdentity>> ValidarLoginAsync(LoginDto dto);
        Task<ResultadoOperacion<System.Security.Claims.ClaimsIdentity>> RegistrarUsuarioAsync(RegisterDto dto);
        Task<ResultadoOperacion<UsuarioPerfilDto>> ObtenerPerfilUsuarioAsync(string usuarioId);
        Task<ResultadoOperacion<bool>> ActualizarPerfilAsync(string usuarioId, ActualizarPerfilDto dto);
        Task<ResultadoOperacion<bool>> CambiarPasswordAsync(string usuarioId, CambiarPasswordDto dto);
    }
}
