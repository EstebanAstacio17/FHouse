using System;

namespace FHouse.Core.Entities
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaModificacion { get; set; }
        public bool Activo { get; set; } = true;
    }
}
