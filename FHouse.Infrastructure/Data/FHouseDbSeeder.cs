using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using FHouse.Core.Entities;
using FHouse.Core.Enums;
using FHouse.Infrastructure.Identity;
using Microsoft.AspNet.Identity;

namespace FHouse.Infrastructure.Data
{
    public static class FHouseDbSeeder
    {
        public static async Task SeedAsync(FHouseDbContext context)
        {
            if (context == null) return;

            // 1. Asegurar Familia inicial
            var familia = await context.Familias.FirstOrDefaultAsync(f => f.Id == 1);
            if (familia == null)
            {
                familia = new Familia
                {
                    Nombre = "Familia Gómez",
                    CodigoInvitacion = "FH-GOMEZ1",
                    TasaCambioActual = 60.50m,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };
                context.Familias.Add(familia);
                await context.SaveChangesAsync();
            }

            // 2. Asegurar Usuario Admin por defecto
            var hasher = new PasswordHasher();
            var adminEmail = "admin@fhouse.com";
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    Id = "usr-admin-001",
                    UserName = adminEmail,
                    Email = adminEmail,
                    NombreCompleto = "Esteban Gómez",
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    PasswordHash = hasher.HashPassword("Admin123!"),
                    FechaRegistro = DateTime.UtcNow,
                    FamiliaActualId = familia.Id
                };
                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
            }

            // 3. Relación Usuario-Familia
            var usuarioFamilia = await context.UsuariosFamilia.FirstOrDefaultAsync(uf => uf.UsuarioId == adminUser.Id && uf.FamiliaId == familia.Id);
            if (usuarioFamilia == null)
            {
                usuarioFamilia = new UsuarioFamilia
                {
                    UsuarioId = adminUser.Id,
                    FamiliaId = familia.Id,
                    Rol = RolFamilia.Admin,
                    AliasFamiliar = "Administrador Principal",
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };
                context.UsuariosFamilia.Add(usuarioFamilia);
                await context.SaveChangesAsync();
            }

            // 4. Categorías base
            if (!await context.Categorias.AnyAsync(c => c.FamiliaId == familia.Id))
            {
                var categorias = new[]
                {
                    new Categoria { Nombre = "Supermercado y Alimentación", Tipo = TipoCategoria.Egreso, Icono = "shopping-cart", Color = "#34C759", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Vivienda y Servicios", Tipo = TipoCategoria.Egreso, Icono = "home", Color = "#0071E3", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Combustible y Transporte", Tipo = TipoCategoria.Egreso, Icono = "car", Color = "#FF9500", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Salud y Medicamentos", Tipo = TipoCategoria.Egreso, Icono = "heart", Color = "#FF3B30", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Educación y Cursos", Tipo = TipoCategoria.Egreso, Icono = "book-open", Color = "#AF52DE", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Salario y Honorarios", Tipo = TipoCategoria.Ingreso, Icono = "briefcase", Color = "#30D158", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Ventas Negocio Portadorza", Tipo = TipoCategoria.Ingreso, Icono = "trending-up", Color = "#2997FF", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Rentas e Inversiones", Tipo = TipoCategoria.Ingreso, Icono = "dollar-sign", Color = "#BF5AF2", FamiliaId = familia.Id }
                };
                context.Categorias.AddRange(categorias);
                await context.SaveChangesAsync();
            }

            // 5. Fuente de Ingreso inicial
            var fuente = await context.FuentesIngreso.FirstOrDefaultAsync(f => f.FamiliaId == familia.Id);
            if (fuente == null)
            {
                fuente = new FuenteIngreso
                {
                    Nombre = "Portadorza Corp",
                    Tipo = TipoFuenteIngreso.Negocio,
                    Descripcion = "Negocio principal de servicios y comercio tecnológico.",
                    ColorIdentificador = "#0071E3",
                    FamiliaId = familia.Id,
                    UsuarioCreadorId = adminUser.Id,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };
                context.FuentesIngreso.Add(fuente);
                await context.SaveChangesAsync();
            }

            // 6. Cuentas bancarias iniciales
            if (!await context.Cuentas.AnyAsync(c => c.FamiliaId == familia.Id))
            {
                var cuentaDOP = new Cuenta
                {
                    Nombre = "Banco Popular Corriente",
                    InstitucionFinanciera = "Banco Popular",
                    Moneda = Moneda.DOP,
                    Tipo = TipoCuenta.Corriente,
                    SaldoActual = 0m,
                    NumeroCuenta = "•••• 4589",
                    FamiliaId = familia.Id,
                    FuenteIngresoId = fuente.Id,
                    UsuarioResponsableId = adminUser.Id,
                    Activo = true
                };

                var cuentaUSD = new Cuenta
                {
                    Nombre = "BHD Ahorros USD",
                    InstitucionFinanciera = "Banco BHD",
                    Moneda = Moneda.USD,
                    Tipo = TipoCuenta.Ahorro,
                    SaldoActual = 0m,
                    NumeroCuenta = "•••• 8921",
                    FamiliaId = familia.Id,
                    UsuarioResponsableId = adminUser.Id,
                    Activo = true
                };

                context.Cuentas.Add(cuentaDOP);
                context.Cuentas.Add(cuentaUSD);
                await context.SaveChangesAsync();
            }
        }
    }
}
