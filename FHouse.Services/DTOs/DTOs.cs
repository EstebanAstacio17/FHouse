using System;
using System.Collections.Generic;
using FHouse.Core.Enums;

namespace FHouse.Services.DTOs
{
    public class CrearTransaccionDto
    {
        public string Concepto { get; set; }
        public decimal Monto { get; set; }
        public Moneda Moneda { get; set; } = Moneda.DOP;
        public TipoTransaccion Tipo { get; set; } = TipoTransaccion.Egreso;
        public DateTime FechaTransaccion { get; set; } = DateTime.UtcNow;
        public int FamiliaId { get; set; }
        public int? FuenteIngresoId { get; set; }
        public int? CuentaOrigenId { get; set; }
        public int? CuentaDestinoId { get; set; }
        public int? CategoriaId { get; set; }
        public string Comentario { get; set; }
        public string ComprobanteUrl { get; set; }
    }

    public class EditarTransaccionDto
    {
        public int Id { get; set; }
        public string Concepto { get; set; }
        public int? FuenteIngresoId { get; set; }
        public int? CategoriaId { get; set; }
        public string Comentario { get; set; }
    }

    public class TransaccionDetalleDto
    {
        public int Id { get; set; }
        public string Concepto { get; set; }
        public decimal Monto { get; set; }
        public Moneda Moneda { get; set; }
        public decimal TasaCambioAplicada { get; set; }
        public decimal MontoEnDOP { get; set; }
        public TipoTransaccion Tipo { get; set; }
        public DateTime FechaTransaccion { get; set; }
        public int FamiliaId { get; set; }
        public int? FuenteIngresoId { get; set; }
        public string NombreFuenteIngreso { get; set; }
        public string ColorFuenteIngreso { get; set; }
        public int? CuentaOrigenId { get; set; }
        public string NombreCuentaOrigen { get; set; }
        public int? CuentaDestinoId { get; set; }
        public string NombreCuentaDestino { get; set; }
        public int? CategoriaId { get; set; }
        public string NombreCategoria { get; set; }
        public string IconoCategoria { get; set; }
        public string ColorCategoria { get; set; }
        public string UsuarioRegistradorId { get; set; }
        public string NombreUsuarioRegistrador { get; set; }
        public string Comentario { get; set; }
        public string ComprobanteUrl { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class FiltroTransaccionDto
    {
        public int FamiliaId { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int? FuenteIngresoId { get; set; }
        public int? CuentaId { get; set; }
        public int? CategoriaId { get; set; }
        public TipoTransaccion? Tipo { get; set; }
        public string UsuarioId { get; set; }
    }

    public class CrearFuenteIngresoDto
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public TipoFuenteIngreso Tipo { get; set; } = TipoFuenteIngreso.Negocio;
        public int FamiliaId { get; set; }
        public string ColorIdentificador { get; set; } = "#0071E3";
    }

    public class FuenteIngresoDetalleDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public TipoFuenteIngreso Tipo { get; set; }
        public int FamiliaId { get; set; }
        public string ColorIdentificador { get; set; }
        public bool Activo { get; set; } = true;
        public decimal TotalIngresosDOP { get; set; }
        public decimal TotalEgresosDOP { get; set; }
        public decimal BalanceNetoDOP => TotalIngresosDOP - TotalEgresosDOP;
        public int CantidadCuentas { get; set; }
        public int CantidadTransacciones { get; set; }
    }

    public class CrearCuentaDto
    {
        public string Nombre { get; set; }
        public string InstitucionFinanciera { get; set; }
        public string NumeroCuenta { get; set; }
        public TipoCuenta Tipo { get; set; } = TipoCuenta.Ahorro;
        public Moneda Moneda { get; set; } = Moneda.DOP;
        public decimal SaldoInicial { get; set; } = 0m;
        public int FamiliaId { get; set; }
        public int? FuenteIngresoId { get; set; }
    }

    public class CuentaDetalleDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string InstitucionFinanciera { get; set; }
        public string NumeroCuenta { get; set; }
        public TipoCuenta Tipo { get; set; }
        public Moneda Moneda { get; set; }
        public decimal SaldoActual { get; set; }
        public int FamiliaId { get; set; }
        public int? FuenteIngresoId { get; set; }
        public string NombreFuenteIngreso { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class TransferenciaCuentaDto
    {
        public int FamiliaId { get; set; }
        public int CuentaOrigenId { get; set; }
        public int CuentaDestinoId { get; set; }
        public decimal Monto { get; set; }
        public string Comentario { get; set; }
    }

    public class CrearCategoriaDto
    {
        public string Nombre { get; set; }
        public string Icono { get; set; } = "tag";
        public string Color { get; set; } = "#86868B";
        public TipoCategoria Tipo { get; set; } = TipoCategoria.Egreso;
        public int FamiliaId { get; set; }
    }

    public class CategoriaDetalleDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Icono { get; set; }
        public string Color { get; set; }
        public TipoCategoria Tipo { get; set; }
        public int FamiliaId { get; set; }
        public bool Activo { get; set; } = true;
        public decimal TotalGastadoMesDOP { get; set; }
    }

