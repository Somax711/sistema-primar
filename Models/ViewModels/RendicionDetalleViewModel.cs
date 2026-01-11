namespace RendicionesPrimar.Models.ViewModels
{
    public class RendicionDetalleViewModel
    {
        public int Id { get; set; }
        public string NumeroTicket { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public string Apellidos { get; set; } = string.Empty;
        public string Rut { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Cargo { get; set; }
        public string? Departamento { get; set; }
        public string? ComentariosAprobador { get; set; }
        public Usuario Usuario { get; set; } = null!;
        public List<ArchivoAdjunto> ArchivosAdjuntos { get; set; } = new List<ArchivoAdjunto>();
        
        // Propiedades de control
        public bool CanApprove1 { get; set; }
        public bool CanApprove2 { get; set; }
        public bool CanMarkPaid { get; set; }
        public bool CanEdit { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public List<string> TimelineEvents { get; set; } = new List<string>();
        public string Nombre { get; set; } = string.Empty;
        public string? MotivoRechazoSupervisor { get; set; }
        public string? MotivoRechazoGerente { get; set; }
    }
}