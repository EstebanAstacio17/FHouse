using System;
using System.ComponentModel.DataAnnotations;

namespace FHouse.Services.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool Recordarme { get; set; }

        public string ReturnUrl { get; set; }
    }

    public class RegisterDto
    {
        [Required(ErrorMessage = "El nombre completo es requerido.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Debe confirmar su contraseña.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }

        // Si es una nueva familia
        public string NombreFamilia { get; set; }

        // Si se une con código de invitación
        public string CodigoInvitacion { get; set; }
    }

    public class UsuarioPerfilDto
    {
        public string Id { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int FamiliaId { get; set; }
        public string NombreFamilia { get; set; }
        public string Rol { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class ActualizarPerfilDto
    {
        [Required(ErrorMessage = "El nombre completo es requerido.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
        public string Email { get; set; }

        public string PhoneNumber { get; set; }
    }

    public class CambiarPasswordDto
    {
        [Required(ErrorMessage = "La contraseña actual es requerida.")]
        [DataType(DataType.Password)]
        public string PasswordActual { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es requerida.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string NuevoPassword { get; set; }

        [Required(ErrorMessage = "Debe confirmar su nueva contraseña.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public string ConfirmarPassword
        {
            get => ConfirmPassword;
            set => ConfirmPassword = value;
        }
    }

    public class ActualizarMiembroDto
    {
        public int Id { get; set; }
        public int MiembroId
        {
            get => Id;
            set => Id = value;
        }
        public string AliasFamiliar { get; set; }
        public FHouse.Core.Enums.RolFamilia Rol { get; set; }
        public bool Activo { get; set; } = true;
    }
}
