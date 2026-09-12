using AutoMapper;
using FHouse.Core.Entities;
using FHouse.Services.DTOs;

namespace FHouse.Services.Mapping
{
    public class FHouseMappingProfile : Profile
    {
        public FHouseMappingProfile()
        {
            CreateMap<Transaccion, TransaccionDetalleDto>()
                .ForMember(d => d.NombreFuenteIngreso, o => o.MapFrom(s => s.FuenteIngreso != null ? s.FuenteIngreso.Nombre : null))
                .ForMember(d => d.ColorFuenteIngreso, o => o.MapFrom(s => s.FuenteIngreso != null ? s.FuenteIngreso.ColorIdentificador : null))
                .ForMember(d => d.NombreCuentaOrigen, o => o.MapFrom(s => s.CuentaOrigen != null ? s.CuentaOrigen.Nombre : null))
                .ForMember(d => d.NombreCuentaDestino, o => o.MapFrom(s => s.CuentaDestino != null ? s.CuentaDestino.Nombre : null))
                .ForMember(d => d.NombreCategoria, o => o.MapFrom(s => s.Categoria != null ? s.Categoria.Nombre : null))
                .ForMember(d => d.IconoCategoria, o => o.MapFrom(s => s.Categoria != null ? s.Categoria.Icono : null))
                .ForMember(d => d.ColorCategoria, o => o.MapFrom(s => s.Categoria != null ? s.Categoria.Color : null));

            CreateMap<CrearTransaccionDto, Transaccion>();

            CreateMap<FuenteIngreso, FuenteIngresoDetalleDto>()
                .ForMember(d => d.CantidadCuentas, o => o.MapFrom(s => s.CuentasAsociadas != null ? s.CuentasAsociadas.Count : 0))
                .ForMember(d => d.CantidadTransacciones, o => o.MapFrom(s => s.Transacciones != null ? s.Transacciones.Count : 0));

            CreateMap<CrearFuenteIngresoDto, FuenteIngreso>();

            CreateMap<Cuenta, CuentaDetalleDto>()
                .ForMember(d => d.NombreFuenteIngreso, o => o.MapFrom(s => s.FuenteIngreso != null ? s.FuenteIngreso.Nombre : null));

            CreateMap<CrearCuentaDto, Cuenta>()
                .ForMember(d => d.SaldoActual, o => o.MapFrom(s => s.SaldoInicial));

            CreateMap<Categoria, CategoriaDetalleDto>();
            CreateMap<CrearCategoriaDto, Categoria>();

            CreateMap<Presupuesto, PresupuestoDetalleDto>()
                .ForMember(d => d.NombreCategoria, o => o.MapFrom(s => s.Categoria != null ? s.Categoria.Nombre : null))
                .ForMember(d => d.NombreFuenteIngreso, o => o.MapFrom(s => s.FuenteIngreso != null ? s.FuenteIngreso.Nombre : null));

            CreateMap<CrearPresupuestoDto, Presupuesto>();

            CreateMap<AuditLog, AuditLogDetalleDto>();
        }
    }
}
