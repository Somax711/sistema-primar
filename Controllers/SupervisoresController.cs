using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RendicionesPrimar.Data;
using RendicionesPrimar.Models;
using RendicionesPrimar.Models.ViewModels;
using System.Security.Claims;
using RendicionesPrimar.Services;

namespace RendicionesPrimar.Controllers
{
    [Authorize(Roles = "aprobador1,aprobador2")]
    public class SupervisoresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RendicionService _rendicionService;
        private readonly EmailService _emailService; // Added EmailService

        public SupervisoresController(ApplicationDbContext context, RendicionService rendicionService, EmailService emailService) // Added EmailService to constructor
        {
            _context = context;
            _rendicionService = rendicionService;
            _emailService = emailService; // Initialize EmailService
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = await _context.Usuarios.FindAsync(userId);
            var nombreUsuario = usuario != null ? $"{usuario.Nombre} {usuario.Apellidos}".Trim() : "Usuario";
            ViewBag.UserName = nombreUsuario;

            // Contar notificaciones no leídas específicas de supervisores
            var notificacionesNoLeidas = await _context.Notificaciones
                .Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "supervisor")
                .CountAsync();

            ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;

            // Crear el modelo de dashboard para supervisor
            var modelo = new DashboardViewModel
            {
                UserName = nombreUsuario,
                NotificacionesNoLeidas = notificacionesNoLeidas,
                TotalRendiciones = await _context.Rendiciones
                    .Where(r => r.Estado == "pendiente" || r.Estado == "aprobado_2")
                    .CountAsync(),
                RendicionesPendientes = await _context.Rendiciones
                    .Where(r => r.Estado == "pendiente")
                    .CountAsync(),
                RendicionesAprobadas = await _context.Rendiciones
                    .Where(r => r.Estado == "aprobado_2")
                    .CountAsync(),
                MontoTotal = 0 // Los supervisores no ven montos totales
            };

            // Obtener últimas rendiciones pendientes de aprobación
            var ultimasRendiciones = await _context.Rendiciones
                .Include(r => r.Usuario)
                .Where(r => r.Estado == "pendiente" || r.Estado == "aprobado_2")
                .OrderByDescending(r => r.FechaCreacion)
                .Take(5)
                .ToListAsync();

            modelo.UltimasRendiciones = ultimasRendiciones;

