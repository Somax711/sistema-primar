using System.ComponentModel.DataAnnotations;

namespace RendicionesPrimar.Models.ViewModels
{
    public class InformacionPersonalViewModel
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son requeridos")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden exceder 100 caracteres")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El RUT es requerido")]
        [StringLength(20, ErrorMessage = "El RUT no puede exceder 20 caracteres")]
        [Display(Name = "RUT")]
        public string Rut { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [StringLength(100, ErrorMessage = "El cargo no puede exceder 100 caracteres")]
        [Display(Name = "Cargo")]
        public string? Cargo { get; set; }

        [StringLength(100, ErrorMessage = "El departamento no puede exceder 100 caracteres")]
        [Display(Name = "Departamento")]
        public string? Departamento { get; set; }
    }
} 