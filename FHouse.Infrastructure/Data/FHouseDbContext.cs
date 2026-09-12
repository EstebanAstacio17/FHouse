using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using FHouse.Core.Entities;
using FHouse.Infrastructure.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Newtonsoft.Json;

namespace FHouse.Infrastructure.Data
{
    public class FHouseDbContext : IdentityDbContext<ApplicationUser>
    {
        static FHouseDbContext()
        {
            Database.SetInitializer<FHouseDbContext>(new CreateDatabaseIfNotExists<FHouseDbContext>());
        }

        public FHouseDbContext() : base("Name=FHouseConnection", throwIfV1Schema: false)
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public FHouseDbContext(string nameOrConnectionString) : base(nameOrConnectionString, throwIfV1Schema: false)
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public static FHouseDbContext Create()
        {
            return new FHouseDbContext();
        }

        public DbSet<Familia> Familias { get; set; }
        public DbSet<UsuarioFamilia> UsuariosFamilia { get; set; }
        public DbSet<FuenteIngreso> FuentesIngreso { get; set; }
        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Transaccion> Transacciones { get; set; }
        public DbSet<Presupuesto> Presupuestos { get; set; }
        public DbSet<TasaCambio> TasasCambio { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            // Configuración de decimales
            modelBuilder.Entity<Familia>().Property(f => f.TasaCambioActual).HasPrecision(18, 4);
            modelBuilder.Entity<TasaCambio>().Property(t => t.TasaDOPporUSD).HasPrecision(18, 4);
            modelBuilder.Entity<Cuenta>().Property(c => c.SaldoActual).HasPrecision(18, 2);
            modelBuilder.Entity<Transaccion>().Property(t => t.Monto).HasPrecision(18, 2);
            modelBuilder.Entity<Transaccion>().Property(t => t.MontoEnDOP).HasPrecision(18, 2);
            modelBuilder.Entity<Transaccion>().Property(t => t.TasaCambioAplicada).HasPrecision(18, 4);
            modelBuilder.Entity<Presupuesto>().Property(p => p.MontoLimite).HasPrecision(18, 2);

            // Relaciones explícitas
            modelBuilder.Entity<Transaccion>()
                .HasOptional(t => t.CuentaOrigen)
                .WithMany(c => c.TransaccionesOrigen)
                .HasForeignKey(t => t.CuentaOrigenId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Transaccion>()
                .HasOptional(t => t.CuentaDestino)
                .WithMany(c => c.TransaccionesDestino)
                .HasForeignKey(t => t.CuentaDestinoId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Transaccion>()
                .HasOptional(t => t.FuenteIngreso)
                .WithMany(f => f.Transacciones)
                .HasForeignKey(t => t.FuenteIngresoId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Cuenta>()
                .HasOptional(c => c.FuenteIngreso)
                .WithMany(f => f.CuentasAsociadas)
                .HasForeignKey(c => c.FuenteIngresoId)
                .WillCascadeOnDelete(false);
        }

        public override async Task<int> SaveChangesAsync()
        {
            var auditEntries = PreSaveChanges();
            var result = await base.SaveChangesAsync();
            await PostSaveChangesAsync(auditEntries);
            return result;
        }

        public override int SaveChanges()
        {
            var auditEntries = PreSaveChanges();
            var result = base.SaveChanges();
            PostSaveChanges(auditEntries);
            return result;
        }

        private List<AuditEntry> PreSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();

            string currentUserId = null;
            string currentUserName = "Sistema";
            string currentIp = "127.0.0.1";

            try
            {
                if (HttpContext.Current?.User?.Identity != null && HttpContext.Current.User.Identity.IsAuthenticated)
                {
                    currentUserName = HttpContext.Current.User.Identity.Name;
                }
                if (HttpContext.Current?.Request != null)
                {
                    currentIp = HttpContext.Current.Request.UserHostAddress ?? "127.0.0.1";
                }
            }
            catch
            {
                // Modo test o background worker
            }

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditEntry(entry)
                {
                    TableName = entry.Entity.GetType().Name,
                    UserId = currentUserId,
                    UserName = currentUserName,
                    IpAddress = currentIp
                };

                // Actualizar FechaModificacion en entidades BaseEntity
                if (entry.Entity is BaseEntity baseEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        baseEntity.FechaCreacion = DateTime.UtcNow;
                        baseEntity.Activo = true;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        baseEntity.FechaModificacion = DateTime.UtcNow;
                    }
                }

                auditEntries.Add(auditEntry);

                var propNames = entry.State == EntityState.Deleted
                    ? entry.OriginalValues.PropertyNames
                    : entry.CurrentValues.PropertyNames;

                foreach (var propName in propNames)
                {
                    if (entry.State == EntityState.Added)
                    {
                        auditEntry.NewValues[propName] = entry.CurrentValues[propName];
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        auditEntry.OldValues[propName] = entry.OriginalValues[propName];
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        if (entry.Property(propName).IsModified)
                        {
                            auditEntry.ChangedColumns.Add(propName);
                            auditEntry.OldValues[propName] = entry.OriginalValues[propName];
                            auditEntry.NewValues[propName] = entry.CurrentValues[propName];
                        }
                    }
                }
            }

            return auditEntries;
        }

        private async Task PostSaveChangesAsync(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0) return;

            foreach (var auditEntry in auditEntries)
            {
                AuditLogs.Add(auditEntry.ToAuditLog());
            }

            await base.SaveChangesAsync();
        }

        private void PostSaveChanges(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0) return;

            foreach (var auditEntry in auditEntries)
            {
                AuditLogs.Add(auditEntry.ToAuditLog());
            }

            base.SaveChanges();
        }
    }

    public class AuditEntry
    {
        public AuditEntry(DbEntityEntry entry)
        {
            Entry = entry;
            State = entry.State;
        }

        public DbEntityEntry Entry { get; }
        public EntityState State { get; set; }
        public string TableName { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string IpAddress { get; set; }
        public Dictionary<string, object> KeyValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();
        public List<string> ChangedColumns { get; } = new List<string>();

        public AuditLog ToAuditLog()
        {
            var audit = new AuditLog
            {
                Entidad = TableName,
                Accion = State.ToString().ToUpper(),
                UsuarioId = UserId,
                NombreUsuario = UserName,
                DireccionIP = IpAddress,
                FechaCreacion = DateTime.UtcNow,
                ValoresAnterioresJson = OldValues.Count == 0 ? null : JsonConvert.SerializeObject(OldValues),
                ValoresNuevosJson = NewValues.Count == 0 ? null : JsonConvert.SerializeObject(NewValues),
                Detalles = ChangedColumns.Count == 0 ? null : $"Columnas modificadas: {string.Join(", ", ChangedColumns)}"
            };

            if (Entry.Entity is BaseEntity entity)
            {
                audit.RegistroId = entity.Id.ToString();
                var propFam = entity.GetType().GetProperty("FamiliaId");
                if (propFam != null)
                {
                    var famVal = propFam.GetValue(entity);
                    if (famVal is int famInt)
                    {
                        audit.FamiliaId = famInt;
                    }
                }
            }

            return audit;
        }
    }
}