            return View("DashboardSupervisor", modelo);
        }

        public async Task<IActionResult> RendicionesPendientes()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = await _context.Usuarios.FindAsync(userId);
            var nombreUsuario = usuario != null ? $"{usuario.Nombre} {usuario.Apellidos}".Trim() : "Usuario";
            ViewBag.UserName = nombreUsuario;
            ViewBag.NotificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "supervisor").CountAsync();
            var rendiciones = await _context.Rendiciones
                .Include(r => r.Usuario)
                .Where(r => r.Estado == "pendiente" || r.Estado == "aprobado_2")
                .OrderByDescending(r => r.FechaCreacion)
                .ToListAsync();
            return View("RendicionesPendientes", rendiciones);
        }

        [HttpGet]
        public async Task<IActionResult> Notificaciones()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = await _context.Usuarios.FindAsync(userId);
            var nombreUsuario = usuario != null ? $"{usuario.Nombre} {usuario.Apellidos}".Trim() : "Usuario";
            ViewBag.UserName = nombreUsuario;
            var notificaciones = await _context.Notificaciones
                .Where(n => n.UsuarioId == userId && n.TipoRol == "supervisor")
                .Include(n => n.Rendicion)
                .OrderByDescending(n => n.FechaCreacion)
                .ToListAsync();
            ViewBag.NotificacionesNoLeidas = notificaciones.Count(n => !n.Leido);
            ViewBag.UserId = userId;
            return View(notificaciones);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerEstadisticas()
        {
            var estadisticas = new
            {
                aprobadas = await _context.Rendiciones.Where(r => r.Estado == "aprobado_2").CountAsync(),
                pendientes = await _context.Rendiciones.Where(r => r.Estado == "pendiente").CountAsync()
            };
            
            return Json(estadisticas);
        }

        [HttpPost]
        public async Task<IActionResult> MarcarNotificacionLeida(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == userId && n.TipoRol == "supervisor");

            if (notificacion != null)
            {
                notificacion.Leido = true;
                await _context.SaveChangesAsync();
            }

            // Si es AJAX, responde JSON. Si no, redirige a la vista de notificaciones.
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }
            else
            {
                return RedirectToAction("Notificaciones", "Supervisores");
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarcarNotificacionLeidaYVer(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var notificacion = await _context.Notificaciones
                .Include(n => n.Rendicion)
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == userId && n.TipoRol == "supervisor");

            if (notificacion != null)
            {
                notificacion.Leido = true;
                await _context.SaveChangesAsync();

                var redirectUrl = Url.Action("Detalle", "Rendiciones", new { id = notificacion.RendicionId });

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, redirectUrl });
                }
                else
                {
                    return Redirect(redirectUrl);
                }
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Notificación no encontrada" });
            }
            else
            {
                return RedirectToAction("Notificaciones");
            }
        }

        // MÉTODOS PERFIL SIN ATRIBUTOS DE RUTA
        [AllowAnonymous]
        [HttpGet]
        [Route("/perfil-supervisor")]
        public async Task<IActionResult> PerfilFijo()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
                return Content("Usuario no encontrado");
            ViewBag.UserName = $"{usuario.Nombre} {usuario.Apellidos}".Trim();
            ViewBag.NotificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "supervisor").CountAsync();
            var viewModel = new InformacionPersonalViewModel
            {
                Nombre = usuario.Nombre ?? string.Empty,
                Apellidos = usuario.Apellidos ?? string.Empty,
                Rut = usuario.Rut ?? string.Empty,
                Email = usuario.Email ?? string.Empty,
                Cargo = usuario.Cargo ?? string.Empty,
                Departamento = usuario.Departamento ?? string.Empty,
                Telefono = usuario.Telefono ?? string.Empty
            };
            return View("Perfil", viewModel);
        }

        [Authorize(Roles = "aprobador1")]
        [HttpPost]
        [Route("/perfil-supervisor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PerfilFijo(InformacionPersonalViewModel model)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (!ModelState.IsValid)
                return Content("Modelo inválido");
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
                return Content("Usuario no encontrado");
            usuario.Nombre = model.Nombre;
            usuario.Apellidos = model.Apellidos;
            usuario.Rut = model.Rut;
            usuario.Email = model.Email;
            usuario.Telefono = model.Telefono;
            usuario.Cargo = model.Cargo;
            usuario.Departamento = model.Departamento;
            _context.Entry(usuario).Property(x => x.Nombre).IsModified = true;
            _context.Entry(usuario).Property(x => x.Apellidos).IsModified = true;
            _context.Entry(usuario).Property(x => x.Rut).IsModified = true;
            _context.Entry(usuario).Property(x => x.Email).IsModified = true;
            _context.Entry(usuario).Property(x => x.Telefono).IsModified = true;
            _context.Entry(usuario).Property(x => x.Cargo).IsModified = true;
            _context.Entry(usuario).Property(x => x.Departamento).IsModified = true;
            await _context.SaveChangesAsync();
            ViewBag.UserName = $"{usuario.Nombre} {usuario.Apellidos}".Trim();
            TempData["SuccessMessage"] = "¡Cambios guardados!";
            return RedirectToAction("PerfilFijo");
        }

        private async Task<string> ObtenerNombreCompletoUsuario(int userId)
        {
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario != null)
                return $"{usuario.Nombre} {usuario.Apellidos}".Trim();
            return "Usuario";
        }

        public IActionResult Ayuda()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = _context.Usuarios.Find(userId);
            var nombreUsuario = usuario != null ? $"{usuario.Nombre} {usuario.Apellidos}".Trim() : "Usuario";
            ViewBag.UserName = nombreUsuario;
            ViewBag.NotificacionesNoLeidas = _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "supervisor").Count();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AprobarRendicion(int id, string? comentarios)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var ok = await _rendicionService.AprobarPrimeraInstanciaAsync(id, userId, comentarios);
            if (!ok)
            {
                TempData["ErrorMessage"] = "No se pudo aprobar la rendición";
                return RedirectToAction("Detalle", "Rendiciones", new { id });
            }
            TempData["SuccessMessage"] = "Rendición aprobada";
            return RedirectToAction("Detalle", "Rendiciones", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> RechazarRendicion(int id, string? comentarios)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var ok = await _rendicionService.RechazarAsync(id, comentarios ?? "Rechazada por el supervisor", userId);
            if (!ok)
            {
                TempData["ErrorMessage"] = "No se pudo rechazar la rendición";
                return RedirectToAction("Detalle", "Rendiciones", new { id });
            }
            TempData["SuccessMessage"] = "Rendición rechazada";
            return RedirectToAction("Detalle", "Rendiciones", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarPago(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var rendicion = await _context.Rendiciones.FindAsync(id);
            if (rendicion == null)
            {
                TempData["ErrorMessage"] = "No se encontró la rendición";
                return RedirectToAction("Detalle", "Rendiciones", new { id });
            }
            if (rendicion.Estado != "aprobado_2")
            {
                TempData["ErrorMessage"] = "La rendición no está lista para pago";
                return RedirectToAction("Detalle", "Rendiciones", new { id });
            }
            rendicion.Estado = "pagado";
            await _context.SaveChangesAsync();

            // Notificar al empleado
            var usuario = await _context.Usuarios.FindAsync(rendicion.UsuarioId);
            if (usuario != null)
            {
                // Crear notificación en la base de datos
                var notificacion = new Notificacion
                {
                    UsuarioId = usuario.Id,
                    RendicionId = rendicion.Id,
                    Mensaje = $"Tu rendición {rendicion.NumeroTicket} ha sido marcada como pagada.",
                    Leido = false,
                    FechaCreacion = DateTime.Now,
                    TipoRol = "empleado"
                };
                _context.Notificaciones.Add(notificacion);
                await _context.SaveChangesAsync();

                // Enviar correo de rendición pagada
                if (!string.IsNullOrEmpty(usuario.Email))
                {
                    await _emailService.EnviarRendicionPagadaAsync(
                        usuario.Email,
                        rendicion.NumeroTicket ?? "",
                        rendicion.MontoTotal.ToString(),
                        $"{usuario.Nombre} {usuario.Apellidos}"
                    );
                }
            }
            TempData["SuccessMessage"] = "Pago procesado correctamente";
            return RedirectToAction("Detalle", "Rendiciones", new { id });
        }

        [HttpGet]
        [Route("Supervisores/Perfil")]
        public IActionResult Perfil()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = _context.Usuarios.Find(userId);
            var nombreUsuario = usuario != null ? $"{usuario.Nombre} {usuario.Apellidos}".Trim() : "Usuario";
            ViewBag.UserName = nombreUsuario;
            return RedirectToAction("PerfilFijo");
        }

        [HttpGet]
        public async Task<IActionResult> Historial(string estado = null)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            ViewBag.NotificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "supervisor").CountAsync();
            var query = _context.Rendiciones.Include(r => r.Usuario).AsQueryable();
            query = query.Where(r => r.Aprobador1Id == userId);
            // Solo filtrar por estado si el usuario lo selecciona
            if (!string.IsNullOrEmpty(estado))
                query = query.Where(r => r.Estado == estado);
            // Si no hay filtro, mostrar todas (pendientes, aprobadas, pagadas, rechazadas)
            var rendiciones = await query.OrderByDescending(r => r.FechaCreacion).ToListAsync();
            ViewBag.Estado = estado;
            return View("~/Views/ControlUsuarios/HistorialSupervisor.cshtml", rendiciones);
        }
    }
}