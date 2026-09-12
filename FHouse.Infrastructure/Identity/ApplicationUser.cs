using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace FHouse.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string NombreCompleto { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public DateTime? UltimoAcceso { get; set; }
        public int? FamiliaActualId { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            userIdentity.AddClaim(new Claim("NombreCompleto", NombreCompleto ?? UserName));
            if (FamiliaActualId.HasValue)
            {
                userIdentity.AddClaim(new Claim("FamiliaId", FamiliaActualId.Value.ToString()));
            }
            return userIdentity;
        }
    }
}
