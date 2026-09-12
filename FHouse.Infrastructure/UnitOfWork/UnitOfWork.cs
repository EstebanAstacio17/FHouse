using System;
using System.Threading.Tasks;
using FHouse.Core.Entities;
using FHouse.Core.Interfaces.Repositories;
using FHouse.Infrastructure.Data;
using FHouse.Infrastructure.Repositories;

namespace FHouse.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FHouseDbContext _context;
        private bool _disposed = false;

        private ITransaccionRepository _transacciones;
        private IFuenteIngresoRepository _fuentesIngreso;
        private ICuentaRepository _cuentas;
        private ICategoriaRepository _categorias;
        private IPresupuestoRepository _presupuestos;
        private ITasaCambioRepository _tasasCambio;
        private IAuditLogRepository _auditLogs;
        private IRepository<Familia> _familias;
        private IRepository<UsuarioFamilia> _usuariosFamilia;

        public UnitOfWork(FHouseDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public ITransaccionRepository Transacciones =>
            _transacciones ?? (_transacciones = new TransaccionRepository(_context));

        public IFuenteIngresoRepository FuentesIngreso =>
            _fuentesIngreso ?? (_fuentesIngreso = new FuenteIngresoRepository(_context));

        public ICuentaRepository Cuentas =>
            _cuentas ?? (_cuentas = new CuentaRepository(_context));

        public ICategoriaRepository Categorias =>
            _categorias ?? (_categorias = new CategoriaRepository(_context));

        public IPresupuestoRepository Presupuestos =>
            _presupuestos ?? (_presupuestos = new PresupuestoRepository(_context));

        public ITasaCambioRepository TasasCambio =>
            _tasasCambio ?? (_tasasCambio = new TasaCambioRepository(_context));

        public IAuditLogRepository AuditLogs =>
            _auditLogs ?? (_auditLogs = new AuditLogRepository(_context));

        public IRepository<Familia> Familias =>
            _familias ?? (_familias = new Repository<Familia>(_context));

        public IRepository<UsuarioFamilia> UsuariosFamilia =>
            _usuariosFamilia ?? (_usuariosFamilia = new Repository<UsuarioFamilia>(_context));

        public async Task<int> GuardarCambiosAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
