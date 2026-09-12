namespace FHouse.Core.Enums
{
    public enum TipoTransaccion
    {
        Ingreso = 1,
        Egreso = 2
    }

    public enum Moneda
    {
        DOP = 1, // Peso Dominicano (RD$)
        USD = 2  // Dólar Americano ($)
    }

    public enum TipoFuenteIngreso
    {
        Negocio = 1,
        Empleo = 2,
        Renta = 3,
        Inversion = 4,
        Freelance = 5,
        Otro = 6
    }

    public enum TipoCuenta
    {
        Corriente = 1,
        Ahorro = 2,
        Efectivo = 3,
        TarjetaCredito = 4,
        BilleteraDigital = 5
    }

    public enum RolFamilia
    {
        Admin = 1,
        Contador = 2,
        Miembro = 3,
        Limitado = 4
    }

    public enum TipoCategoria
    {
        Egreso = 1,
        Ingreso = 2,
        Ambos = 3
    }
}
