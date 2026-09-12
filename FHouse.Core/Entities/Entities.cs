using System;
using System.Collections.Generic;
using FHouse.Core.Enums;

namespace FHouse.Core.Entities
{
    public class Familia : BaseEntity
    {
        public string Nombre { get; set; }
        public string CodigoInvitacion { get; set; }
        public string Descripcion { get; set; }
        public decimal TasaCambioActual { get; set; } = 59.50m; // 1 USD = X DOP

        public virtual ICollection<UsuarioFamilia> Miembros { get; set; } = new List<UsuarioFamilia>();
        public virtual ICollection<FuenteIngreso> FuentesIngreso { get; set; } = new List<FuenteIngreso>();
        public virtual ICollection<Cuenta> Cuentas { get; set; } = new List<Cuenta>();
        public virtual ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
        public virtual ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
        public virtual ICollection<Presupuesto> Presupuestos { get; set; } = new List<Presupuesto>();
        public virtual ICollection<TasaCambio> TasasCambio { get; set; } = new List<TasaCambio>();
    }

    public class UsuarioFamilia : BaseEntity
    {
        public string UsuarioId { get; set; } // ASP.NET Identity User Id
        public int FamiliaId { get; set; }
        public RolFamilia Rol { get; set; } = RolFamilia.Miembro;
        public string AliasFamiliar { get; set; }

        public virtual Familia Familia { get; set; }
    }

    public class FuenteIngreso : BaseEntity
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public TipoFuenteIngreso Tipo { get; set; } = TipoFuenteIngreso.Negocio;
        public int FamiliaId { get; set; }
        public string UsuarioCreadorId { get; set; }
        public string ColorIdentificador { get; set; } = "#0071E3"; // Apple blue default

        public virtual Familia Familia { get; set; }
        public virtual ICollection<Cuenta> CuentasAsociadas { get; set; } = new List<Cuenta>();
        public virtual ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
        public virtual ICollection<Presupuesto> Presupuestos { get; set; } = new List<Presupuesto>();
    }

    public class Cuenta : BaseEntity
    {
        public string Nombre { get; set; }
        public string InstitucionFinanciera { get; set; } // Banco / Entidad
        public string NumeroCuenta { get; set; }
        public TipoCuenta Tipo { get; set; } = TipoCuenta.Ahorro;
        public Moneda Moneda { get; set; } = Moneda.DOP;
        public decimal SaldoActual { get; set; } = 0m;
        public int FamiliaId { get; set; }
        public int? FuenteIngresoId { get; set; }
        public string UsuarioResponsableId { get; set; }

        public virtual Familia Familia { get; set; }
        public virtual FuenteIngreso FuenteIngreso { get; set; }
        public virtual ICollection<Transaccion> TransaccionesOrigen { get; set; } = new List<Transaccion>();
        public virtual ICollection<Transaccion> TransaccionesDestino { get; set; } = new List<Transaccion>();
    }

    public class Categoria : BaseEntity
    {
        public string Nombre { get; set; }
        public string Icono { get; set; } = "tag"; // Lucide icon identifier
        public string Color { get; set; } = "#86868B";
        public TipoCategoria Tipo { get; set; } = TipoCategoria.Egreso;
        public int FamiliaId { get; set; }

        public virtual Familia Familia { get; set; }
        public virtual ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
        public virtual ICollection<Presupuesto> Presupuestos { get; set; } = new List<Presupuesto>();
    }

    public class Transaccion : BaseEntity
    {
        public string Concepto { get; set; }
        public decimal Monto { get; set; }
        public Moneda Moneda { get; set; } = Moneda.DOP;
        public decimal TasaCambioAplicada { get; set; } = 1.0m;
        public decimal MontoEnDOP { get; set; } // Monto normalizado en DOP para agregaciones
        public TipoTransaccion Tipo { get; set; } = TipoTransaccion.Egreso;
        public DateTime FechaTransaccion { get; set; } = DateTime.UtcNow;

        public int FamiliaId { get; set; }
        public int? FuenteIngresoId { get; set; }
        public int? CuentaOrigenId { get; set; }
        public int? CuentaDestinoId { get; set; } // Si es transferencia interna
        public int? CategoriaId { get; set; }
        public string UsuarioRegistradorId { get; set; }
        public string NombreUsuarioRegistrador { get; set; }

        public string Comentario { get; set; }
        public string ComprobanteUrl { get; set; }

        public virtual Familia Familia { get; set; }
        public virtual FuenteIngreso FuenteIngreso { get; set; }
        public virtual Cuenta CuentaOrigen { get; set; }
        public virtual Cuenta CuentaDestino { get; set; }
        public virtual Categoria Categoria { get; set; }
    }

    public class Presupuesto : BaseEntity
    {
        public string Nombre { get; set; }
        public decimal MontoLimite { get; set; }
        public Moneda Moneda { get; set; } = Moneda.DOP;
        public int Mes { get; set; }
        public int Anio { get; set; }
        public int FamiliaId { get; set; }
        public int? CategoriaId { get; set; }
        public int? FuenteIngresoId { get; set; }

        public virtual Familia Familia { get; set; }
        public virtual Categoria Categoria { get; set; }
        public virtual FuenteIngreso FuenteIngreso { get; set; }
    }

    public class TasaCambio : BaseEntity
    {
        public int FamiliaId { get; set; }
        public decimal TasaDOPporUSD { get; set; }
        public DateTime FechaVigencia { get; set; } = DateTime.UtcNow;
        public string UsuarioRegistradorId { get; set; }

        public virtual Familia Familia { get; set; }
    }

    public class AuditLog : BaseEntity
    {
        public string Entidad { get; set; }
        public string Accion { get; set; } // INSERT, UPDATE, DELETE
        public string RegistroId { get; set; }
        public string ValoresAnterioresJson { get; set; }
        public string ValoresNuevosJson { get; set; }
        public string UsuarioId { get; set; }
        public string NombreUsuario { get; set; }
        public string DireccionIP { get; set; }
        public int? FamiliaId { get; set; }
        public string Detalles { get; set; }
    }
}
