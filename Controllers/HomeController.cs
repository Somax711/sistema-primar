using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RendicionesPrimar.Data;
using RendicionesPrimar.Models;
using RendicionesPrimar.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;

namespace RendicionesPrimar.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.ToLower() ?? "";
            switch (userRole)
            {
                case "aprobador1":
                case "supervisor":
                    return RedirectToAction("DashboardSupervisor");
                case "aprobador2":
                case "gerente":
                    return RedirectToAction("DashboardGerente");
                case "empleado":
                    return RedirectToAction("DashboardEmpleado");
                default:
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    TempData["Error"] = "Rol no reconocido. Contacte al administrador.";
                    return RedirectToAction("Login", "Account");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugNotificaciones()
        {
            try
            {
                var result = new
                {
                    DatabaseConnection = "OK",
                    Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    Tables = new
                    {
                        Usuarios = await _context.Usuarios.CountAsync(),
                        Rendiciones = await _context.Rendiciones.CountAsync(),
                        Notificaciones = await _context.Notificaciones.CountAsync()
                    },
                    RecentUsers = await _context.Usuarios
                        .Where(u => u.Activo)
                        .OrderByDescending(u => u.FechaCreacion)
                        .Take(3)
                        .Select(u => new { u.Id, u.Nombre, u.Email, u.Rol })
                        .ToListAsync(),
                    RecentNotifications = await _context.Notificaciones
                        .OrderByDescending(n => n.FechaCreacion)
                        .Take(5)
                        .Select(n => new { n.Id, n.UsuarioId, n.Mensaje, n.TipoRol, n.Leido, n.FechaCreacion })
                        .ToListAsync()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = "Error en debug",
                    message = ex.Message,
                    stackTrace = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace.Length))
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CrearNotificacionSimple()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value?.ToLower() ?? "empleado";
                
                // Determinar el tipo de rol para la notificación
                string tipoRol = "empleado";
                if (userRole == "aprobador1" || userRole == "supervisor")
                    tipoRol = "supervisor";
                else if (userRole == "aprobador2" || userRole == "gerente")
                    tipoRol = "gerente";
                
                var notificacion = new Notificacion
                {
                    UsuarioId = userId,
                    RendicionId = 1, // Valor de prueba
                    Mensaje = $"🧪 Prueba de notificación para {tipoRol} - {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                    Leido = false,
                    FechaCreacion = DateTime.Now,
                    TipoRol = tipoRol
                };

                _context.Notificaciones.Add(notificacion);
                await _context.SaveChangesAsync();

                // Enviar notificación en tiempo real
                var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<NotificacionesHub>>();
                await hubContext.Clients.Group($"user_{userId}").SendAsync("NuevaNotificacion", new {
                    Id = notificacion.Id,
                    RendicionId = notificacion.RendicionId,
                    Mensaje = notificacion.Mensaje,
                    Leido = notificacion.Leido,
                    FechaCreacion = notificacion.FechaCreacion,
                    TipoRol = notificacion.TipoRol
                });

                return Json(new
                {
                    success = true,
                    message = $"Notificación de prueba creada para {tipoRol}",
                    notificacion = new
                    {
                        notificacion.Id,
                        notificacion.Mensaje,
                        notificacion.FechaCreacion,
                        notificacion.TipoRol
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var tablesExist = new
                {
                    Usuarios = await _context.Usuarios.AnyAsync(),
                    Rendiciones = await _context.Rendiciones.AnyAsync(),
                    Notificaciones = await _context.Notificaciones.AnyAsync()
                };

                return Json(new
                {
                    canConnect,
                    tablesExist,
                    timestamp = DateTime.Now,
                    connectionString = _context.Database.GetConnectionString()?.Replace("Password=", "Password=***")
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = true,
                    message = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        private async Task<string> ObtenerNombreCompletoUsuario(int userId)
        {
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario != null)
                return $"{usuario.Nombre} {usuario.Apellidos}".Trim();
            return "Usuario";
        }

        [HttpGet]
        public async Task<IActionResult> DashboardEmpleado()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            // Obtener datos del dashboard (puedes personalizar esto según lo que necesites mostrar)
            var model = new Models.ViewModels.DashboardViewModel
            {
                TotalRendiciones = await _context.Rendiciones.CountAsync(r => r.UsuarioId == userId && !r.EliminadaPorEmpleado),
                RendicionesPendientes = await _context.Rendiciones.CountAsync(r => r.UsuarioId == userId && r.Estado == "pendiente" && !r.EliminadaPorEmpleado),
                RendicionesAprobadas = await _context.Rendiciones.CountAsync(r => r.UsuarioId == userId && (r.Estado == "aprobado_2" || r.Estado == "pagado") && !r.EliminadaPorEmpleado),
                RendicionesRechazadas = await _context.Rendiciones.CountAsync(r => r.UsuarioId == userId && r.Estado == "rechazado" && !r.EliminadaPorEmpleado),
                UltimasRendiciones = await _context.Rendiciones.Where(r => r.UsuarioId == userId && !r.EliminadaPorEmpleado).OrderByDescending(r => r.FechaCreacion).Take(5).ToListAsync()
            };
            ViewBag.NotificacionesNoLeidas = await _context.Notificaciones.CountAsync(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "empleado");
            return View("DashboardEmpleado", model);
        }

        [HttpGet]
        public async Task<IActionResult> DashboardSupervisor()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            // Datos para el dashboard de supervisor
            var total = await _context.Rendiciones.CountAsync();
            var pendientes = await _context.Rendiciones.CountAsync(r => r.Estado == "pendiente");
            var enEsperaGerencia = await _context.Rendiciones.CountAsync(r => r.Estado == "aprobado_1");
            var aprobadas = await _context.Rendiciones.CountAsync(r => r.Estado == "aprobado_2" || r.Estado == "pagado");
            var rechazadas = await _context.Rendiciones.CountAsync(r => r.Estado == "rechazado");
            var model = new Models.ViewModels.DashboardViewModel
            {
                TotalRendiciones = total,
                RendicionesPendientes = pendientes,
                RendicionesAprobadas = aprobadas,
                RendicionesRechazadas = rechazadas,
                UltimasRendiciones = await _context.Rendiciones.OrderByDescending(r => r.FechaCreacion).Take(5).ToListAsync()
            };
            ViewBag.EnEsperaGerencia = enEsperaGerencia;
            ViewBag.NotificacionesNoLeidas = await _context.Notificaciones.CountAsync(n => n.UsuarioId == userId && !n.Leido);
            return View("DashboardSupervisor", model);
        }

        [HttpGet]
        public async Task<IActionResult> DashboardGerente()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            // Datos para el dashboard de gerente
            var model = new Models.ViewModels.DashboardViewModel
            {
                TotalRendiciones = await _context.Rendiciones.CountAsync(),
                RendicionesPendientes = await _context.Rendiciones.CountAsync(r => r.Estado == "aprobado_1"),
                RendicionesAprobadas = await _context.Rendiciones.CountAsync(r => r.Estado == "aprobado_2"),
                MontoTotal = await _context.Rendiciones.Where(r => r.Estado == "aprobado_1").SumAsync(r => (decimal?)r.MontoTotal) ?? 0,
                UltimasRendiciones = await _context.Rendiciones.OrderByDescending(r => r.FechaCreacion).Take(5).ToListAsync()
            };
            ViewBag.NotificacionesNoLeidas = await _context.Notificaciones.CountAsync(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "gerente");
            return View("DashboardGerente", model);
        }

        [HttpPost]
        [AjaxAuthorize]
        public async Task<IActionResult> EliminarNotificacion(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == userId);

            if (notificacion == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "La notificación no existe o ya fue eliminada." });
                TempData["ErrorMessage"] = "La notificación no existe o ya fue eliminada.";
                return RedirectToAction("Notificaciones");
            }

            try
            {
                _context.Notificaciones.Remove(notificacion);
                await _context.SaveChangesAsync();
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true });
                TempData["SuccessMessage"] = "Notificación eliminada correctamente.";
                return RedirectToAction("Notificaciones");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "La notificación ya fue eliminada por otro proceso." });
                TempData["ErrorMessage"] = "La notificación ya fue eliminada por otro proceso.";
                return RedirectToAction("Notificaciones");
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Error inesperado: " + ex.Message });
                TempData["ErrorMessage"] = "Error inesperado: " + ex.Message;
                return RedirectToAction("Notificaciones");
            }
        }

        [HttpGet]
        public IActionResult Notificaciones()
        {
            var rol = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.ToLower() ?? "";
            switch (rol)
            {
                case "empleado":
                    return RedirectToAction("Notificaciones", "Empleados");
                case "aprobador1":
                case "supervisor":
                    return RedirectToAction("Notificaciones", "Supervisores");
                case "aprobador2":
                case "gerente":
                    return RedirectToAction("Notificaciones", "Gerentes");
                default:
                    return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> AyudaGerente()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            if (userRole.ToLower().Contains("gerente") || userRole.Equals("aprobador2", StringComparison.OrdinalIgnoreCase))
            {
                var notificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "gerente").CountAsync();
                ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;
            }
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            return View("Ayuda");
        }

        [HttpGet]
        public async Task<IActionResult> Ayuda()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            if (userRole.ToLower().Contains("gerente") || userRole.Equals("aprobador2", StringComparison.OrdinalIgnoreCase))
            {
                var notificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "gerente").CountAsync();
                ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;
            }
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            return View("Ayuda");
        }

        [HttpPost]
        public async Task<IActionResult> MarcarComoLeida(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notificacion = await _context.Notificaciones.FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == userId);
            if (notificacion != null && !notificacion.Leido)
            {
                notificacion.Leido = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Notificación marcada como leída.";
            }
            // Si la petición es AJAX, devolver JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }
            return RedirectToAction("Notificaciones", "Empleados");
        }
    }
}