    public class CrearPresupuestoDto
    {
        public string Nombre { get; set; }
        public decimal MontoLimite { get; set; }
        public Moneda Moneda { get; set; } = Moneda.DOP;
        public int Mes { get; set; }
        public int Anio { get; set; }
        public int FamiliaId { get; set; }
        public int? CategoriaId { get; set; }
        public int? FuenteIngresoId { get; set; }
    }

    public class PresupuestoDetalleDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal MontoLimite { get; set; }
        public Moneda Moneda { get; set; }
        public decimal MontoEjecutadoDOP { get; set; }
        public decimal PorcentajeEjecutado => MontoLimite > 0 ? Math.Round((MontoEjecutadoDOP / MontoLimite) * 100, 2) : 0;
        public int Mes { get; set; }
        public int Anio { get; set; }
        public int FamiliaId { get; set; }
        public int? CategoriaId { get; set; }
        public string NombreCategoria { get; set; }
        public int? FuenteIngresoId { get; set; }
        public string NombreFuenteIngreso { get; set; }
    }

    public class MiembroFamiliaDto
    {
        public int Id { get; set; }
        public string UsuarioId { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public RolFamilia Rol { get; set; }
        public string AliasFamiliar { get; set; }
        public DateTime FechaUnion { get; set; }
        public int FamiliaId { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class InvitarMiembroDto
    {
        public int FamiliaId { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public RolFamilia Rol { get; set; } = RolFamilia.Miembro;
        public string AliasFamiliar { get; set; }
    }

    public class CambiarRolDto
    {
        public int MiembroId { get; set; }
        public RolFamilia NuevoRol { get; set; }
    }

    public class ReporteFinancieroDto
    {
        public int FamiliaId { get; set; }
        public string NombreFamilia { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public decimal TotalIngresosDOP { get; set; }
        public decimal TotalEgresosDOP { get; set; }
        public decimal BalanceNetoDOP => TotalIngresosDOP - TotalEgresosDOP;
        public List<FuenteIngresoDetalleDto> DesglosePorFuente { get; set; } = new List<FuenteIngresoDetalleDto>();
        public List<CategoriaGastoDto> DesglosePorCategoria { get; set; } = new List<CategoriaGastoDto>();
        public List<TransaccionDetalleDto> Transacciones { get; set; } = new List<TransaccionDetalleDto>();
    }

    public class DashboardResumenDto
    {
        public int FamiliaId { get; set; }
        public string NombreFamilia { get; set; }
        public decimal TasaCambioActual { get; set; }
        public decimal TotalIngresosMesDOP { get; set; }
        public decimal TotalEgresosMesDOP { get; set; }
        public decimal BalanceNetoMesDOP => TotalIngresosMesDOP - TotalEgresosMesDOP;
        public decimal SaldoTotalCuentasDOP { get; set; }
        public decimal SaldoTotalCuentasUSD { get; set; }
        public List<TransaccionDetalleDto> TransaccionesRecientes { get; set; } = new List<TransaccionDetalleDto>();
        public List<FuenteIngresoDetalleDto> FuentesIngreso { get; set; } = new List<FuenteIngresoDetalleDto>();
        public List<CuentaDetalleDto> Cuentas { get; set; } = new List<CuentaDetalleDto>();
        public List<CategoriaGastoDto> GastosPorCategoria { get; set; } = new List<CategoriaGastoDto>();
        public List<FlujoMensualDto> HistoricoMensual { get; set; } = new List<FlujoMensualDto>();
    }

    public class CategoriaGastoDto
    {
        public string Categoria { get; set; }
        public string Color { get; set; }
        public string Icono { get; set; }
        public decimal TotalDOP { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class FlujoMensualDto
    {
        public string MesNombre { get; set; }
        public int Mes { get; set; }
        public int Anio { get; set; }
        public decimal IngresosDOP { get; set; }
        public decimal EgresosDOP { get; set; }
        public decimal NetoDOP => IngresosDOP - EgresosDOP;
    }

    public class AuditLogDetalleDto
    {
        public int Id { get; set; }
        public string Entidad { get; set; }
        public string Accion { get; set; }
        public string RegistroId { get; set; }
        public string ValoresAnterioresJson { get; set; }
        public string ValoresNuevosJson { get; set; }
        public string UsuarioId { get; set; }
        public string NombreUsuario { get; set; }
        public string DireccionIP { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Detalles { get; set; }
    }

    public class FamiliaDetalleDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string CodigoInvitacion { get; set; }
        public decimal TasaCambioActual { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int TotalMiembros { get; set; }
        public int TotalCuentas { get; set; }
        public int TotalFuentes { get; set; }
    }

    public class ActualizarFamiliaDto
    {
        public int FamiliaId { get; set; }
        public string Nombre { get; set; }
    }
}
