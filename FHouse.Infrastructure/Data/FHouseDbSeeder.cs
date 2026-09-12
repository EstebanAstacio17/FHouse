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

            // Seeder: Solo asegura categorías base si no existen para familias creadas, sin inyectar usuarios de prueba hardcodeados.

            // Asegurar categorías base para familias existentes que no tengan categorías
            var familias = await context.Familias.ToListAsync();
            foreach (var familia in familias)
            {
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
                        new Categoria { Nombre = "Ventas y Negocios", Tipo = TipoCategoria.Ingreso, Icono = "trending-up", Color = "#2997FF", FamiliaId = familia.Id },
                        new Categoria { Nombre = "Rentas e Inversiones", Tipo = TipoCategoria.Ingreso, Icono = "dollar-sign", Color = "#BF5AF2", FamiliaId = familia.Id }
                    };
                    context.Categorias.AddRange(categorias);
                }
            }
            await context.SaveChangesAsync();

            // 5. Limpieza preventiva de datos demo no deseados
            var fuentesDummy = await context.FuentesIngreso.Where(f => f.Nombre == "Portadorza Corp" && !context.Transacciones.Any(t => t.FuenteIngresoId == f.Id)).ToListAsync();
            if (fuentesDummy.Any())
            {
                var fuenteIds = fuentesDummy.Select(f => f.Id).ToList();
                var cuentasVinculadas = await context.Cuentas.Where(c => c.FuenteIngresoId.HasValue && fuenteIds.Contains(c.FuenteIngresoId.Value)).ToListAsync();
                foreach (var c in cuentasVinculadas)
                {
                    c.FuenteIngresoId = null;
                }
                context.FuentesIngreso.RemoveRange(fuentesDummy);
                await context.SaveChangesAsync();
            }
        }
    }
}
