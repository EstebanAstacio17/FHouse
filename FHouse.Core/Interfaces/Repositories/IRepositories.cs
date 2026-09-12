using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FHouse.Core.Entities;
using FHouse.Core.Enums;

namespace FHouse.Core.Interfaces.Repositories
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T> ObtenerPorIdAsync(int id);
        Task<IEnumerable<T>> ObtenerTodosAsync();
        Task<IEnumerable<T>> BuscarAsync(Expression<Func<T, bool>> predicate);
        Task<T> PrimerODefectoAsync(Expression<Func<T, bool>> predicate);
        Task<bool> ExisteAsync(Expression<Func<T, bool>> predicate);
        Task AgregarAsync(T entity);
        void Actualizar(T entity);
        void Eliminar(T entity);
        void EliminarLogico(T entity);
    }

    public interface ITransaccionRepository : IRepository<Transaccion>
    {
        Task<IEnumerable<Transaccion>> ObtenerPorFamiliaAsync(int familiaId, int? limit = null);
        Task<IEnumerable<Transaccion>> ObtenerPorFuenteAsync(int fuenteIngresoId, int? limit = null);
        Task<IEnumerable<Transaccion>> ObtenerPorCuentaAsync(int cuentaId);
        Task<IEnumerable<Transaccion>> ObtenerFiltradasAsync(int familiaId, DateTime? desde, DateTime? hasta, int? fuenteId, int? cuentaId, int? categoriaId, TipoTransaccion? tipo, string usuarioId);
        Task<decimal> CalcularTotalIngresosFamiliaAsync(int familiaId, DateTime desde, DateTime hasta);
        Task<decimal> CalcularTotalEgresosFamiliaAsync(int familiaId, DateTime desde, DateTime hasta);
    }

    public interface IFuenteIngresoRepository : IRepository<FuenteIngreso>
    {
        Task<IEnumerable<FuenteIngreso>> ObtenerPorFamiliaAsync(int familiaId);
        Task<FuenteIngreso> ObtenerConDetallesAsync(int id);
    }

    public interface ICuentaRepository : IRepository<Cuenta>
    {
        Task<IEnumerable<Cuenta>> ObtenerPorFamiliaAsync(int familiaId);
        Task<IEnumerable<Cuenta>> ObtenerPorFuenteAsync(int fuenteId);
    }

    public interface ICategoriaRepository : IRepository<Categoria>
    {
        Task<IEnumerable<Categoria>> ObtenerPorFamiliaAsync(int familiaId);
    }

    public interface IPresupuestoRepository : IRepository<Presupuesto>
    {
        Task<IEnumerable<Presupuesto>> ObtenerPorFamiliaPeriodoAsync(int familiaId, int mes, int anio);
    }

    public interface ITasaCambioRepository : IRepository<TasaCambio>
    {
        Task<TasaCambio> ObtenerUltimaTasaAsync(int familiaId);
    }

    public interface IAuditLogRepository : IRepository<AuditLog>
    {
        Task<IEnumerable<AuditLog>> ObtenerPorFamiliaAsync(int familiaId, int limit = 100);
    }

    public interface IUnitOfWork : IDisposable
    {
        ITransaccionRepository Transacciones { get; }
        IFuenteIngresoRepository FuentesIngreso { get; }
        ICuentaRepository Cuentas { get; }
        ICategoriaRepository Categorias { get; }
        IPresupuestoRepository Presupuestos { get; }
        ITasaCambioRepository TasasCambio { get; }
        IAuditLogRepository AuditLogs { get; }
        IRepository<Familia> Familias { get; }
        IRepository<UsuarioFamilia> UsuariosFamilia { get; }

        Task<int> GuardarCambiosAsync();
        Task<TResult> EjecutarEnTransaccionAsync<TResult>(Func<Task<TResult>> accion);
        Task EjecutarEnTransaccionAsync(Func<Task> accion);
    }
}
