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
    [Authorize(Roles = "aprobador2")]
    public class GerentesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RendicionService _rendicionService;

        public GerentesController(ApplicationDbContext context, RendicionService rendicionService)
        {
            _context = context;
            _rendicionService = rendicionService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "aprobador2";

            // Obtener el usuario completo con su nombre
            var nombreUsuario = await ObtenerNombreCompletoUsuario(userId);

            // Contar notificaciones no leídas específicas de gerentes
            var notificacionesNoLeidas = await _context.Notificaciones
                .Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "gerente")
                .CountAsync();

            ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;
            ViewBag.UserRole = userRole;
            ViewBag.UserName = nombreUsuario;

            // Crear el modelo de dashboard para gerente
            var modelo = new DashboardViewModel
            {
                UserName = nombreUsuario ?? string.Empty,
                UserRole = userRole ?? string.Empty,
                NotificacionesNoLeidas = notificacionesNoLeidas,
                TotalRendiciones = await _context.Rendiciones
                    .Where(r => r.Estado == "aprobado_1" || r.Estado == "rechazado_1")
                    .CountAsync(),
                RendicionesPendientes = await _context.Rendiciones
                    .Where(r => r.Estado == "aprobado_1" || r.Estado == "rechazado_1")
                    .CountAsync(),
                RendicionesAprobadas = 0,
                MontoTotal = await _context.Rendiciones
                    .Where(r => r.Estado == "aprobado_1" || r.Estado == "rechazado_1")
                    .SumAsync(r => r.MontoTotal)
            };

            // Obtener últimas rendiciones pendientes de aprobación final
            var ultimasRendiciones = await _context.Rendiciones
                .Include(r => r.Usuario)
                .Where(r => r.Estado == "aprobado_1" || r.Estado == "rechazado_1")
                .OrderByDescending(r => r.FechaCreacion)
                .Take(5)
                .ToListAsync();

            modelo.UltimasRendiciones = ultimasRendiciones;

            return View("~/Views/Home/DashboardGerente.cshtml", modelo);
        }

        public async Task<IActionResult> RendicionesPendientes()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            var notificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "gerente").CountAsync();
            ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;

            var rendiciones = await _context.Rendiciones
                .Include(r => r.Usuario)
                .Where(r => r.Estado == "aprobado_1" || r.Estado == "rechazado_1")
                .OrderByDescending(r => r.FechaCreacion)
                .ToListAsync();

            return View("RendicionesPendientes", rendiciones);
        }

        [HttpGet]
        public async Task<IActionResult> Notificaciones()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notificaciones = await _context.Notificaciones
                .Where(n => n.UsuarioId == userId && n.TipoRol == "gerente")
                .Include(n => n.Rendicion)
                .OrderByDescending(n => n.FechaCreacion)
                .ToListAsync();
            ViewBag.NotificacionesNoLeidas = notificaciones.Count(n => !n.Leido);
            ViewBag.UserId = userId;
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            return View(notificaciones);
        }

        public async Task<IActionResult> Reportes()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            var notificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "gerente").CountAsync();
            ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;

            // Estadísticas para reportes de gerente
            var estadisticas = new
            {
                TotalPendientesAprobacion = await _context.Rendiciones.Where(r => r.Estado == "aprobado_1" || r.Estado == "rechazado_1").CountAsync(),
                TotalAprobadas = await _context.Rendiciones.Where(r => r.Estado == "aprobado_2").CountAsync(),
                TotalPagadas = await _context.Rendiciones.Where(r => r.Estado == "pagado").CountAsync(),
                TotalRechazadas = await _context.Rendiciones.Where(r => r.Estado == "rechazado").CountAsync(),
                MontoTotalPendiente = await _context.Rendiciones.Where(r => r.Estado == "aprobado_1" || r.Estado == "rechazado_1").SumAsync(r => r.MontoTotal),
                MontoTotalAprobado = await _context.Rendiciones.Where(r => r.Estado == "aprobado_2").SumAsync(r => r.MontoTotal),
                RendicionesPorMes = await _context.Rendiciones
                    .Where(r => r.FechaCreacion >= DateTime.Now.AddMonths(-1))
                    .CountAsync()
            };

            return View("Reportes", estadisticas);
        }

        [HttpPost]
        public async Task<IActionResult> AprobarRendicion(int id, string? comentarios)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var ok = await _rendicionService.AprobarSegundaInstanciaAsync(id, userId, comentarios);
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
            var ok = await _rendicionService.RechazarAsync(id, comentarios ?? "Rechazada por el gerente", userId);
            if (!ok)
            {
                TempData["ErrorMessage"] = "No se pudo rechazar la rendición";
                return RedirectToAction("Detalle", "Rendiciones", new { id });
            }
            TempData["SuccessMessage"] = "Rendición rechazada";
            return RedirectToAction("Detalle", "Rendiciones", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> MarcarNotificacionLeida(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == userId && n.TipoRol == "gerente");

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
                return RedirectToAction("Notificaciones", "Gerentes");
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarcarNotificacionLeidaYVer(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var notificacion = await _context.Notificaciones
                .Include(n => n.Rendicion)
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == userId && n.TipoRol == "gerente");

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

        [HttpGet]
        [Route("/perfil-gerente")]
        public async Task<IActionResult> PerfilFijo()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = await _context.Usuarios.FindAsync(userId);
            var nombreCompleto = usuario != null ? $"{usuario.Nombre} {usuario.Apellidos}".Trim() : "Usuario";
            ViewBag.UserName = nombreCompleto;
            var notificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "gerente").CountAsync();
            ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;
            if (usuario == null)
                return Content("Usuario no encontrado");
            var viewModel = new InformacionPersonalViewModel
            {
                Nombre = usuario.Nombre,
                Apellidos = usuario.Apellidos,
                Rut = usuario.Rut,
                Email = usuario.Email,
                Cargo = usuario.Cargo,
                Departamento = usuario.Departamento,
                Telefono = usuario.Telefono
            };
            return View("Perfil", viewModel);
        }

        [Authorize(Roles = "aprobador2")]
        [HttpPost]
        [Route("/perfil-gerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PerfilFijo(InformacionPersonalViewModel model)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Hay errores en el formulario. Por favor revisa los campos.";
                var usuario = await _context.Usuarios.FindAsync(userId);
                var nombreCompleto = usuario != null ? $"{usuario.Nombre} {usuario.Apellidos}".Trim() : "Usuario";
                ViewBag.UserName = nombreCompleto;
                var viewModel = new InformacionPersonalViewModel
                {
                    Nombre = model.Nombre,
                    Apellidos = model.Apellidos,
                    Rut = model.Rut,
                    Email = model.Email,
                    Cargo = model.Cargo,
                    Departamento = model.Departamento,
                    Telefono = model.Telefono
                };
                return View("Perfil", viewModel);
            }
            var usuarioDb = await _context.Usuarios.FindAsync(userId);
            if (usuarioDb == null)
            {
                TempData["ErrorMessage"] = "No se encontró el usuario en la base de datos.";
                return Content("Usuario no encontrado");
            }
            usuarioDb.Nombre = model.Nombre;
            usuarioDb.Apellidos = model.Apellidos;
            usuarioDb.Rut = model.Rut;
            usuarioDb.Email = model.Email;
            usuarioDb.Telefono = model.Telefono;
            usuarioDb.Cargo = model.Cargo;
            usuarioDb.Departamento = model.Departamento;
            _context.Entry(usuarioDb).Property(x => x.Nombre).IsModified = true;
            _context.Entry(usuarioDb).Property(x => x.Apellidos).IsModified = true;
            _context.Entry(usuarioDb).Property(x => x.Rut).IsModified = true;
            _context.Entry(usuarioDb).Property(x => x.Email).IsModified = true;
            _context.Entry(usuarioDb).Property(x => x.Telefono).IsModified = true;
            _context.Entry(usuarioDb).Property(x => x.Cargo).IsModified = true;
            _context.Entry(usuarioDb).Property(x => x.Departamento).IsModified = true;
            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "¡Cambios guardados exitosamente!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al guardar los cambios en la base de datos.";
            }
            return RedirectToAction("PerfilFijo");
        }

        [HttpGet]
        public async Task<IActionResult> Perfil()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = await _context.Usuarios.FindAsync(userId);
            var nombreCompleto = usuario != null ? $"{usuario.Nombre} {usuario.Apellidos}".Trim() : "Usuario";
            ViewBag.UserName = nombreCompleto;
            var notificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "gerente").CountAsync();
            ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;
            if (usuario == null)
                return Content("Usuario no encontrado");
            var viewModel = new InformacionPersonalViewModel
            {
                Nombre = usuario.Nombre,
                Apellidos = usuario.Apellidos,
                Rut = usuario.Rut,
                Email = usuario.Email,
                Cargo = usuario.Cargo,
                Departamento = usuario.Departamento,
                Telefono = usuario.Telefono
            };
            return View("Perfil", viewModel);
        }

        [HttpPost]
        [AjaxAuthorize]
        public async Task<IActionResult> EliminarNotificacion(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == userId && n.TipoRol == "gerente");
            if (notificacion == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "La notificación no existe o ya fue eliminada." });
                TempData["ErrorMessage"] = "La notificación no existe o ya fue eliminada.";
                return RedirectToAction("Notificaciones", "Gerentes");
            }
            try
            {
                _context.Notificaciones.Remove(notificacion);
                await _context.SaveChangesAsync();
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true });
                TempData["SuccessMessage"] = "Notificación eliminada correctamente.";
                return RedirectToAction("Notificaciones", "Gerentes");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "La notificación ya fue eliminada por otro proceso." });
                TempData["ErrorMessage"] = "La notificación ya fue eliminada por otro proceso.";
                return RedirectToAction("Notificaciones", "Gerentes");
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Error inesperado: " + ex.Message });
                TempData["ErrorMessage"] = "Error inesperado: " + ex.Message;
                return RedirectToAction("Notificaciones", "Gerentes");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Historial(string estado = null)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            var notificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "gerente").CountAsync();
            ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;
            var query = _context.Rendiciones.Include(r => r.Usuario).AsQueryable();
            query = query.Where(r => r.Aprobador2Id == userId);
            if (!string.IsNullOrEmpty(estado))
                query = query.Where(r => r.Estado == estado);
            var rendiciones = await query.OrderByDescending(r => r.FechaCreacion).ToListAsync();
            ViewBag.Estado = estado;
            return View("HistorialGerente", rendiciones);
        }

        private async Task<string> ObtenerNombreCompletoUsuario(int userId)
        {
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario != null)
                return $"{usuario.Nombre} {usuario.Apellidos}".Trim();
            return "Usuario";
        }
    }
}