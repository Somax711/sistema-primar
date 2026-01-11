using System.ComponentModel.DataAnnotations;

namespace RendicionesPrimar.Models.ViewModels
{
    public class AprobarRendicionViewModel
    {
        public int RendicionId { get; set; }
        
        [StringLength(500, ErrorMessage = "Los comentarios no pueden exceder 500 caracteres")]
        public string Comentarios { get; set; } = string.Empty;
        
        public bool Aprobar { get; set; } = true;
    }
}
