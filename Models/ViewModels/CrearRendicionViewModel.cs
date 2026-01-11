using System.ComponentModel.DataAnnotations;

namespace RendicionesPrimar.Models.ViewModels
{
    public class CrearRendicionViewModel
    {
        // Propiedades para mostrar (solo lectura)
        public string? Nombre { get; set; }
        public string? Apellidos { get; set; }
        public string? Rut { get; set; }
        public string? Telefono { get; set; }
        public string? Cargo { get; set; }
        public string? Departamento { get; set; }

        // Propiedades para el formulario
        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(200, ErrorMessage = "El título no puede exceder 200 caracteres")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es requerida")]
        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto total es requerido")]
        [Range(0.01, 9999999999.99, ErrorMessage = "El monto debe ser mayor a cero.")]
        public decimal MontoTotal { get; set; }

        public List<IFormFile> Archivos { get; set; } = new List<IFormFile>();
    }
}