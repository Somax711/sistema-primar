using Microsoft.EntityFrameworkCore;
using RendicionesPrimar.Models;

namespace RendicionesPrimar.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rendicion> Rendiciones { get; set; }
        public DbSet<ArchivoAdjunto> ArchivosAdjuntos { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuración para Usuarios
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("usuarios");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(100);
                entity.Property(e => e.Apellidos).HasColumnName("apellidos").HasMaxLength(100);
                entity.Property(e => e.Rut).HasColumnName("rut").HasMaxLength(20);
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
                entity.Property(e => e.Telefono).HasColumnName("telefono").HasMaxLength(20);
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.Rol).HasColumnName("rol").HasMaxLength(50);
                entity.Property(e => e.Cargo).HasColumnName("cargo").HasMaxLength(100);
                entity.Property(e => e.Departamento).HasColumnName("departamento").HasMaxLength(100);
                entity.Property(e => e.Activo).HasColumnName("activo");
                entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
                
                // Campos MFA
                entity.Property(e => e.MfaCodigoVerificacion).HasColumnName("mfa_codigo_verificacion").HasMaxLength(6);
                entity.Property(e => e.MfaCodigoExpira).HasColumnName("mfa_codigo_expira");
                // Ignorar campos de recuperación que no existen en la BD
                entity.Ignore(e => e.CodigoRecuperacion);
                entity.Ignore(e => e.CodigoRecuperacionExpira);
            });

            // Configuración para Rendiciones - SIN RELACIONES NAVEGACIONALES
            modelBuilder.Entity<Rendicion>(entity =>
            {
                entity.ToTable("rendiciones");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.NumeroTicket).HasColumnName("numero_ticket").HasMaxLength(20);
                entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
                entity.Property(e => e.Titulo).HasColumnName("titulo").HasMaxLength(200);
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(1000);
                entity.Property(e => e.MontoTotal).HasColumnName("monto_total").HasColumnType("decimal(18,2)");
                entity.Property(e => e.Estado).HasColumnName("estado").HasMaxLength(50);
                entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
                entity.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(100);
                entity.Property(e => e.Apellidos).HasColumnName("apellidos").HasMaxLength(100);
                entity.Property(e => e.Rut).HasColumnName("rut").HasMaxLength(20);
                entity.Property(e => e.Telefono).HasColumnName("telefono").HasMaxLength(20);
                entity.Property(e => e.Cargo).HasColumnName("cargo").HasMaxLength(100);
                entity.Property(e => e.Departamento).HasColumnName("departamento").HasMaxLength(100);
                entity.Property(e => e.ComentariosAprobador).HasColumnName("comentarios_aprobador").HasMaxLength(1000);
                entity.Property(e => e.Aprobador1Id).HasColumnName("aprobador_1_id");
                entity.Property(e => e.Aprobador2Id).HasColumnName("aprobador_2_id");
                entity.Property(e => e.FechaAprobacion1).HasColumnName("fecha_aprobacion_1");
                entity.Property(e => e.FechaAprobacion2).HasColumnName("fecha_aprobacion_2");
                entity.Property(e => e.FechaPago).HasColumnName("fecha_pago");
                entity.Property(e => e.MotivoRechazoSupervisor).HasColumnName("motivo_rechazo_supervisor").HasMaxLength(1000);
                entity.Property(e => e.MotivoRechazoGerente).HasColumnName("motivo_rechazo_gerente").HasMaxLength(1000);

                // Configurar relaciones navegacionales
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.Rendiciones)
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.Aprobador1)
                    .WithMany()
                    .HasForeignKey(e => e.Aprobador1Id)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.Aprobador2)
                    .WithMany()
                    .HasForeignKey(e => e.Aprobador2Id)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración para Notificaciones
            modelBuilder.Entity<Notificacion>(entity =>
            {
                entity.ToTable("notificaciones");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
                entity.Property(e => e.RendicionId).HasColumnName("rendicion_id");
                entity.Property(e => e.Mensaje).HasColumnName("mensaje").HasMaxLength(500);
                entity.Property(e => e.Leido).HasColumnName("leido");
                entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
                entity.Property(e => e.TipoRol).HasColumnName("tipo_rol").HasMaxLength(50);

                // Configurar relaciones navegacionales
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.Notificaciones)
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Rendicion)
                    .WithMany(r => r.Notificaciones)
                    .HasForeignKey(e => e.RendicionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración para ArchivosAdjuntos
            modelBuilder.Entity<ArchivoAdjunto>(entity =>
            {
                entity.ToTable("archivos_adjuntos");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.RendicionId).HasColumnName("rendicion_id");
                entity.Property(e => e.NombreArchivo).HasColumnName("nombre_archivo").HasMaxLength(255);
                entity.Property(e => e.RutaArchivo).HasColumnName("ruta_archivo").HasMaxLength(500);
                entity.Property(e => e.TipoArchivo).HasColumnName("tipo_archivo").HasMaxLength(100);
                entity.Property(e => e.TamanoArchivo).HasColumnName("tamano_archivo");
                entity.Property(e => e.FechaSubida).HasColumnName("fecha_subida");

                // Configurar relación navegacional
                entity.HasOne(e => e.Rendicion)
                    .WithMany(r => r.ArchivosAdjuntos)
                    .HasForeignKey(e => e.RendicionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Las relaciones navegacionales ya están configuradas en las entidades correspondientes

            base.OnModelCreating(modelBuilder);
        }
    }
}