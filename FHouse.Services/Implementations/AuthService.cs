using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FHouse.Core.Common;
using FHouse.Core.Entities;
using FHouse.Core.Enums;
using FHouse.Infrastructure.Data;
using FHouse.Infrastructure.Identity;
using FHouse.Services.Contracts;
using FHouse.Services.DTOs;
using Microsoft.AspNet.Identity;

namespace FHouse.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly FHouseDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(FHouseDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _passwordHasher = new PasswordHasher();
        }

        public async Task<ResultadoOperacion<ClaimsIdentity>> ValidarLoginAsync(LoginDto dto)
        {
            if (dto == null) return ResultadoOperacion<ClaimsIdentity>.Falla("Datos inválidos.");

            var emailNormalized = dto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized || u.UserName.ToLower() == emailNormalized);

            if (user == null)
            {
                return ResultadoOperacion<ClaimsIdentity>.Falla("Correo electrónico o contraseña incorrectos.");
            }

            // Brute-force & Lockout Protection
            if (user.LockoutEnabled && user.LockoutEndDateUtc.HasValue && user.LockoutEndDateUtc.Value > DateTime.UtcNow)
            {
                var minutosRestantes = Math.Ceiling((user.LockoutEndDateUtc.Value - DateTime.UtcNow).TotalMinutes);
                return ResultadoOperacion<ClaimsIdentity>.Falla($"Cuenta temporalmente bloqueada por múltiples intentos fallidos. Intente nuevamente en {minutosRestantes} minuto(s).");
            }

            var passwordResult = _passwordHasher.VerifyHashedPassword(user.PasswordHash, dto.Password);
            if (passwordResult == PasswordVerificationResult.Failed)
            {
                user.AccessFailedCount++;
                if (user.AccessFailedCount >= 5)
                {
                    user.LockoutEnabled = true;
                    user.LockoutEndDateUtc = DateTime.UtcNow.AddMinutes(15);
                }
                await _context.SaveChangesAsync();
                return ResultadoOperacion<ClaimsIdentity>.Falla("Correo electrónico o contraseña incorrectos.");
            }

            // Reset failed count on successful authentication
            if (user.AccessFailedCount > 0 || user.LockoutEndDateUtc.HasValue)
            {
                user.AccessFailedCount = 0;
                user.LockoutEndDateUtc = null;
            }
            user.UltimoAcceso = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Obtener relación familiar
            var usuarioFamilia = await _context.UsuariosFamilia
                .Include(uf => uf.Familia)
                .FirstOrDefaultAsync(uf => uf.UsuarioId == user.Id);

            if (usuarioFamilia != null && !usuarioFamilia.Activo)
            {
                return ResultadoOperacion<ClaimsIdentity>.Falla("Tu cuenta se encuentra pendiente de aprobación por un administrador. No podrás ingresar a la plataforma hasta que tu acceso sea autorizado y se te asigne un rol.");
            }

            int familiaId = 1;
            string familiaNombre = "Mi Familia";
            string rol = "Miembro";

            if (usuarioFamilia != null)
            {
                familiaId = usuarioFamilia.FamiliaId;
                familiaNombre = usuarioFamilia.Familia?.Nombre;
                rol = usuarioFamilia.Rol.ToString();
            }
            else if (user.FamiliaActualId.HasValue)
            {
                var fam = await _context.Familias.FirstOrDefaultAsync(f => f.Id == user.FamiliaActualId.Value);
                if (fam != null)
                {
                    familiaId = fam.Id;
                    familiaNombre = fam.Nombre;
                }
            }

            if (string.IsNullOrEmpty(familiaNombre))
            {
                var fam = await _context.Familias.FirstOrDefaultAsync(f => f.Id == familiaId);
                familiaNombre = fam?.Nombre ?? "Mi Familia";
            }

            // Crear ClaimsIdentity para la cookie de autenticación
            var identity = new ClaimsIdentity(DefaultAuthenticationTypes.ApplicationCookie);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
            identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            identity.AddClaim(new Claim("NombreCompleto", user.NombreCompleto ?? user.UserName));
            identity.AddClaim(new Claim("FamiliaId", familiaId.ToString()));
            identity.AddClaim(new Claim("FamiliaNombre", familiaNombre));
            identity.AddClaim(new Claim(ClaimTypes.Role, rol));

            return ResultadoOperacion<ClaimsIdentity>.Ok(identity, "Inicio de sesión exitoso.");
        }

        public async Task<ResultadoOperacion<ClaimsIdentity>> RegistrarUsuarioAsync(RegisterDto dto)
        {
            if (dto == null) return ResultadoOperacion<ClaimsIdentity>.Falla("Datos inválidos.");

            var emailNormalized = dto.Email.Trim().ToLowerInvariant();
            var existe = await _context.Users.AnyAsync(u => u.Email.ToLower() == emailNormalized);
            if (existe)
            {
                return ResultadoOperacion<ClaimsIdentity>.Falla("Ya existe una cuenta registrada con este correo electrónico.");
            }

            // Verificar si ya existen usuarios activos en la plataforma
            bool hayUsuariosActivos = await _context.UsuariosFamilia.AnyAsync(uf => uf.Activo);

            // RESILIENCE: Validate invitation code FIRST before creating any entities
            Familia familia = null;
            RolFamilia rol = hayUsuariosActivos ? RolFamilia.Miembro : RolFamilia.Admin;

            if (!string.IsNullOrWhiteSpace(dto.CodigoInvitacion))
            {
                var codigo = dto.CodigoInvitacion.Trim().ToUpperInvariant();
                familia = await _context.Familias.FirstOrDefaultAsync(f => f.CodigoInvitacion == codigo && f.Activo);
                if (familia == null)
                {
                    return ResultadoOperacion<ClaimsIdentity>.Falla("El código de invitación ingresado no es válido o la familia no existe.");
                }
            }

            // Only now create the user — after all validations passed
            var userId = Guid.NewGuid().ToString();
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = emailNormalized,
                Email = emailNormalized,
                NombreCompleto = dto.NombreCompleto.Trim(),
                FechaRegistro = DateTime.UtcNow,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                PasswordHash = _passwordHasher.HashPassword(dto.Password)
            };

            _context.Users.Add(user);

            if (familia == null)
            {
                // Create new family
                var nombreFam = string.IsNullOrWhiteSpace(dto.NombreFamilia) ? $"Familia {dto.NombreCompleto.Trim()}" : dto.NombreFamilia.Trim();
                var codInvitacion = "FH-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpperInvariant();

                familia = new Familia
                {
                    Nombre = nombreFam,
                    CodigoInvitacion = codInvitacion,
                    TasaCambioActual = 60.50m,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };

                _context.Familias.Add(familia);
                await _context.SaveChangesAsync();

                // Seed default categories for the new family
                var categoriasDefault = new[]
                {
                    new Categoria { Nombre = "Alimentación y Supermercado", Tipo = TipoCategoria.Egreso, Icono = "shopping-cart", Color = "#34C759", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Vivienda y Servicios", Tipo = TipoCategoria.Egreso, Icono = "home", Color = "#0071E3", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Transporte y Combustible", Tipo = TipoCategoria.Egreso, Icono = "car", Color = "#FF9500", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Salud y Bienestar", Tipo = TipoCategoria.Egreso, Icono = "heart", Color = "#FF3B30", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Educación", Tipo = TipoCategoria.Egreso, Icono = "book-open", Color = "#AF52DE", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Salario / Nómina", Tipo = TipoCategoria.Ingreso, Icono = "briefcase", Color = "#30D158", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Negocio / Ventas", Tipo = TipoCategoria.Ingreso, Icono = "trending-up", Color = "#2997FF", FamiliaId = familia.Id },
                    new Categoria { Nombre = "Rentas / Inversiones", Tipo = TipoCategoria.Ingreso, Icono = "dollar-sign", Color = "#BF5AF2", FamiliaId = familia.Id }
                };

                _context.Categorias.AddRange(categoriasDefault);

                // Create default account
                var cuentaDefault = new Cuenta
                {
                    Nombre = "Cuenta Principal (DOP)",
                    InstitucionFinanciera = "Banco Popular",
                    Moneda = Moneda.DOP,
                    Tipo = TipoCuenta.Ahorro,
                    SaldoActual = 0m,
                    NumeroCuenta = "•••• 1001",
                    FamiliaId = familia.Id,
                    UsuarioResponsableId = userId,
                    Activo = true
                };
                _context.Cuentas.Add(cuentaDefault);
            }

            // 3. Create family member relationship
            // Si ya existen usuarios activos en la plataforma, la cuenta se crea INACTIVA (pendiente de aprobación)
            bool cuentaActivaInicial = !hayUsuariosActivos;

            var usuarioFamilia = new UsuarioFamilia
            {
                UsuarioId = userId,
                FamiliaId = familia.Id,
                Rol = rol,
                AliasFamiliar = rol == RolFamilia.Admin ? "Administrador" : "Miembro",
                FechaCreacion = DateTime.UtcNow,
                Activo = cuentaActivaInicial
            };

            _context.UsuariosFamilia.Add(usuarioFamilia);
            user.FamiliaActualId = familia.Id;

            await _context.SaveChangesAsync();

            // Si requiere aprobación por un usuario existente, no se emite ticket de sesión
            if (!cuentaActivaInicial)
            {
                return ResultadoOperacion<ClaimsIdentity>.Ok(
                    null, 
                    "Tu cuenta ha sido creada exitosamente. Dado que ya existen usuarios activos en la plataforma, tu registro se encuentra pendiente de aprobación. Un administrador o usuario activo debe autorizar tu acceso y asignarte tu rol antes de que puedas ingresar."
                );
            }

            // 4. Build ClaimsIdentity (solo para el primer usuario administrador inicial)
            var identity = new ClaimsIdentity(DefaultAuthenticationTypes.ApplicationCookie);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
            identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            identity.AddClaim(new Claim("NombreCompleto", user.NombreCompleto));
            identity.AddClaim(new Claim("FamiliaId", familia.Id.ToString()));
            identity.AddClaim(new Claim("FamiliaNombre", familia.Nombre));
            identity.AddClaim(new Claim(ClaimTypes.Role, rol.ToString()));

            return ResultadoOperacion<ClaimsIdentity>.Ok(identity, "¡Cuenta creada exitosamente como Administrador Principal! Bienvenido a F House.");
        }

        public async Task<ResultadoOperacion<UsuarioPerfilDto>> ObtenerPerfilUsuarioAsync(string usuarioId)
        {
            if (string.IsNullOrWhiteSpace(usuarioId)) return ResultadoOperacion<UsuarioPerfilDto>.Falla("Usuario no especificado.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (user == null) return ResultadoOperacion<UsuarioPerfilDto>.Falla("Usuario no encontrado.");

            var uf = await _context.UsuariosFamilia
                .Include(u => u.Familia)
                .FirstOrDefaultAsync(u => u.UsuarioId == user.Id && u.Activo);

            int famId = uf?.FamiliaId ?? user.FamiliaActualId ?? 1;
            string famNombre = uf?.Familia?.Nombre;
            if (string.IsNullOrEmpty(famNombre))
            {
                var fam = await _context.Familias.FirstOrDefaultAsync(f => f.Id == famId);
                famNombre = fam?.Nombre ?? "Mi Familia";
            }

            var perfil = new UsuarioPerfilDto
            {
                Id = user.Id,
                NombreCompleto = user.NombreCompleto ?? user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FamiliaId = famId,
                NombreFamilia = famNombre,
                Rol = uf?.Rol.ToString() ?? "Admin",
                FechaRegistro = user.FechaRegistro
            };

            return ResultadoOperacion<UsuarioPerfilDto>.Ok(perfil);
        }

        public async Task<ResultadoOperacion<bool>> ActualizarPerfilAsync(string usuarioId, ActualizarPerfilDto dto)
        {
            if (string.IsNullOrWhiteSpace(usuarioId)) return ResultadoOperacion<bool>.Falla("Usuario no especificado.");
            if (dto == null) return ResultadoOperacion<bool>.Falla("Datos inválidos.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (user == null) return ResultadoOperacion<bool>.Falla("Usuario no encontrado.");

            var emailNormalized = dto.Email.Trim().ToLowerInvariant();
            if (user.Email.ToLower() != emailNormalized)
            {
                var yaExiste = await _context.Users.AnyAsync(u => u.Id != usuarioId && u.Email.ToLower() == emailNormalized);
                if (yaExiste)
                {
                    return ResultadoOperacion<bool>.Falla("El correo electrónico ya está registrado por otro usuario.");
                }
                user.Email = emailNormalized;
                user.UserName = emailNormalized;
            }

            user.NombreCompleto = dto.NombreCompleto.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();

            await _context.SaveChangesAsync();
            return ResultadoOperacion<bool>.Ok(true, "Perfil actualizado correctamente.");
        }

        public async Task<ResultadoOperacion<bool>> CambiarPasswordAsync(string usuarioId, CambiarPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(usuarioId)) return ResultadoOperacion<bool>.Falla("Usuario no especificado.");
            if (dto == null) return ResultadoOperacion<bool>.Falla("Datos inválidos.");

            if (dto.NuevoPassword != dto.ConfirmarPassword)
            {
                return ResultadoOperacion<bool>.Falla("La nueva contraseña y la confirmación no coinciden.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (user == null) return ResultadoOperacion<bool>.Falla("Usuario no encontrado.");

            var passwordResult = _passwordHasher.VerifyHashedPassword(user.PasswordHash, dto.PasswordActual);
            if (passwordResult == PasswordVerificationResult.Failed)
            {
                return ResultadoOperacion<bool>.Falla("La contraseña actual es incorrecta.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(dto.NuevoPassword);
            user.SecurityStamp = Guid.NewGuid().ToString();

            await _context.SaveChangesAsync();
            return ResultadoOperacion<bool>.Ok(true, "Contraseña cambiada exitosamente.");
        }
    }
}
