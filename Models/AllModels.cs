using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RendicionesPrimar.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        
        [StringLength(100)]
        public string? Nombre { get; set; }
        
        [StringLength(100)]
        public string? Apellidos { get; set; }
        
        [StringLength(20)]
        public string? Rut { get; set; }
        
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(20)]
        public string? Telefono { get; set; }
        
        public string? PasswordHash { get; set; }
        
        [StringLength(50)]
        public string? Rol { get; set; }
        
        [StringLength(100)]
        public string? Cargo { get; set; }
        
        [StringLength(100)]
        public string? Departamento { get; set; }
        
        public bool Activo { get; set; } = true;
        
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        
        // Campos para recuperación de contraseña
        [StringLength(64)]
        public string? CodigoRecuperacion { get; set; }
        
        public DateTime? CodigoRecuperacionExpira { get; set; }
        
        // Campos MFA
        [StringLength(6)]
        public string? MfaCodigoVerificacion { get; set; }
        
        public DateTime? MfaCodigoExpira { get; set; }
        
        // Propiedades de navegación
        public ICollection<Rendicion> Rendiciones { get; set; } = new List<Rendicion>();
        public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
    }

    public class Rendicion
    {
        public int Id { get; set; }

        [StringLength(20)]
        public string? NumeroTicket { get; set; }

        public int UsuarioId { get; set; }

        [StringLength(200)]
        public string? Titulo { get; set; }

        [StringLength(1000)]
        public string? Descripcion { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoTotal { get; set; }

        [StringLength(50)]
        public string? Estado { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Campos para aprobaciones
        [Column("aprobador_1_id")]
        public int? Aprobador1Id { get; set; }
        public DateTime? FechaAprobacion1 { get; set; }

        [Column("aprobador_2_id")]
        public int? Aprobador2Id { get; set; }
        public DateTime? FechaAprobacion2 { get; set; }

        public DateTime? FechaPago { get; set; }

        [Column("comentarios_aprobador")]
        [StringLength(1000)]
        public string? ComentariosAprobador { get; set; }

        // Información personal del empleado al momento de la rendición
        [StringLength(100)]
        [Column("nombre")]
        public string? Nombre { get; set; }

        [StringLength(100)]
        [Column("apellidos")]
        public string? Apellidos { get; set; }

        [StringLength(20)]
        public string? Rut { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        public string? Cargo { get; set; }

        [StringLength(100)]
        public string? Departamento { get; set; }

        // Propiedades de navegación
        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; } = null!;
        
        [ForeignKey("Aprobador1Id")]
        public Usuario? Aprobador1 { get; set; }
        
        [ForeignKey("Aprobador2Id")]
        public Usuario? Aprobador2 { get; set; }
        
        public ICollection<ArchivoAdjunto> ArchivosAdjuntos { get; set; } = new List<ArchivoAdjunto>();
        public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();

        public string EstadoLegible
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Estado)) return "Desconocido";
                return Estado switch
                {
                    "pendiente" => "Pendiente",
                    "aprobado_1" => "Aceptada",
                    "aprobado_2" => "Aceptada",
                    "pagado" => "Aceptada",
                    "rechazado" => "Rechazada",
                    _ => "Desconocido"
                };
            }
        }

        public string? MotivoRechazoSupervisor { get; set; }
        public string? MotivoRechazoGerente { get; set; }
        public bool EliminadaPorEmpleado { get; set; } = false;
    }
    
    public class Notificacion
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int RendicionId { get; set; }
        
        [StringLength(500)]
        public string? Mensaje { get; set; }
        
        public bool Leido { get; set; } = false;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        
        [StringLength(50)]
        public string? TipoRol { get; set; }
        
        // Propiedades de navegación
        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; } = null!;
        
        [ForeignKey("RendicionId")]
        public Rendicion Rendicion { get; set; } = null!;
    }
    
    public class ArchivoAdjunto
    {
        public int Id { get; set; }
        public int RendicionId { get; set; }
        
        [StringLength(255)]
        public string? NombreArchivo { get; set; }
        
        [StringLength(500)]
        public string? RutaArchivo { get; set; }
        
        [StringLength(100)]
        public string? TipoArchivo { get; set; }
        
        public long TamanoArchivo { get; set; }
        public DateTime FechaSubida { get; set; } = DateTime.Now;
        
        // Propiedades de navegación
        [ForeignKey("RendicionId")]
        public Rendicion Rendicion { get; set; } = null!;
    }
}