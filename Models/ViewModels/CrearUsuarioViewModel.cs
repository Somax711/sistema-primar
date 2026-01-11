using System.ComponentModel.DataAnnotations;

namespace RendicionesPrimar.Models.ViewModels
{
    public class CrearUsuarioViewModel
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
        [Display(Name = "Nombre")]
        public string? Nombre { get; set; }

        [Required(ErrorMessage = "Los apellidos son requeridos")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden tener más de 100 caracteres")]
        [Display(Name = "Apellidos")]
        public string? Apellidos { get; set; }

        [Required(ErrorMessage = "El RUT es requerido")]
        [StringLength(20, ErrorMessage = "El RUT no puede tener más de 20 caracteres")]
        [Display(Name = "RUT")]
        public string? Rut { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede tener más de 100 caracteres")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(20, ErrorMessage = "El teléfono no puede tener más de 20 caracteres")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [Display(Name = "Contraseña")]
        public string? Password { get; set; }

        [Display(Name = "Rol")]
        public string? Rol { get; set; }

        [StringLength(100, ErrorMessage = "El cargo no puede tener más de 100 caracteres")]
        [Display(Name = "Cargo")]
        public string? Cargo { get; set; }

        [StringLength(100, ErrorMessage = "El departamento no puede tener más de 100 caracteres")]
        [Display(Name = "Departamento")]
        public string? Departamento { get; set; }
    }
} 