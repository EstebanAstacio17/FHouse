using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FHouse.Core.Entities;
using FHouse.Core.Enums;
using FHouse.Core.Interfaces.Repositories;
using FHouse.Infrastructure.Data;

namespace FHouse.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly FHouseDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(FHouseDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T> ObtenerPorIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.Id == id);
        }

        public virtual async Task<IEnumerable<T>> ObtenerTodosAsync()
        {
            return await _dbSet.Where(e => e.Activo).ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> BuscarAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T> PrimerODefectoAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).FirstOrDefaultAsync();
        }

        public virtual async Task<bool> ExisteAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public virtual async Task AgregarAsync(T entity)
        {
            _dbSet.Add(entity);
            await Task.CompletedTask;
        }

        public virtual void Actualizar(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
        }

        public virtual void Eliminar(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual void EliminarLogico(T entity)
        {
            entity.Activo = false;
            entity.FechaModificacion = DateTime.UtcNow;
            _context.Entry(entity).State = EntityState.Modified;
        }
    }

    public class TransaccionRepository : Repository<Transaccion>, ITransaccionRepository
    {
        public TransaccionRepository(FHouseDbContext context) : base(context) { }

        public async Task<IEnumerable<Transaccion>> ObtenerPorFamiliaAsync(int familiaId, int? limit = null)
        {
            var query = _dbSet.Include(t => t.FuenteIngreso)
                              .Include(t => t.CuentaOrigen)
                              .Include(t => t.CuentaDestino)
                              .Include(t => t.Categoria)
                              .Where(t => t.FamiliaId == familiaId && t.Activo)
                              .OrderByDescending(t => t.FechaTransaccion);

            if (limit.HasValue)
            {
                return await query.Take(limit.Value).ToListAsync();
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Transaccion>> ObtenerPorFuenteAsync(int fuenteIngresoId, int? limit = null)
        {
            var query = _dbSet.Include(t => t.CuentaOrigen)
                              .Include(t => t.Categoria)
                              .Where(t => t.FuenteIngresoId == fuenteIngresoId && t.Activo)
                              .OrderByDescending(t => t.FechaTransaccion);

            if (limit.HasValue)
            {
                return await query.Take(limit.Value).ToListAsync();
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Transaccion>> ObtenerPorCuentaAsync(int cuentaId)
        {
            return await _dbSet.Include(t => t.FuenteIngreso)
                              .Include(t => t.Categoria)
                              .Where(t => (t.CuentaOrigenId == cuentaId || t.CuentaDestinoId == cuentaId) && t.Activo)
                              .OrderByDescending(t => t.FechaTransaccion)
                              .ToListAsync();
        }

        public async Task<IEnumerable<Transaccion>> ObtenerFiltradasAsync(int familiaId, DateTime? desde, DateTime? hasta, int? fuenteId, int? cuentaId, int? categoriaId, TipoTransaccion? tipo, string usuarioId)
        {
            var query = _dbSet.Include(t => t.FuenteIngreso)
                              .Include(t => t.CuentaOrigen)
                              .Include(t => t.Categoria)
                              .Where(t => t.FamiliaId == familiaId && t.Activo);

            if (desde.HasValue)
                query = query.Where(t => t.FechaTransaccion >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(t => t.FechaTransaccion <= hasta.Value);

            if (fuenteId.HasValue)
                query = query.Where(t => t.FuenteIngresoId == fuenteId.Value);

            if (cuentaId.HasValue)
                query = query.Where(t => t.CuentaOrigenId == cuentaId.Value || t.CuentaDestinoId == cuentaId.Value);

            if (categoriaId.HasValue)
                query = query.Where(t => t.CategoriaId == categoriaId.Value);

            if (tipo.HasValue)
                query = query.Where(t => t.Tipo == tipo.Value);

            if (!string.IsNullOrEmpty(usuarioId))
                query = query.Where(t => t.UsuarioRegistradorId == usuarioId);

            return await query.OrderByDescending(t => t.FechaTransaccion).ToListAsync();
        }

        public async Task<decimal> CalcularTotalIngresosFamiliaAsync(int familiaId, DateTime desde, DateTime hasta)
        {
            return await _dbSet.Where(t => t.FamiliaId == familiaId && t.Activo && t.Tipo == TipoTransaccion.Ingreso && t.FechaTransaccion >= desde && t.FechaTransaccion <= hasta)
                               .Select(t => (decimal?)t.MontoEnDOP)
                               .SumAsync() ?? 0m;
        }

        public async Task<decimal> CalcularTotalEgresosFamiliaAsync(int familiaId, DateTime desde, DateTime hasta)
        {
            return await _dbSet.Where(t => t.FamiliaId == familiaId && t.Activo && t.Tipo == TipoTransaccion.Egreso && t.FechaTransaccion >= desde && t.FechaTransaccion <= hasta)
                               .Select(t => (decimal?)t.MontoEnDOP)
                               .SumAsync() ?? 0m;
        }
    }

    public class FuenteIngresoRepository : Repository<FuenteIngreso>, IFuenteIngresoRepository
    {
        public FuenteIngresoRepository(FHouseDbContext context) : base(context) { }

        public async Task<IEnumerable<FuenteIngreso>> ObtenerPorFamiliaAsync(int familiaId)
        {
            return await _dbSet.Include(f => f.CuentasAsociadas)
                               .Where(f => f.FamiliaId == familiaId)
                               .OrderByDescending(f => f.Activo)
                               .ThenBy(f => f.Nombre)
                               .ToListAsync();
        }

        public async Task<FuenteIngreso> ObtenerConDetallesAsync(int id)
        {
            return await _dbSet.Include(f => f.CuentasAsociadas)
                               .Include(f => f.Transacciones)
                               .FirstOrDefaultAsync(f => f.Id == id);
        }
    }

    public class CuentaRepository : Repository<Cuenta>, ICuentaRepository
    {
        public CuentaRepository(FHouseDbContext context) : base(context) { }

        public async Task<IEnumerable<Cuenta>> ObtenerPorFamiliaAsync(int familiaId)
        {
            return await _dbSet.Include(c => c.FuenteIngreso)
                               .Where(c => c.FamiliaId == familiaId)
                               .OrderByDescending(c => c.Activo)
                               .ThenBy(c => c.Nombre)
                               .ToListAsync();
        }

        public async Task<IEnumerable<Cuenta>> ObtenerPorFuenteAsync(int fuenteId)
        {
            return await _dbSet.Where(c => c.FuenteIngresoId == fuenteId && c.Activo)
                               .OrderByDescending(c => c.Activo)
                               .ThenBy(c => c.Nombre)
                               .ToListAsync();
        }
    }

    public class CategoriaRepository : Repository<Categoria>, ICategoriaRepository
    {
        public CategoriaRepository(FHouseDbContext context) : base(context) { }

        public async Task<IEnumerable<Categoria>> ObtenerPorFamiliaAsync(int familiaId)
        {
            return await _dbSet.Where(c => c.FamiliaId == familiaId && c.Activo).ToListAsync();
        }
    }

    public class PresupuestoRepository : Repository<Presupuesto>, IPresupuestoRepository
    {
        public PresupuestoRepository(FHouseDbContext context) : base(context) { }

        public async Task<IEnumerable<Presupuesto>> ObtenerPorFamiliaPeriodoAsync(int familiaId, int mes, int anio)
        {
            return await _dbSet.Include(p => p.Categoria)
                               .Include(p => p.FuenteIngreso)
                               .Where(p => p.FamiliaId == familiaId && p.Mes == mes && p.Anio == anio && p.Activo)
                               .ToListAsync();
        }
    }

    public class TasaCambioRepository : Repository<TasaCambio>, ITasaCambioRepository
    {
        public TasaCambioRepository(FHouseDbContext context) : base(context) { }

        public async Task<TasaCambio> ObtenerUltimaTasaAsync(int familiaId)
        {
            return await _dbSet.Where(t => t.FamiliaId == familiaId && t.Activo)
                               .OrderByDescending(t => t.FechaVigencia)
                               .FirstOrDefaultAsync();
        }
    }

    public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(FHouseDbContext context) : base(context) { }

        public async Task<IEnumerable<AuditLog>> ObtenerPorFamiliaAsync(int familiaId, int limit = 100)
        {
            return await _dbSet.Where(a => a.FamiliaId == familiaId || a.FamiliaId == null)
                               .OrderByDescending(a => a.FechaCreacion)
                               .Take(limit)
                               .ToListAsync();
        }
    }
}
