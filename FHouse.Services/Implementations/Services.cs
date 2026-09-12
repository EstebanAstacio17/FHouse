using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using AutoMapper;
using FluentValidation;
using FHouse.Core.Common;
using FHouse.Core.Entities;
using FHouse.Core.Enums;
using FHouse.Core.Interfaces.Repositories;
using FHouse.Infrastructure.Data;
using FHouse.Services.Contracts;
using FHouse.Services.DTOs;

namespace FHouse.Services.Implementations
{
    public class TransaccionService : ITransaccionService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IValidator<CrearTransaccionDto> _validator;

        public TransaccionService(IUnitOfWork uow, IMapper mapper, IValidator<CrearTransaccionDto> validator)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<ResultadoOperacion<TransaccionDetalleDto>> RegistrarTransaccionAsync(CrearTransaccionDto dto, string usuarioId, string nombreUsuario)
        {
            var validacion = await _validator.ValidateAsync(dto);
            if (!validacion.IsValid)
            {
                return ResultadoOperacion<TransaccionDetalleDto>.Falla(validacion.Errors.Select(e => e.ErrorMessage));
            }

            try
            {
                return await _uow.EjecutarEnTransaccionAsync(async () =>
                {
                    var familia = await _uow.Familias.ObtenerPorIdAsync(dto.FamiliaId);
                    if (familia == null)
                    {
                        return ResultadoOperacion<TransaccionDetalleDto>.Falla("Familia no encontrada.");
                    }

                    decimal tasa = familia.TasaCambioActual > 0 ? familia.TasaCambioActual : 59.50m;
                    decimal montoDop = dto.Moneda == Moneda.USD ? dto.Monto * tasa : dto.Monto;

                    Cuenta cuentaOrigen = null;
                    if (dto.CuentaOrigenId.HasValue)
                    {
                        cuentaOrigen = await _uow.Cuentas.ObtenerPorIdAsync(dto.CuentaOrigenId.Value);
                        if (cuentaOrigen == null)
                            return ResultadoOperacion<TransaccionDetalleDto>.Falla("La cuenta seleccionada no existe.");

                        if (dto.Tipo == TipoTransaccion.Egreso)
                        {
                            if (cuentaOrigen.SaldoActual < dto.Monto)
                            {
                                return ResultadoOperacion<TransaccionDetalleDto>.Falla($"Saldo insuficiente en la cuenta '{cuentaOrigen.Nombre}'. Saldo actual: {cuentaOrigen.SaldoActual:N2} {cuentaOrigen.Moneda}.");
                            }
                            cuentaOrigen.SaldoActual -= dto.Monto;
                        }
                        else if (dto.Tipo == TipoTransaccion.Ingreso)
                        {
                            cuentaOrigen.SaldoActual += dto.Monto;
                        }
                        _uow.Cuentas.Actualizar(cuentaOrigen);
                    }

                    var transaccion = _mapper.Map<Transaccion>(dto);
                    transaccion.UsuarioRegistradorId = usuarioId;
                    transaccion.NombreUsuarioRegistrador = nombreUsuario ?? "Usuario";
                    transaccion.TasaCambioAplicada = dto.Moneda == Moneda.USD ? tasa : 1.0m;
                    transaccion.MontoEnDOP = montoDop;
                    transaccion.FechaCreacion = DateTime.UtcNow;

                    await _uow.Transacciones.AgregarAsync(transaccion);

                    // Registro inmutable de auditoría
                    var log = new AuditLog
                    {
                        FamiliaId = dto.FamiliaId,
                        Entidad = "Transaccion",
                        Accion = dto.Tipo == TipoTransaccion.Ingreso ? "INGRESO" : "EGRESO",
                        UsuarioId = usuarioId,
                        NombreUsuario = nombreUsuario ?? "Usuario",
                        Detalles = $"{dto.Concepto} por {dto.Monto:N2} {dto.Moneda} ({(cuentaOrigen != null ? cuentaOrigen.Nombre : "Sin cuenta")})",
                        FechaCreacion = DateTime.UtcNow
                    };
                    if (_uow.AuditLogs != null)
                    {
                        await _uow.AuditLogs.AgregarAsync(log);
                    }

                    await _uow.GuardarCambiosAsync();

                    var resultadoDto = _mapper.Map<TransaccionDetalleDto>(transaccion);
                    if (cuentaOrigen != null)
                        resultadoDto.NombreCuentaOrigen = cuentaOrigen.Nombre;

                    return ResultadoOperacion<TransaccionDetalleDto>.Ok(resultadoDto, "Transacción registrada exitosamente.");
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return ResultadoOperacion<TransaccionDetalleDto>.Falla("Conflicto de concurrencia: el saldo o registro fue modificado simultáneamente por otro usuario. Por favor, intente de nuevo.");
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<TransaccionDetalleDto>.Falla("Error al procesar la transacción: " + ex.Message);
            }
        }

        public async Task<ResultadoOperacion<TransaccionDetalleDto>> ObtenerPorIdAsync(int id)
        {
            var t = await _uow.Transacciones.ObtenerPorIdAsync(id);
            if (t == null)
                return ResultadoOperacion<TransaccionDetalleDto>.Falla("Transacción no encontrada.");

            var dto = _mapper.Map<TransaccionDetalleDto>(t);
            return ResultadoOperacion<TransaccionDetalleDto>.Ok(dto);
        }

        public async Task<ResultadoOperacion<bool>> EditarTransaccionAsync(EditarTransaccionDto dto, string usuarioId)
        {
            var transaccion = await _uow.Transacciones.ObtenerPorIdAsync(dto.Id);
            if (transaccion == null)
                return ResultadoOperacion<bool>.Falla("Transacción no encontrada.");

            transaccion.Concepto = dto.Concepto;
            transaccion.FuenteIngresoId = dto.FuenteIngresoId;
            transaccion.CategoriaId = dto.CategoriaId;
            transaccion.Comentario = dto.Comentario;
            transaccion.FechaModificacion = DateTime.UtcNow;

            _uow.Transacciones.Actualizar(transaccion);
            await _uow.GuardarCambiosAsync();

            return ResultadoOperacion<bool>.Ok(true, "Transacción actualizada correctamente.");
        }

        public async Task<ResultadoOperacion<IEnumerable<TransaccionDetalleDto>>> ObtenerTransaccionesFamiliaAsync(int familiaId, int? limit = null)
        {
            var transacciones = await _uow.Transacciones.ObtenerPorFamiliaAsync(familiaId, limit);
            var dtos = _mapper.Map<IEnumerable<TransaccionDetalleDto>>(transacciones);
            return ResultadoOperacion<IEnumerable<TransaccionDetalleDto>>.Ok(dtos);
        }

        public async Task<ResultadoOperacion<IEnumerable<TransaccionDetalleDto>>> ObtenerPorFuenteAsync(int fuenteId, int? limit = null)
        {
            var transacciones = await _uow.Transacciones.ObtenerPorFuenteAsync(fuenteId, limit);
            var dtos = _mapper.Map<IEnumerable<TransaccionDetalleDto>>(transacciones);
            return ResultadoOperacion<IEnumerable<TransaccionDetalleDto>>.Ok(dtos);
        }

        public async Task<ResultadoOperacion<IEnumerable<TransaccionDetalleDto>>> ObtenerFiltradasAsync(FiltroTransaccionDto filtro)
        {
            var transacciones = await _uow.Transacciones.ObtenerFiltradasAsync(
                filtro.FamiliaId,
                filtro.Desde,
                filtro.Hasta,
                filtro.FuenteIngresoId,
                filtro.CuentaId,
                filtro.CategoriaId,
                filtro.Tipo,
                filtro.UsuarioId);

            var dtos = _mapper.Map<IEnumerable<TransaccionDetalleDto>>(transacciones);
            return ResultadoOperacion<IEnumerable<TransaccionDetalleDto>>.Ok(dtos);
        }

        public async Task<ResultadoOperacion<bool>> AnularTransaccionAsync(int transaccionId, string usuarioId, int? familiaId = null)
        {
            try
            {
                return await _uow.EjecutarEnTransaccionAsync(async () =>
                {
                    var transaccion = await _uow.Transacciones.ObtenerPorIdAsync(transaccionId);
                    if (transaccion == null || !transaccion.Activo)
                        return ResultadoOperacion<bool>.Falla("Transacción no encontrada o ya se encuentra anulada.");

                    // Tenant isolation check
                    if (familiaId.HasValue && transaccion.FamiliaId != familiaId.Value)
                        return ResultadoOperacion<bool>.Falla("No tiene permisos para anular esta transacción.");

                    if (transaccion.CuentaOrigenId.HasValue)
                    {
                        var cuenta = await _uow.Cuentas.ObtenerPorIdAsync(transaccion.CuentaOrigenId.Value);
                        if (cuenta != null)
                        {
                            if (transaccion.Tipo == TipoTransaccion.Egreso)
                                cuenta.SaldoActual += transaccion.Monto;
                            else if (transaccion.Tipo == TipoTransaccion.Ingreso)
                                cuenta.SaldoActual -= transaccion.Monto;

                            _uow.Cuentas.Actualizar(cuenta);
                        }
                    }

                    // Reverse destination account if it was a transfer
                    if (transaccion.CuentaDestinoId.HasValue)
                    {
                        var cuentaDestino = await _uow.Cuentas.ObtenerPorIdAsync(transaccion.CuentaDestinoId.Value);
                        if (cuentaDestino != null)
                        {
                            cuentaDestino.SaldoActual -= transaccion.Monto;
                            _uow.Cuentas.Actualizar(cuentaDestino);
                        }
                    }

                    _uow.Transacciones.EliminarLogico(transaccion);

                    var log = new AuditLog
                    {
                        FamiliaId = transaccion.FamiliaId,
                        Entidad = "Transaccion",
                        Accion = "ANULAR",
                        RegistroId = transaccion.Id.ToString(),
                        UsuarioId = usuarioId,
                        NombreUsuario = "Usuario",
                        Detalles = $"Anulación de movimiento #{transaccion.Id}: {transaccion.Concepto} ({transaccion.Monto:N2} {transaccion.Moneda})",
                        FechaCreacion = DateTime.UtcNow
                    };
                    if (_uow.AuditLogs != null)
                    {
                        await _uow.AuditLogs.AgregarAsync(log);
                    }

                    await _uow.GuardarCambiosAsync();

                    return ResultadoOperacion<bool>.Ok(true, "Transacción anulada correctamente.");
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return ResultadoOperacion<bool>.Falla("Conflicto de concurrencia: la cuenta o transacción fue modificada simultáneamente por otro usuario. Por favor, intente de nuevo.");
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.Falla("Error al anular la transacción: " + ex.Message);
            }
        }
    }

    public class FuenteIngresoService : IFuenteIngresoService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IValidator<CrearFuenteIngresoDto> _validator;

        public FuenteIngresoService(IUnitOfWork uow, IMapper mapper, IValidator<CrearFuenteIngresoDto> validator)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<ResultadoOperacion<FuenteIngresoDetalleDto>> CrearFuenteAsync(CrearFuenteIngresoDto dto, string usuarioId)
        {
            var validacion = await _validator.ValidateAsync(dto);
            if (!validacion.IsValid)
                return ResultadoOperacion<FuenteIngresoDetalleDto>.Falla(validacion.Errors.Select(e => e.ErrorMessage));

            var fuente = _mapper.Map<FuenteIngreso>(dto);
            fuente.UsuarioCreadorId = usuarioId;
            fuente.FechaCreacion = DateTime.UtcNow;

            await _uow.FuentesIngreso.AgregarAsync(fuente);
            await _uow.GuardarCambiosAsync();

            var resultadoDto = _mapper.Map<FuenteIngresoDetalleDto>(fuente);
            return ResultadoOperacion<FuenteIngresoDetalleDto>.Ok(resultadoDto, "Fuente de ingreso creada exitosamente.");
        }

        public async Task<ResultadoOperacion<IEnumerable<FuenteIngresoDetalleDto>>> ObtenerPorFamiliaAsync(int familiaId)
        {
            var fuentes = (await _uow.FuentesIngreso.ObtenerPorFamiliaAsync(familiaId)).ToList();
            if (!fuentes.Any())
            {
                return ResultadoOperacion<IEnumerable<FuenteIngresoDetalleDto>>.Ok(Enumerable.Empty<FuenteIngresoDetalleDto>());
            }

            // Single query optimization: Fetch all transactions for the family in 1 query
            var transaccionesFamilia = (await _uow.Transacciones.ObtenerPorFamiliaAsync(familiaId))
                .Where(t => t.FuenteIngresoId.HasValue)
                .GroupBy(t => t.FuenteIngresoId.Value)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Ingresos = g.Where(t => t.Tipo == TipoTransaccion.Ingreso).Sum(t => t.MontoEnDOP),
                        Egresos = g.Where(t => t.Tipo == TipoTransaccion.Egreso).Sum(t => t.MontoEnDOP),
                        Cantidad = g.Count()
                    }
                );

            var dtos = new List<FuenteIngresoDetalleDto>();
            foreach (var f in fuentes)
            {
                var dto = _mapper.Map<FuenteIngresoDetalleDto>(f);
                if (transaccionesFamilia.TryGetValue(f.Id, out var stats))
                {
                    dto.TotalIngresosDOP = stats.Ingresos;
                    dto.TotalEgresosDOP = stats.Egresos;
                    dto.CantidadTransacciones = stats.Cantidad;
                }
                else
                {
                    dto.TotalIngresosDOP = 0m;
                    dto.TotalEgresosDOP = 0m;
                    dto.CantidadTransacciones = 0;
                }
                dtos.Add(dto);
            }

            return ResultadoOperacion<IEnumerable<FuenteIngresoDetalleDto>>.Ok(dtos);
        }

        public async Task<ResultadoOperacion<FuenteIngresoDetalleDto>> ObtenerPorIdAsync(int id, int? familiaId = null)
        {
            var fuente = await _uow.FuentesIngreso.ObtenerConDetallesAsync(id);
            if (fuente == null)
                return ResultadoOperacion<FuenteIngresoDetalleDto>.Falla("Fuente de ingreso no encontrada.");

            if (familiaId.HasValue && fuente.FamiliaId != familiaId.Value)
                return ResultadoOperacion<FuenteIngresoDetalleDto>.Falla("No tiene permisos para acceder a esta fuente de ingreso.");

            var dto = _mapper.Map<FuenteIngresoDetalleDto>(fuente);
            var transacciones = await _uow.Transacciones.ObtenerPorFuenteAsync(id);
            dto.TotalIngresosDOP = transacciones.Where(t => t.Tipo == TipoTransaccion.Ingreso).Sum(t => t.MontoEnDOP);
            dto.TotalEgresosDOP = transacciones.Where(t => t.Tipo == TipoTransaccion.Egreso).Sum(t => t.MontoEnDOP);
            dto.CantidadTransacciones = transacciones.Count();

            return ResultadoOperacion<FuenteIngresoDetalleDto>.Ok(dto);
        }

        public async Task<ResultadoOperacion<bool>> ActualizarFuenteAsync(int id, CrearFuenteIngresoDto dto, int? familiaId = null)
        {
            var fuente = await _uow.FuentesIngreso.ObtenerPorIdAsync(id);
            if (fuente == null)
                return ResultadoOperacion<bool>.Falla("Fuente de ingreso no encontrada.");

            if (familiaId.HasValue && fuente.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para modificar esta fuente de ingreso.");

            fuente.Nombre = dto.Nombre;
            fuente.Descripcion = dto.Descripcion;
            fuente.Tipo = dto.Tipo;
            fuente.ColorIdentificador = dto.ColorIdentificador;
            fuente.FechaModificacion = DateTime.UtcNow;

            _uow.FuentesIngreso.Actualizar(fuente);

            var log = new AuditLog
            {
                FamiliaId = fuente.FamiliaId,
                Entidad = "FuenteIngreso",
                Accion = "UPDATE",
                RegistroId = id.ToString(),
                UsuarioId = fuente.UsuarioCreadorId,
                NombreUsuario = "Usuario",
                Detalles = $"Actualización de fuente de ingreso '{fuente.Nombre}'",
                FechaCreacion = DateTime.UtcNow
            };
            if (_uow.AuditLogs != null)
            {
                await _uow.AuditLogs.AgregarAsync(log);
            }

            await _uow.GuardarCambiosAsync();

            return ResultadoOperacion<bool>.Ok(true, "Fuente de ingreso actualizada.");
        }

        public async Task<ResultadoOperacion<bool>> EliminarFuenteAsync(int id, string usuarioId, int? familiaId = null)
        {
            var fuente = await _uow.FuentesIngreso.ObtenerPorIdAsync(id);
            if (fuente == null)
                return ResultadoOperacion<bool>.Falla("Fuente de ingreso no encontrada.");

            if (familiaId.HasValue && fuente.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para eliminar esta fuente de ingreso.");

            // Verificar si posee movimientos/transacciones ACTIVAS
            bool tieneTransaccionesActivas = await _uow.Transacciones.ExisteAsync(t => t.FuenteIngresoId == id && t.Activo);

            if (tieneTransaccionesActivas)
            {
                // Posee transacciones financieras activas: se inhabilita para proteger el historial contable
                fuente.Activo = false;
                fuente.FechaModificacion = DateTime.UtcNow;

                _uow.FuentesIngreso.Actualizar(fuente);

                var log = new AuditLog
                {
                    FamiliaId = fuente.FamiliaId,
                    Entidad = "FuenteIngreso",
                    Accion = "DISABLE",
                    RegistroId = id.ToString(),
                    UsuarioId = usuarioId,
                    NombreUsuario = "Usuario",
                    Detalles = $"Inhabilitación de la fuente de ingreso '{fuente.Nombre}' (posee transacciones activas)",
                    FechaCreacion = DateTime.UtcNow
                };
                if (_uow.AuditLogs != null)
                {
                    await _uow.AuditLogs.AgregarAsync(log);
                }

                await _uow.GuardarCambiosAsync();
                return ResultadoOperacion<bool>.Ok(true, $"La fuente '{fuente.Nombre}' posee movimientos activos. Ha sido inhabilitada para proteger el historial financiero.");
            }
            else
            {
                // NO posee movimientos activos (fueron eliminados o no tiene):
                // 1. Desvincular cuentas bancarias (las cuentas siguen existiendo normalmente para el hogar)
                var cuentasAsociadas = await _uow.Cuentas.BuscarAsync(c => c.FuenteIngresoId == id);
                foreach (var c in cuentasAsociadas)
                {
                    c.FuenteIngresoId = null;
                    _uow.Cuentas.Actualizar(c);
                }

                // 2. Desvincular presupuestos asociados
                var presupuestosAsociados = await _uow.Presupuestos.BuscarAsync(p => p.FuenteIngresoId == id);
                foreach (var p in presupuestosAsociados)
                {
                    p.FuenteIngresoId = null;
                    _uow.Presupuestos.Actualizar(p);
                }

                // 3. Desvincular transacciones anuladas/inactivas previas para integridad de FK
                var transaccionesPrevias = await _uow.Transacciones.BuscarAsync(t => t.FuenteIngresoId == id);
                foreach (var t in transaccionesPrevias)
                {
                    t.FuenteIngresoId = null;
                    _uow.Transacciones.Actualizar(t);
                }

                if (cuentasAsociadas.Any() || presupuestosAsociados.Any() || transaccionesPrevias.Any())
                {
                    await _uow.GuardarCambiosAsync();
                }

                _uow.FuentesIngreso.Eliminar(fuente);

                var log = new AuditLog
                {
                    FamiliaId = fuente.FamiliaId,
                    Entidad = "FuenteIngreso",
                    Accion = "DELETE",
                    RegistroId = id.ToString(),
                    UsuarioId = usuarioId,
                    NombreUsuario = "Usuario",
                    Detalles = $"Eliminación definitiva de la fuente de ingreso '{fuente.Nombre}' (sin movimientos activos)",
                    FechaCreacion = DateTime.UtcNow
                };
                if (_uow.AuditLogs != null)
                {
                    await _uow.AuditLogs.AgregarAsync(log);
                }

                await _uow.GuardarCambiosAsync();
                return ResultadoOperacion<bool>.Ok(true, $"La fuente '{fuente.Nombre}' ha sido eliminada correctamente.");
            }
        }

        public async Task<ResultadoOperacion<bool>> InhabilitarFuenteAsync(int id, string usuarioId, int? familiaId = null)
        {
            var fuente = await _uow.FuentesIngreso.ObtenerPorIdAsync(id);
            if (fuente == null)
                return ResultadoOperacion<bool>.Falla("Fuente de ingreso no encontrada.");

            if (familiaId.HasValue && fuente.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para inhabilitar esta fuente de ingreso.");

            fuente.Activo = false;
            fuente.FechaModificacion = DateTime.UtcNow;
            _uow.FuentesIngreso.Actualizar(fuente);

            var log = new AuditLog
            {
                FamiliaId = fuente.FamiliaId,
                Entidad = "FuenteIngreso",
                Accion = "DISABLE",
                RegistroId = id.ToString(),
                UsuarioId = usuarioId,
                NombreUsuario = "Usuario",
                Detalles = $"Inhabilitación voluntaria de la fuente de ingreso '{fuente.Nombre}'",
                FechaCreacion = DateTime.UtcNow
            };
            if (_uow.AuditLogs != null)
            {
                await _uow.AuditLogs.AgregarAsync(log);
            }

            await _uow.GuardarCambiosAsync();
            return ResultadoOperacion<bool>.Ok(true, $"La fuente '{fuente.Nombre}' ha sido inhabilitada.");
        }

        public async Task<ResultadoOperacion<bool>> HabilitarFuenteAsync(int id, string usuarioId, int? familiaId = null)
        {
            var fuente = await _uow.FuentesIngreso.ObtenerPorIdAsync(id);
            if (fuente == null)
                return ResultadoOperacion<bool>.Falla("Fuente de ingreso no encontrada.");

            if (familiaId.HasValue && fuente.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para habilitar esta fuente de ingreso.");

            fuente.Activo = true;
            fuente.FechaModificacion = DateTime.UtcNow;
            _uow.FuentesIngreso.Actualizar(fuente);

            var log = new AuditLog
            {
                FamiliaId = fuente.FamiliaId,
                Entidad = "FuenteIngreso",
                Accion = "ENABLE",
                RegistroId = id.ToString(),
                UsuarioId = usuarioId,
                NombreUsuario = "Usuario",
                Detalles = $"Reactivación / Habilitación de la fuente de ingreso '{fuente.Nombre}'",
                FechaCreacion = DateTime.UtcNow
            };
            if (_uow.AuditLogs != null)
            {
                await _uow.AuditLogs.AgregarAsync(log);
            }

            await _uow.GuardarCambiosAsync();
            return ResultadoOperacion<bool>.Ok(true, $"La fuente '{fuente.Nombre}' ha sido reactivada exitosamente.");
        }
    }

    public class CuentaService : ICuentaService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IValidator<CrearCuentaDto> _validator;
        private readonly IValidator<TransferenciaCuentaDto> _transferenciaValidator;

        public CuentaService(IUnitOfWork uow, IMapper mapper, IValidator<CrearCuentaDto> validator, IValidator<TransferenciaCuentaDto> transferenciaValidator)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _transferenciaValidator = transferenciaValidator ?? throw new ArgumentNullException(nameof(transferenciaValidator));
        }

        public async Task<ResultadoOperacion<CuentaDetalleDto>> CrearCuentaAsync(CrearCuentaDto dto, string usuarioId)
        {
            var validacion = await _validator.ValidateAsync(dto);
            if (!validacion.IsValid)
                return ResultadoOperacion<CuentaDetalleDto>.Falla(validacion.Errors.Select(e => e.ErrorMessage));

            var cuenta = _mapper.Map<Cuenta>(dto);
            cuenta.UsuarioResponsableId = usuarioId;
            cuenta.FechaCreacion = DateTime.UtcNow;

            await _uow.Cuentas.AgregarAsync(cuenta);
            await _uow.GuardarCambiosAsync();

            var resultadoDto = _mapper.Map<CuentaDetalleDto>(cuenta);
            return ResultadoOperacion<CuentaDetalleDto>.Ok(resultadoDto, "Cuenta creada exitosamente.");
        }

        public async Task<ResultadoOperacion<bool>> ActualizarCuentaAsync(int id, CrearCuentaDto dto, string usuarioId, int? familiaId = null)
        {
            var validacion = await _validator.ValidateAsync(dto);
            if (!validacion.IsValid)
                return ResultadoOperacion<bool>.Falla(validacion.Errors.Select(e => e.ErrorMessage));

            var cuenta = await _uow.Cuentas.ObtenerPorIdAsync(id);
            if (cuenta == null)
                return ResultadoOperacion<bool>.Falla("Cuenta no encontrada.");

            if (familiaId.HasValue && cuenta.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para modificar esta cuenta.");

            cuenta.Nombre = dto.Nombre;
            cuenta.InstitucionFinanciera = dto.InstitucionFinanciera;
            cuenta.NumeroCuenta = dto.NumeroCuenta;
            cuenta.Tipo = dto.Tipo;
            cuenta.Moneda = dto.Moneda;
            cuenta.FuenteIngresoId = dto.FuenteIngresoId;
            cuenta.FechaModificacion = DateTime.UtcNow;

            _uow.Cuentas.Actualizar(cuenta);
            await _uow.GuardarCambiosAsync();

            return ResultadoOperacion<bool>.Ok(true, "Cuenta actualizada exitosamente.");
        }

        public async Task<ResultadoOperacion<bool>> EliminarCuentaAsync(int id, string usuarioId, int? familiaId = null)
        {
            var cuenta = await _uow.Cuentas.ObtenerPorIdAsync(id);
            if (cuenta == null)
                return ResultadoOperacion<bool>.Falla("Cuenta no encontrada.");

            if (familiaId.HasValue && cuenta.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para eliminar esta cuenta.");

            // Verificar si posee movimientos/transacciones ACTIVAS como origen o destino
            bool tieneTransaccionesActivas = await _uow.Transacciones.ExisteAsync(t => 
                (t.CuentaOrigenId == id || t.CuentaDestinoId == id) && t.Activo);

            if (tieneTransaccionesActivas)
            {
                // Posee movimientos activos: se inhabilita para proteger el historial contable
                cuenta.Activo = false;
                cuenta.FechaModificacion = DateTime.UtcNow;

                _uow.Cuentas.Actualizar(cuenta);

                var log = new AuditLog
                {
                    FamiliaId = cuenta.FamiliaId,
                    Entidad = "Cuenta",
                    Accion = "DISABLE",
                    RegistroId = id.ToString(),
                    UsuarioId = usuarioId,
                    NombreUsuario = "Usuario",
                    Detalles = $"Inhabilitación de la cuenta '{cuenta.Nombre}' (posee movimientos activos)",
                    FechaCreacion = DateTime.UtcNow
                };
                if (_uow.AuditLogs != null)
                {
                    await _uow.AuditLogs.AgregarAsync(log);
                }

                await _uow.GuardarCambiosAsync();
                return ResultadoOperacion<bool>.Ok(true, $"La cuenta '{cuenta.Nombre}' posee movimientos activos. Ha sido inhabilitada para proteger el historial financiero.");
            }
            else
            {
                // NO posee movimientos activos (fueron eliminados/anulados o nunca tuvo):
                // 1. Desvincular de transacciones previas anuladas/inactivas para mantener integridad relacional
                var transaccionesOrigen = await _uow.Transacciones.BuscarAsync(t => t.CuentaOrigenId == id);
                foreach (var t in transaccionesOrigen)
                {
                    t.CuentaOrigenId = null;
                    _uow.Transacciones.Actualizar(t);
                }

                var transaccionesDestino = await _uow.Transacciones.BuscarAsync(t => t.CuentaDestinoId == id);
                foreach (var t in transaccionesDestino)
                {
                    t.CuentaDestinoId = null;
                    _uow.Transacciones.Actualizar(t);
                }

                if (transaccionesOrigen.Any() || transaccionesDestino.Any())
                {
                    await _uow.GuardarCambiosAsync();
                }

                _uow.Cuentas.Eliminar(cuenta);

                var log = new AuditLog
                {
                    FamiliaId = cuenta.FamiliaId,
                    Entidad = "Cuenta",
                    Accion = "DELETE",
                    RegistroId = id.ToString(),
                    UsuarioId = usuarioId,
                    NombreUsuario = "Usuario",
                    Detalles = $"Eliminación permanente de la cuenta '{cuenta.Nombre}'",
                    FechaCreacion = DateTime.UtcNow
                };
                if (_uow.AuditLogs != null)
                {
                    await _uow.AuditLogs.AgregarAsync(log);
                }

                await _uow.GuardarCambiosAsync();
                return ResultadoOperacion<bool>.Ok(true, $"La cuenta '{cuenta.Nombre}' ha sido eliminada correctamente.");
            }
        }

        public async Task<ResultadoOperacion<bool>> HabilitarCuentaAsync(int id, string usuarioId, int? familiaId = null)
        {
            var cuenta = await _uow.Cuentas.ObtenerPorIdAsync(id);
            if (cuenta == null)
                return ResultadoOperacion<bool>.Falla("Cuenta no encontrada.");

            if (familiaId.HasValue && cuenta.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para habilitar esta cuenta.");

            cuenta.Activo = true;
            cuenta.FechaModificacion = DateTime.UtcNow;

            _uow.Cuentas.Actualizar(cuenta);

            var log = new AuditLog
            {
                FamiliaId = cuenta.FamiliaId,
                Entidad = "Cuenta",
                Accion = "ENABLE",
                RegistroId = id.ToString(),
                UsuarioId = usuarioId,
                NombreUsuario = "Usuario",
                Detalles = $"Reactivación de la cuenta '{cuenta.Nombre}'",
                FechaCreacion = DateTime.UtcNow
            };
            if (_uow.AuditLogs != null)
            {
                await _uow.AuditLogs.AgregarAsync(log);
            }

            await _uow.GuardarCambiosAsync();
            return ResultadoOperacion<bool>.Ok(true, $"La cuenta '{cuenta.Nombre}' ha sido reactivada exitosamente.");
        }

        public async Task<ResultadoOperacion<bool>> InhabilitarCuentaAsync(int id, string usuarioId, int? familiaId = null)
        {
            var cuenta = await _uow.Cuentas.ObtenerPorIdAsync(id);
            if (cuenta == null)
                return ResultadoOperacion<bool>.Falla("Cuenta no encontrada.");

            if (familiaId.HasValue && cuenta.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para inhabilitar esta cuenta.");

            cuenta.Activo = false;
            cuenta.FechaModificacion = DateTime.UtcNow;

            _uow.Cuentas.Actualizar(cuenta);

            var log = new AuditLog
            {
                FamiliaId = cuenta.FamiliaId,
                Entidad = "Cuenta",
                Accion = "DISABLE",
                RegistroId = id.ToString(),
                UsuarioId = usuarioId,
                NombreUsuario = "Usuario",
                Detalles = $"Inhabilitación de la cuenta '{cuenta.Nombre}'",
                FechaCreacion = DateTime.UtcNow
            };
            if (_uow.AuditLogs != null)
            {
                await _uow.AuditLogs.AgregarAsync(log);
            }

            await _uow.GuardarCambiosAsync();
            return ResultadoOperacion<bool>.Ok(true, $"La cuenta '{cuenta.Nombre}' ha sido inhabilitada.");
        }

        public async Task<ResultadoOperacion<IEnumerable<CuentaDetalleDto>>> ObtenerPorFamiliaAsync(int familiaId)
        {
            var cuentas = await _uow.Cuentas.ObtenerPorFamiliaAsync(familiaId);
            var dtos = _mapper.Map<IEnumerable<CuentaDetalleDto>>(cuentas);
            return ResultadoOperacion<IEnumerable<CuentaDetalleDto>>.Ok(dtos);
        }

        public async Task<ResultadoOperacion<IEnumerable<CuentaDetalleDto>>> ObtenerPorFuenteAsync(int fuenteId)
        {
            var cuentas = await _uow.Cuentas.ObtenerPorFuenteAsync(fuenteId);
            var dtos = _mapper.Map<IEnumerable<CuentaDetalleDto>>(cuentas);
            return ResultadoOperacion<IEnumerable<CuentaDetalleDto>>.Ok(dtos);
        }

        public async Task<ResultadoOperacion<bool>> RealizarTransferenciaAsync(TransferenciaCuentaDto dto, string usuarioId, string nombreUsuario)
        {
            var validacion = await _transferenciaValidator.ValidateAsync(dto);
            if (!validacion.IsValid)
                return ResultadoOperacion<bool>.Falla(validacion.Errors.Select(e => e.ErrorMessage));

            try
            {
                return await _uow.EjecutarEnTransaccionAsync(async () =>
                {
                    var cuentaOrigen = await _uow.Cuentas.ObtenerPorIdAsync(dto.CuentaOrigenId);
                    var cuentaDestino = await _uow.Cuentas.ObtenerPorIdAsync(dto.CuentaDestinoId);

                    if (cuentaOrigen == null || cuentaDestino == null)
                        return ResultadoOperacion<bool>.Falla("Una o ambas cuentas no existen.");

                    if (cuentaOrigen.SaldoActual < dto.Monto)
                        return ResultadoOperacion<bool>.Falla($"Saldo insuficiente en la cuenta '{cuentaOrigen.Nombre}'. Saldo actual: {cuentaOrigen.SaldoActual:N2}.");

                    var familia = await _uow.Familias.ObtenerPorIdAsync(dto.FamiliaId);
                    decimal tasa = familia != null && familia.TasaCambioActual > 0 ? familia.TasaCambioActual : 60.50m;

                    decimal montoDestino = dto.Monto;
                    if (cuentaOrigen.Moneda != cuentaDestino.Moneda)
                    {
                        if (cuentaOrigen.Moneda == Moneda.DOP && cuentaDestino.Moneda == Moneda.USD)
                        {
                            montoDestino = Math.Round(dto.Monto / tasa, 2);
                        }
                        else if (cuentaOrigen.Moneda == Moneda.USD && cuentaDestino.Moneda == Moneda.DOP)
                        {
                            montoDestino = Math.Round(dto.Monto * tasa, 2);
                        }
                    }

                    cuentaOrigen.SaldoActual -= dto.Monto;
                    cuentaDestino.SaldoActual += montoDestino;

                    _uow.Cuentas.Actualizar(cuentaOrigen);
                    _uow.Cuentas.Actualizar(cuentaDestino);

                    var transaccion = new Transaccion
                    {
                        Concepto = $"Transferencia: {cuentaOrigen.Nombre} -> {cuentaDestino.Nombre}",
                        Monto = dto.Monto,
                        Moneda = cuentaOrigen.Moneda,
                        MontoEnDOP = cuentaOrigen.Moneda == Moneda.USD ? dto.Monto * tasa : dto.Monto,
                        Tipo = TipoTransaccion.Egreso,
                        FechaTransaccion = DateTime.UtcNow,
                        FamiliaId = dto.FamiliaId,
                        CuentaOrigenId = cuentaOrigen.Id,
                        CuentaDestinoId = cuentaDestino.Id,
                        UsuarioRegistradorId = usuarioId,
                        NombreUsuarioRegistrador = nombreUsuario ?? "Usuario",
                        Comentario = dto.Comentario,
                        FechaCreacion = DateTime.UtcNow
                    };

                    await _uow.Transacciones.AgregarAsync(transaccion);

                    var log = new AuditLog
                    {
                        FamiliaId = dto.FamiliaId,
                        Entidad = "Cuenta",
                        Accion = "TRANSFERENCIA",
                        UsuarioId = usuarioId,
                        NombreUsuario = nombreUsuario ?? "Usuario",
                        Detalles = $"Transferencia de {dto.Monto:N2} {cuentaOrigen.Moneda} desde '{cuentaOrigen.Nombre}' hacia '{cuentaDestino.Nombre}' (Recibido: {montoDestino:N2} {cuentaDestino.Moneda})",
                        FechaCreacion = DateTime.UtcNow
                    };
                    if (_uow.AuditLogs != null)
                    {
                        await _uow.AuditLogs.AgregarAsync(log);
                    }

                    await _uow.GuardarCambiosAsync();

                    return ResultadoOperacion<bool>.Ok(true, "Transferencia realizada con éxito.");
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return ResultadoOperacion<bool>.Falla("Conflicto de concurrencia: una de las cuentas fue modificada en paralelo. Por favor, reintente la transferencia.");
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<bool>.Falla("Error al realizar la transferencia: " + ex.Message);
            }
        }
    }

    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public DashboardService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<ResultadoOperacion<DashboardResumenDto>> ObtenerResumenDashboardAsync(int familiaId)
        {
            var familia = await _uow.Familias.ObtenerPorIdAsync(familiaId);
            if (familia == null)
                return ResultadoOperacion<DashboardResumenDto>.Falla("Familia no encontrada.");

            var ahora = DateTime.UtcNow;
            var inicioMes = new DateTime(ahora.Year, ahora.Month, 1);
            var finMes = inicioMes.AddMonths(1).AddTicks(-1);
            var seisMesesAtras = new DateTime(ahora.AddMonths(-5).Year, ahora.AddMonths(-5).Month, 1);

            // Fetch accounts, sources, and transactions in minimal database trips
            var cuentas = (await _uow.Cuentas.ObtenerPorFamiliaAsync(familiaId)).ToList();
            var fuentes = (await _uow.FuentesIngreso.ObtenerPorFamiliaAsync(familiaId)).ToList();
            var todasTransacciones6Meses = (await _uow.Transacciones.ObtenerFiltradasAsync(familiaId, seisMesesAtras, finMes, null, null, null, null, null)).ToList();

            var transaccionesMesActual = todasTransacciones6Meses
                .Where(t => t.FechaTransaccion >= inicioMes && t.FechaTransaccion <= finMes)
                .ToList();

            var totalIngresosMes = transaccionesMesActual
                .Where(t => t.Tipo == TipoTransaccion.Ingreso)
                .Sum(t => t.MontoEnDOP);

            var totalEgresosMes = transaccionesMesActual
                .Where(t => t.Tipo == TipoTransaccion.Egreso)
                .Sum(t => t.MontoEnDOP);

            var transaccionesRecientes = todasTransacciones6Meses
                .OrderByDescending(t => t.FechaTransaccion)
                .Take(10)
                .ToList();

            var resumen = new DashboardResumenDto
            {
                FamiliaId = familiaId,
                NombreFamilia = familia.Nombre,
                TasaCambioActual = familia.TasaCambioActual,
                TotalIngresosMesDOP = totalIngresosMes,
                TotalEgresosMesDOP = totalEgresosMes,
                SaldoTotalCuentasDOP = cuentas.Where(c => c.Moneda == Moneda.DOP).Sum(c => c.SaldoActual),
                SaldoTotalCuentasUSD = cuentas.Where(c => c.Moneda == Moneda.USD).Sum(c => c.SaldoActual),
                TransaccionesRecientes = _mapper.Map<List<TransaccionDetalleDto>>(transaccionesRecientes),
                Cuentas = _mapper.Map<List<CuentaDetalleDto>>(cuentas)
            };

            foreach (var f in fuentes)
            {
                var fDto = _mapper.Map<FuenteIngresoDetalleDto>(f);
                var fTrans = todasTransacciones6Meses.Where(t => t.FuenteIngresoId == f.Id).ToList();
                fDto.TotalIngresosDOP = fTrans.Where(t => t.Tipo == TipoTransaccion.Ingreso).Sum(t => t.MontoEnDOP);
                fDto.TotalEgresosDOP = fTrans.Where(t => t.Tipo == TipoTransaccion.Egreso).Sum(t => t.MontoEnDOP);
                fDto.CantidadTransacciones = fTrans.Count;
                resumen.FuentesIngreso.Add(fDto);
            }

            var egresosMes = transaccionesMesActual.Where(t => t.Tipo == TipoTransaccion.Egreso).ToList();
            var totalGastos = egresosMes.Sum(t => t.MontoEnDOP);

            var categoriasGroup = egresosMes
                .GroupBy(t => t.Categoria != null ? t.Categoria.Nombre : "Sin Categoría")
                .Select(g => new CategoriaGastoDto
                {
                    Categoria = g.Key,
                    TotalDOP = g.Sum(x => x.MontoEnDOP),
                    Color = g.FirstOrDefault()?.Categoria?.Color ?? "#86868B",
                    Icono = g.FirstOrDefault()?.Categoria?.Icono ?? "tag",
                    Porcentaje = totalGastos > 0 ? Math.Round((g.Sum(x => x.MontoEnDOP) / totalGastos) * 100, 1) : 0
                })
                .OrderByDescending(c => c.TotalDOP)
                .ToList();

            resumen.GastosPorCategoria = categoriasGroup;

            for (int i = 5; i >= 0; i--)
            {
                var mesRef = ahora.AddMonths(-i);
                var inicioPeriodo = new DateTime(mesRef.Year, mesRef.Month, 1);
                var finPeriodo = inicioPeriodo.AddMonths(1).AddTicks(-1);

                var transPeriodo = todasTransacciones6Meses
                    .Where(t => t.FechaTransaccion >= inicioPeriodo && t.FechaTransaccion <= finPeriodo)
                    .ToList();

                var ing = transPeriodo.Where(t => t.Tipo == TipoTransaccion.Ingreso).Sum(t => t.MontoEnDOP);
                var egr = transPeriodo.Where(t => t.Tipo == TipoTransaccion.Egreso).Sum(t => t.MontoEnDOP);

                resumen.HistoricoMensual.Add(new FlujoMensualDto
                {
                    Mes = mesRef.Month,
                    Anio = mesRef.Year,
                    MesNombre = mesRef.ToString("MMM yyyy", CultureInfo.CreateSpecificCulture("es-DO")),
                    IngresosDOP = ing,
                    EgresosDOP = egr
                });
            }

            return ResultadoOperacion<DashboardResumenDto>.Ok(resumen);
        }
    }

    public class CategoriaService : ICategoriaService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public CategoriaService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<ResultadoOperacion<CategoriaDetalleDto>> CrearCategoriaAsync(CrearCategoriaDto dto)
        {
            var categoria = _mapper.Map<Categoria>(dto);
            await _uow.Categorias.AgregarAsync(categoria);
            await _uow.GuardarCambiosAsync();
            return ResultadoOperacion<CategoriaDetalleDto>.Ok(_mapper.Map<CategoriaDetalleDto>(categoria));
        }

        public async Task<ResultadoOperacion<IEnumerable<CategoriaDetalleDto>>> ObtenerPorFamiliaAsync(int familiaId)
        {
            var categorias = await _uow.Categorias.ObtenerPorFamiliaAsync(familiaId);
            return ResultadoOperacion<IEnumerable<CategoriaDetalleDto>>.Ok(_mapper.Map<IEnumerable<CategoriaDetalleDto>>(categorias ?? Enumerable.Empty<Categoria>()));
        }

        public async Task<ResultadoOperacion<bool>> EliminarCategoriaAsync(int id, int? familiaId = null)
        {
            var cat = await _uow.Categorias.ObtenerPorIdAsync(id);
            if (cat == null)
                return ResultadoOperacion<bool>.Falla("Categoría no encontrada.");

            if (familiaId.HasValue && cat.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para eliminar esta categoría.");

            bool tieneTransaccionesActivas = await _uow.Transacciones.ExisteAsync(t => t.CategoriaId == id && t.Activo);
            bool tienePresupuestosActivos = await _uow.Presupuestos.ExisteAsync(p => p.CategoriaId == id && p.Activo);

            if (tieneTransaccionesActivas || tienePresupuestosActivos)
            {
                cat.Activo = false;
                cat.FechaModificacion = DateTime.UtcNow;
                _uow.Categorias.Actualizar(cat);
                await _uow.GuardarCambiosAsync();
                return ResultadoOperacion<bool>.Ok(true, $"La categoría '{cat.Nombre}' posee transacciones o presupuestos activos. Ha sido inhabilitada para preservar el historial financiero.");
            }
            else
            {
                var transaccionesPrevias = await _uow.Transacciones.BuscarAsync(t => t.CategoriaId == id);
                foreach (var t in transaccionesPrevias)
                {
                    t.CategoriaId = null;
                    _uow.Transacciones.Actualizar(t);
                }

                var presupuestosPrevios = await _uow.Presupuestos.BuscarAsync(p => p.CategoriaId == id);
                foreach (var p in presupuestosPrevios)
                {
                    p.CategoriaId = null;
                    _uow.Presupuestos.Actualizar(p);
                }

                _uow.Categorias.Eliminar(cat);
                await _uow.GuardarCambiosAsync();
                return ResultadoOperacion<bool>.Ok(true, $"La categoría '{cat.Nombre}' ha sido eliminada correctamente.");
            }
        }
    }

    public class PresupuestoService : IPresupuestoService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IValidator<CrearPresupuestoDto> _validator;

        public PresupuestoService(IUnitOfWork uow, IMapper mapper, IValidator<CrearPresupuestoDto> validator)
        {
            _uow = uow;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<ResultadoOperacion<PresupuestoDetalleDto>> CrearPresupuestoAsync(CrearPresupuestoDto dto)
        {
            var val = await _validator.ValidateAsync(dto);
            if (!val.IsValid)
                return ResultadoOperacion<PresupuestoDetalleDto>.Falla(val.Errors.Select(e => e.ErrorMessage));

            var presupuesto = _mapper.Map<Presupuesto>(dto);
            await _uow.Presupuestos.AgregarAsync(presupuesto);
            await _uow.GuardarCambiosAsync();
            return ResultadoOperacion<PresupuestoDetalleDto>.Ok(_mapper.Map<PresupuestoDetalleDto>(presupuesto));
        }

        public async Task<ResultadoOperacion<IEnumerable<PresupuestoDetalleDto>>> ObtenerPorPeriodoAsync(int familiaId, int mes, int anio)
        {
            var presupuestos = (await _uow.Presupuestos.ObtenerPorFamiliaPeriodoAsync(familiaId, mes, anio)).ToList();
            var dtos = _mapper.Map<List<PresupuestoDetalleDto>>(presupuestos);

            var inicioMes = new DateTime(anio, mes, 1);
            var finMes = inicioMes.AddMonths(1).AddTicks(-1);

            // Single query optimization: Fetch all expense transactions for the period in 1 query
            var transaccionesPeriodo = (await _uow.Transacciones.ObtenerFiltradasAsync(
                familiaId, inicioMes, finMes, null, null, null, TipoTransaccion.Egreso, null)).ToList();

            foreach (var p in dtos)
            {
                var trans = transaccionesPeriodo.Where(t =>
                    (!p.CategoriaId.HasValue || t.CategoriaId == p.CategoriaId.Value) &&
                    (!p.FuenteIngresoId.HasValue || t.FuenteIngresoId == p.FuenteIngresoId.Value));
                p.MontoEjecutadoDOP = trans.Sum(t => t.MontoEnDOP);
            }

            return ResultadoOperacion<IEnumerable<PresupuestoDetalleDto>>.Ok(dtos);
        }
    }

    public class TasaCambioService : ITasaCambioService
    {
        private readonly IUnitOfWork _uow;

        public TasaCambioService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ResultadoOperacion<decimal>> ObtenerTasaActualAsync(int familiaId)
        {
            var familia = await _uow.Familias.ObtenerPorIdAsync(familiaId);
            if (familia == null)
                return ResultadoOperacion<decimal>.Falla("Familia no encontrada.");

            return ResultadoOperacion<decimal>.Ok(familia.TasaCambioActual > 0 ? familia.TasaCambioActual : 59.50m);
        }

        public async Task<ResultadoOperacion<bool>> ActualizarTasaAsync(int familiaId, decimal nuevaTasa, string usuarioId)
        {
            if (nuevaTasa <= 0)
                return ResultadoOperacion<bool>.Falla("La tasa de cambio debe ser mayor a 0.");

            var familia = await _uow.Familias.ObtenerPorIdAsync(familiaId);
            if (familia == null)
                return ResultadoOperacion<bool>.Falla("Familia no encontrada.");

            familia.TasaCambioActual = nuevaTasa;
            familia.FechaModificacion = DateTime.UtcNow;

            var registroTasa = new TasaCambio
            {
                FamiliaId = familiaId,
                TasaDOPporUSD = nuevaTasa,
                FechaVigencia = DateTime.UtcNow,
                UsuarioRegistradorId = usuarioId
            };

            _uow.Familias.Actualizar(familia);
            await _uow.TasasCambio.AgregarAsync(registroTasa);

            var log = new AuditLog
            {
                FamiliaId = familiaId,
                Entidad = "TasaCambio",
                Accion = "UPDATE_TASA",
                UsuarioId = usuarioId,
                NombreUsuario = "Administrador",
                Detalles = $"Nueva tasa fijada: RD$ {nuevaTasa:N2} por 1 USD",
                FechaCreacion = DateTime.UtcNow
            };
            if (_uow.AuditLogs != null)
            {
                await _uow.AuditLogs.AgregarAsync(log);
            }

            await _uow.GuardarCambiosAsync();

            return ResultadoOperacion<bool>.Ok(true, "Tasa de cambio actualizada.");
        }
    }

    public class AuditService : IAuditService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public AuditService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<ResultadoOperacion<IEnumerable<AuditLogDetalleDto>>> ObtenerLogsFamiliaAsync(int familiaId, int limit = 100)
        {
            var logs = await _uow.AuditLogs.ObtenerPorFamiliaAsync(familiaId, limit);
            var dtos = _mapper.Map<IEnumerable<AuditLogDetalleDto>>(logs);
            return ResultadoOperacion<IEnumerable<AuditLogDetalleDto>>.Ok(dtos);
        }
    }

    public class UsuarioFamiliaService : IUsuarioFamiliaService
    {
        private readonly IUnitOfWork _uow;
        private readonly FHouseDbContext _context;

        public UsuarioFamiliaService(IUnitOfWork uow, FHouseDbContext context = null)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _context = context;
        }

        public async Task<ResultadoOperacion<FamiliaDetalleDto>> ObtenerFamiliaAsync(int familiaId)
        {
            var familia = await _uow.Familias.ObtenerPorIdAsync(familiaId);
            if (familia == null)
                return ResultadoOperacion<FamiliaDetalleDto>.Falla("Familia no encontrada.");

            var miembros = await _uow.UsuariosFamilia.BuscarAsync(u => u.FamiliaId == familiaId && u.Activo);
            var cuentas = await _uow.Cuentas.ObtenerPorFamiliaAsync(familiaId);
            var fuentes = await _uow.FuentesIngreso.ObtenerPorFamiliaAsync(familiaId);

            var dto = new FamiliaDetalleDto
            {
                Id = familia.Id,
                Nombre = familia.Nombre,
                CodigoInvitacion = familia.CodigoInvitacion,
                TasaCambioActual = familia.TasaCambioActual,
                FechaCreacion = familia.FechaCreacion,
                TotalMiembros = miembros.Count(),
                TotalCuentas = cuentas.Count(),
                TotalFuentes = fuentes.Count()
            };

            return ResultadoOperacion<FamiliaDetalleDto>.Ok(dto);
        }

        public async Task<ResultadoOperacion<bool>> ActualizarNombreFamiliaAsync(int familiaId, string nuevoNombre, string usuarioId)
        {
            if (string.IsNullOrWhiteSpace(nuevoNombre))
                return ResultadoOperacion<bool>.Falla("El nombre de la familia no puede estar vacío.");

            var familia = await _uow.Familias.ObtenerPorIdAsync(familiaId);
            if (familia == null)
                return ResultadoOperacion<bool>.Falla("Familia no encontrada.");

            familia.Nombre = nuevoNombre.Trim();
            familia.FechaModificacion = DateTime.UtcNow;

            _uow.Familias.Actualizar(familia);

            var log = new AuditLog
            {
                FamiliaId = familiaId,
                Entidad = "Familia",
                Accion = "UPDATE_NOMBRE",
                UsuarioId = usuarioId,
                NombreUsuario = "Administrador",
                Detalles = $"Cambio de nombre de familia a '{familia.Nombre}'",
                FechaCreacion = DateTime.UtcNow
            };
            if (_uow.AuditLogs != null)
            {
                await _uow.AuditLogs.AgregarAsync(log);
            }

            await _uow.GuardarCambiosAsync();
            return ResultadoOperacion<bool>.Ok(true, "Nombre de familia actualizado.");
        }

        public async Task<ResultadoOperacion<string>> RegenerarCodigoInvitacionAsync(int familiaId, string usuarioId)
        {
            var familia = await _uow.Familias.ObtenerPorIdAsync(familiaId);
            if (familia == null)
                return ResultadoOperacion<string>.Falla("Familia no encontrada.");

            var nuevoCodigo = "FH-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpperInvariant();
            familia.CodigoInvitacion = nuevoCodigo;
            familia.FechaModificacion = DateTime.UtcNow;

            _uow.Familias.Actualizar(familia);

            var log = new AuditLog
            {
                FamiliaId = familiaId,
                Entidad = "Familia",
                Accion = "REGEN_CODIGO",
                UsuarioId = usuarioId,
                NombreUsuario = "Administrador",
                Detalles = $"Nuevo código de invitación generado: '{nuevoCodigo}'",
                FechaCreacion = DateTime.UtcNow
            };
            if (_uow.AuditLogs != null)
            {
                await _uow.AuditLogs.AgregarAsync(log);
            }

            await _uow.GuardarCambiosAsync();
            return ResultadoOperacion<string>.Ok(nuevoCodigo, "Código de invitación regenerado exitosamente.");
        }

        public async Task<ResultadoOperacion<IEnumerable<MiembroFamiliaDto>>> ObtenerMiembrosFamiliaAsync(int familiaId)
        {
            var miembros = (await _uow.UsuariosFamilia.BuscarAsync(u => u.FamiliaId == familiaId)).ToList();
            var lista = new List<MiembroFamiliaDto>();

            foreach (var m in miembros)
            {
                string nombreCompleto = m.AliasFamiliar;
                string email = $"{m.UsuarioId.ToLower()}@fhouse.local";

                if (_context != null)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == m.UsuarioId);
                    if (user != null)
                    {
                        if (!string.IsNullOrWhiteSpace(user.NombreCompleto))
                            nombreCompleto = user.NombreCompleto;
                        if (!string.IsNullOrWhiteSpace(user.Email))
                            email = user.Email;
                    }
                }

                if (string.IsNullOrWhiteSpace(nombreCompleto))
                    nombreCompleto = "Miembro Familiar";

                lista.Add(new MiembroFamiliaDto
                {
                    Id = m.Id,
                    UsuarioId = m.UsuarioId,
                    NombreCompleto = nombreCompleto,
                    Email = email,
                    Rol = m.Rol,
                    AliasFamiliar = m.AliasFamiliar,
                    FechaUnion = m.FechaCreacion,
                    FamiliaId = m.FamiliaId,
                    Activo = m.Activo
                });
            }

            if (!lista.Any())
            {
                lista.Add(new MiembroFamiliaDto
                {
                    Id = 1,
                    UsuarioId = "usr-admin-001",
                    NombreCompleto = "Administrador Principal",
                    Email = "admin@fhouse.local",
                    Rol = RolFamilia.Admin,
                    AliasFamiliar = "Admin del Hogar",
                    FechaUnion = DateTime.UtcNow.AddMonths(-3),
                    FamiliaId = familiaId,
                    Activo = true
                });
            }

            return ResultadoOperacion<IEnumerable<MiembroFamiliaDto>>.Ok(lista);
        }

        public async Task<ResultadoOperacion<MiembroFamiliaDto>> InvitarMiembroAsync(InvitarMiembroDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
                return ResultadoOperacion<MiembroFamiliaDto>.Falla("El nombre del miembro es requerido.");

            var nuevoUsuarioFamilia = new UsuarioFamilia
            {
                FamiliaId = dto.FamiliaId,
                UsuarioId = Guid.NewGuid().ToString().Substring(0, 8),
                Rol = dto.Rol,
                AliasFamiliar = dto.NombreCompleto,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            await _uow.UsuariosFamilia.AgregarAsync(nuevoUsuarioFamilia);
            await _uow.GuardarCambiosAsync();

            var resultadoDto = new MiembroFamiliaDto
            {
                Id = nuevoUsuarioFamilia.Id,
                UsuarioId = nuevoUsuarioFamilia.UsuarioId,
                NombreCompleto = dto.NombreCompleto,
                Email = dto.Email,
                Rol = dto.Rol,
                AliasFamiliar = dto.AliasFamiliar,
                FechaUnion = nuevoUsuarioFamilia.FechaCreacion,
                FamiliaId = dto.FamiliaId,
                Activo = true
            };

            return ResultadoOperacion<MiembroFamiliaDto>.Ok(resultadoDto, "Miembro familiar agregado con éxito.");
        }

        public async Task<ResultadoOperacion<bool>> CambiarRolAsync(CambiarRolDto dto)
        {
            var miembro = await _uow.UsuariosFamilia.ObtenerPorIdAsync(dto.MiembroId);
            if (miembro == null)
                return ResultadoOperacion<bool>.Falla("Miembro no encontrado.");

            var solicitante = (await _uow.UsuariosFamilia.BuscarAsync(uf => uf.UsuarioId == dto.UsuarioIdSolicitante && uf.FamiliaId == miembro.FamiliaId && uf.Activo)).FirstOrDefault();
            if (solicitante == null || solicitante.Rol != RolFamilia.Admin)
                return ResultadoOperacion<bool>.Falla("Solo un administrador puede cambiar roles de otros integrantes.");

            if (string.Equals(miembro.UsuarioId, dto.UsuarioIdSolicitante, StringComparison.OrdinalIgnoreCase))
                return ResultadoOperacion<bool>.Falla("Por seguridad, ningún usuario puede modificar su propio rol. Solo otro administrador puede hacerlo.");

            miembro.Rol = dto.NuevoRol;
            miembro.FechaModificacion = DateTime.UtcNow;

            _uow.UsuariosFamilia.Actualizar(miembro);
            await _uow.GuardarCambiosAsync();

            return ResultadoOperacion<bool>.Ok(true, $"El rol de '{miembro.AliasFamiliar}' ha sido actualizado a {dto.NuevoRol}.");
        }

        public async Task<ResultadoOperacion<bool>> ActualizarMiembroAsync(ActualizarMiembroDto dto, string usuarioId)
        {
            if (dto == null) return ResultadoOperacion<bool>.Falla("Datos inválidos.");

            var miembro = await _uow.UsuariosFamilia.ObtenerPorIdAsync(dto.Id);
            if (miembro == null) return ResultadoOperacion<bool>.Falla("Miembro no encontrado.");

            var solicitante = (await _uow.UsuariosFamilia.BuscarAsync(uf => uf.UsuarioId == usuarioId && uf.FamiliaId == miembro.FamiliaId && uf.Activo)).FirstOrDefault();
            if (solicitante == null)
            {
                return ResultadoOperacion<bool>.Falla("No tienes membresía activa en este hogar.");
            }

            bool esMismoUsuario = string.Equals(miembro.UsuarioId, usuarioId, StringComparison.OrdinalIgnoreCase);
            bool solicitanteEsAdmin = solicitante.Rol == RolFamilia.Admin;

            if (!solicitanteEsAdmin && !esMismoUsuario)
            {
                return ResultadoOperacion<bool>.Falla("No tienes permisos de administrador para modificar a otros integrantes.");
            }

            // REGLA DE SEGURIDAD: Ningún usuario puede modificar su propio rol ni auto-inhabilitarse
            if (esMismoUsuario)
            {
                // Solo se le permite actualizar su propio alias/parentesco
                miembro.AliasFamiliar = dto.AliasFamiliar?.Trim();
                miembro.FechaModificacion = DateTime.UtcNow;
            }
            else
            {
                // Solo un Administrador puede cambiar el rol o estado de otro usuario
                if (!solicitanteEsAdmin)
                {
                    return ResultadoOperacion<bool>.Falla("Solo un administrador puede modificar el rol o acceso de este integrante.");
                }

                miembro.AliasFamiliar = dto.AliasFamiliar?.Trim();
                miembro.Rol = dto.Rol;
                miembro.Activo = dto.Activo;
                miembro.FechaModificacion = DateTime.UtcNow;
            }

            _uow.UsuariosFamilia.Actualizar(miembro);

            var log = new AuditLog
            {
                FamiliaId = miembro.FamiliaId,
                Entidad = "UsuarioFamilia",
                Accion = "UPDATE_MIEMBRO",
                RegistroId = miembro.Id.ToString(),
                UsuarioId = usuarioId,
                NombreUsuario = solicitante.AliasFamiliar ?? "Usuario",
                Detalles = $"Actualización de miembro '{miembro.AliasFamiliar}' (Rol: {miembro.Rol}, Activo: {miembro.Activo}) por '{solicitante.AliasFamiliar}'",
                FechaCreacion = DateTime.UtcNow
            };
            if (_uow.AuditLogs != null)
            {
                await _uow.AuditLogs.AgregarAsync(log);
            }

            await _uow.GuardarCambiosAsync();
            return ResultadoOperacion<bool>.Ok(true, "Miembro familiar actualizado correctamente.");
        }

        public async Task<ResultadoOperacion<bool>> InhabilitarMiembroAsync(int usuarioFamiliaId, string usuarioId, int? familiaId = null)
        {
            var miembro = await _uow.UsuariosFamilia.ObtenerPorIdAsync(usuarioFamiliaId);
            if (miembro == null) return ResultadoOperacion<bool>.Falla("Miembro no encontrado.");

            if (familiaId.HasValue && miembro.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para inhabilitar miembros de otra familia.");

            if (string.Equals(miembro.UsuarioId, usuarioId, StringComparison.OrdinalIgnoreCase))
            {
                return ResultadoOperacion<bool>.Falla("No puedes inhabilitar tu propia cuenta de acceso.");
            }

            miembro.Activo = false;
            miembro.FechaModificacion = DateTime.UtcNow;

            _uow.UsuariosFamilia.Actualizar(miembro);

            var log = new AuditLog
            {
                FamiliaId = miembro.FamiliaId,
                Entidad = "UsuarioFamilia",
                Accion = "DISABLE_MIEMBRO",
                RegistroId = miembro.Id.ToString(),
                UsuarioId = usuarioId,
                NombreUsuario = "Usuario",
                Detalles = $"Inhabilitación del miembro '{miembro.AliasFamiliar}'",
                FechaCreacion = DateTime.UtcNow
            };
            if (_uow.AuditLogs != null)
            {
                await _uow.AuditLogs.AgregarAsync(log);
            }

            await _uow.GuardarCambiosAsync();
            return ResultadoOperacion<bool>.Ok(true, $"El miembro '{miembro.AliasFamiliar}' ha sido inhabilitado.");
        }

        public async Task<ResultadoOperacion<bool>> HabilitarMiembroAsync(int usuarioFamiliaId, string usuarioId, int? familiaId = null)
        {
            var miembro = await _uow.UsuariosFamilia.ObtenerPorIdAsync(usuarioFamiliaId);
            if (miembro == null) return ResultadoOperacion<bool>.Falla("Miembro no encontrado.");

            if (familiaId.HasValue && miembro.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para habilitar miembros de otra familia.");

            miembro.Activo = true;
            miembro.FechaModificacion = DateTime.UtcNow;

            _uow.UsuariosFamilia.Actualizar(miembro);

            var log = new AuditLog
            {
                FamiliaId = miembro.FamiliaId,
                Entidad = "UsuarioFamilia",
                Accion = "ENABLE_MIEMBRO",
                RegistroId = miembro.Id.ToString(),
                UsuarioId = usuarioId,
                NombreUsuario = "Usuario",
                Detalles = $"Reactivación del miembro '{miembro.AliasFamiliar}'",
                FechaCreacion = DateTime.UtcNow
            };
            if (_uow.AuditLogs != null)
            {
                await _uow.AuditLogs.AgregarAsync(log);
            }

            await _uow.GuardarCambiosAsync();
            return ResultadoOperacion<bool>.Ok(true, $"El miembro '{miembro.AliasFamiliar}' ha sido reactivado.");
        }

        public async Task<ResultadoOperacion<bool>> EliminarMiembroAsync(int usuarioFamiliaId, string usuarioId, int? familiaId = null)
        {
            var miembro = await _uow.UsuariosFamilia.ObtenerPorIdAsync(usuarioFamiliaId);
            if (miembro == null) return ResultadoOperacion<bool>.Falla("Miembro no encontrado.");

            if (familiaId.HasValue && miembro.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para eliminar miembros de otra familia.");

            if (string.Equals(miembro.UsuarioId, usuarioId, StringComparison.OrdinalIgnoreCase))
            {
                return ResultadoOperacion<bool>.Falla("No puedes eliminar tu propia cuenta del hogar.");
            }

            // Verificar si el miembro posee transacciones/movimientos activos
            bool tieneTransaccionesActivas = await _uow.Transacciones.ExisteAsync(t => 
                t.FamiliaId == miembro.FamiliaId && t.UsuarioRegistradorId == miembro.UsuarioId && t.Activo);

            if (tieneTransaccionesActivas)
            {
                // Inhabilitar para proteger historial contable
                miembro.Activo = false;
                miembro.FechaModificacion = DateTime.UtcNow;

                _uow.UsuariosFamilia.Actualizar(miembro);

                var log = new AuditLog
                {
                    FamiliaId = miembro.FamiliaId,
                    Entidad = "UsuarioFamilia",
                    Accion = "DISABLE_MIEMBRO",
                    RegistroId = miembro.Id.ToString(),
                    UsuarioId = usuarioId,
                    NombreUsuario = "Usuario",
                    Detalles = $"Inhabilitación del miembro '{miembro.AliasFamiliar}' (posee transacciones activas registradas)",
                    FechaCreacion = DateTime.UtcNow
                };
                if (_uow.AuditLogs != null)
                {
                    await _uow.AuditLogs.AgregarAsync(log);
                }

                await _uow.GuardarCambiosAsync();
                return ResultadoOperacion<bool>.Ok(true, $"El miembro '{miembro.AliasFamiliar}' posee movimientos registrados. Ha sido inhabilitado para proteger el historial financiero.");
            }
            else
            {
                _uow.UsuariosFamilia.Eliminar(miembro);

                var log = new AuditLog
                {
                    FamiliaId = miembro.FamiliaId,
                    Entidad = "UsuarioFamilia",
                    Accion = "DELETE_MIEMBRO",
                    RegistroId = miembro.Id.ToString(),
                    UsuarioId = usuarioId,
                    NombreUsuario = "Usuario",
                    Detalles = $"Eliminación permanente del miembro familiar '{miembro.AliasFamiliar}' (sin movimientos activos)",
                    FechaCreacion = DateTime.UtcNow
                };
                if (_uow.AuditLogs != null)
                {
                    await _uow.AuditLogs.AgregarAsync(log);
                }

                await _uow.GuardarCambiosAsync();
                return ResultadoOperacion<bool>.Ok(true, $"El miembro '{miembro.AliasFamiliar}' ha sido eliminado correctamente.");
            }
        }

        public async Task<ResultadoOperacion<bool>> AprobarMiembroAsync(int usuarioFamiliaId, RolFamilia rol, string usuarioAdminId, int? familiaId = null)
        {
            var miembro = await _uow.UsuariosFamilia.ObtenerPorIdAsync(usuarioFamiliaId);
            if (miembro == null) return ResultadoOperacion<bool>.Falla("Registro de usuario no encontrado.");

            if (familiaId.HasValue && miembro.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para autorizar miembros de otra familia.");

            miembro.Activo = true;
            miembro.Rol = rol;
            miembro.FechaModificacion = DateTime.UtcNow;

            _uow.UsuariosFamilia.Actualizar(miembro);

            var log = new AuditLog
            {
                FamiliaId = miembro.FamiliaId,
                Entidad = "UsuarioFamilia",
                Accion = "APROBAR_ACCESO",
                RegistroId = miembro.Id.ToString(),
                UsuarioId = usuarioAdminId,
                NombreUsuario = "Administrador",
                Detalles = $"Aprobación y autorización de acceso al usuario '{miembro.AliasFamiliar}' con rol '{rol}'",
                FechaCreacion = DateTime.UtcNow
            };
            if (_uow.AuditLogs != null)
            {
                await _uow.AuditLogs.AgregarAsync(log);
            }

            await _uow.GuardarCambiosAsync();
            return ResultadoOperacion<bool>.Ok(true, $"¡Usuario '{miembro.AliasFamiliar}' aprobado con éxito con el rol de {rol}!");
        }

        public async Task<ResultadoOperacion<bool>> RechazarMiembroAsync(int usuarioFamiliaId, string usuarioAdminId, int? familiaId = null)
        {
            var miembro = await _uow.UsuariosFamilia.ObtenerPorIdAsync(usuarioFamiliaId);
            if (miembro == null) return ResultadoOperacion<bool>.Falla("Registro de usuario no encontrado.");

            if (familiaId.HasValue && miembro.FamiliaId != familiaId.Value)
                return ResultadoOperacion<bool>.Falla("No tiene permisos para rechazar miembros de otra familia.");

            _uow.UsuariosFamilia.Eliminar(miembro);

            var log = new AuditLog
            {
                FamiliaId = miembro.FamiliaId,
                Entidad = "UsuarioFamilia",
                Accion = "RECHAZAR_ACCESO",
                RegistroId = miembro.Id.ToString(),
                UsuarioId = usuarioAdminId,
                NombreUsuario = "Administrador",
                Detalles = $"Rechazo de solicitud de acceso del usuario '{miembro.AliasFamiliar}'",
                FechaCreacion = DateTime.UtcNow
            };
            if (_uow.AuditLogs != null)
            {
                await _uow.AuditLogs.AgregarAsync(log);
            }

            await _uow.GuardarCambiosAsync();
            return ResultadoOperacion<bool>.Ok(true, $"La solicitud de acceso de '{miembro.AliasFamiliar}' ha sido rechazada.");
        }
    }

    public class ReporteService : IReporteService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public ReporteService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<ResultadoOperacion<ReporteFinancieroDto>> GenerarReporteAsync(int familiaId, DateTime desde, DateTime hasta)
        {
            var familia = await _uow.Familias.ObtenerPorIdAsync(familiaId);
            var transacciones = (await _uow.Transacciones.ObtenerFiltradasAsync(familiaId, desde, hasta, null, null, null, null, null)).ToList();
            var fuentes = (await _uow.FuentesIngreso.ObtenerPorFamiliaAsync(familiaId)).ToList();

            var reporte = new ReporteFinancieroDto
            {
                FamiliaId = familiaId,
                NombreFamilia = familia?.Nombre ?? "F House",
                Desde = desde,
                Hasta = hasta,
                TotalIngresosDOP = transacciones.Where(t => t.Tipo == TipoTransaccion.Ingreso).Sum(t => t.MontoEnDOP),
                TotalEgresosDOP = transacciones.Where(t => t.Tipo == TipoTransaccion.Egreso).Sum(t => t.MontoEnDOP),
                Transacciones = _mapper.Map<List<TransaccionDetalleDto>>(transacciones)
            };

            foreach (var f in fuentes)
            {
                var fDto = _mapper.Map<FuenteIngresoDetalleDto>(f);
                var fTrans = transacciones.Where(t => t.FuenteIngresoId == f.Id).ToList();
                fDto.TotalIngresosDOP = fTrans.Where(t => t.Tipo == TipoTransaccion.Ingreso).Sum(t => t.MontoEnDOP);
                fDto.TotalEgresosDOP = fTrans.Where(t => t.Tipo == TipoTransaccion.Egreso).Sum(t => t.MontoEnDOP);
                fDto.CantidadTransacciones = fTrans.Count;
                reporte.DesglosePorFuente.Add(fDto);
            }

            var egresos = transacciones.Where(t => t.Tipo == TipoTransaccion.Egreso).ToList();
            var totalEgr = reporte.TotalEgresosDOP;

            reporte.DesglosePorCategoria = egresos
                .GroupBy(t => t.Categoria != null ? t.Categoria.Nombre : "Sin Categoría")
                .Select(g => new CategoriaGastoDto
                {
                    Categoria = g.Key,
                    TotalDOP = g.Sum(t => t.MontoEnDOP),
                    Color = g.FirstOrDefault()?.Categoria?.Color ?? "#86868B",
                    Icono = g.FirstOrDefault()?.Categoria?.Icono ?? "tag",
                    Porcentaje = totalEgr > 0 ? Math.Round((g.Sum(t => t.MontoEnDOP) / totalEgr) * 100, 1) : 0
                })
                .OrderByDescending(c => c.TotalDOP)
                .ToList();

            return ResultadoOperacion<ReporteFinancieroDto>.Ok(reporte);
        }

        public async Task<string> ExportarCsvAsync(int familiaId, DateTime desde, DateTime hasta)
        {
            var transacciones = await _uow.Transacciones.ObtenerFiltradasAsync(familiaId, desde, hasta, null, null, null, null, null);
            var sb = new StringBuilder();
            sb.AppendLine("ID,Fecha,Concepto,Tipo,Moneda,Monto,MontoEnDOP,FuenteIngreso,Cuenta,Categoria,Usuario,Comentario");

            foreach (var t in transacciones)
            {
                string fuente = t.FuenteIngreso != null ? t.FuenteIngreso.Nombre : "General";
                string cuenta = t.CuentaOrigen != null ? t.CuentaOrigen.Nombre : "Sin Cuenta";
                string categoria = t.Categoria != null ? t.Categoria.Nombre : "Sin Categoría";
                sb.AppendLine($"{t.Id},{t.FechaTransaccion:yyyy-MM-dd},\"{t.Concepto}\",{t.Tipo},{t.Moneda},{t.Monto:F2},{t.MontoEnDOP:F2},\"{fuente}\",\"{cuenta}\",\"{categoria}\",\"{t.NombreUsuarioRegistrador}\",\"{t.Comentario}\"");
            }

            return sb.ToString();
        }
    }
}
