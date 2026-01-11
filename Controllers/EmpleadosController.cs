using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RendicionesPrimar.Data;
using RendicionesPrimar.Models;
using RendicionesPrimar.Models.ViewModels;
using System.Security.Claims;

namespace RendicionesPrimar.Controllers
{
    [Authorize(Roles = "empleado")]
    public class EmpleadosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmpleadosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";

            // Obtener el usuario completo con su nombre
            var nombreUsuario = await ObtenerNombreCompletoUsuario(userId);

            // Contar notificaciones no leídas específicas de empleados
            var notificacionesNoLeidas = await _context.Notificaciones
                .Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "empleado")
                .CountAsync();

            ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;
            ViewBag.UserRole = userRole;
            ViewBag.UserName = nombreUsuario;

            // Crear el modelo de dashboard para empleado
            var modelo = new DashboardViewModel
            {
                UserName = nombreUsuario ?? string.Empty,
                UserRole = userRole ?? string.Empty,
                NotificacionesNoLeidas = notificacionesNoLeidas,
                TotalRendiciones = await _context.Rendiciones
                    .Where(r => r.UsuarioId == userId)
                    .CountAsync(),
                RendicionesPendientes = await _context.Rendiciones
                    .Where(r => r.UsuarioId == userId && r.Estado == "pendiente")
                    .CountAsync(),
                RendicionesAprobadas = await _context.Rendiciones
                    .Where(r => r.UsuarioId == userId && (r.Estado == "aprobado_2" || r.Estado == "pagado"))
                    .CountAsync(),
                MontoTotal = await _context.Rendiciones
                    .Where(r => r.UsuarioId == userId)
                    .SumAsync(r => r.MontoTotal)
            };

            // Obtener últimas rendiciones del empleado
            var ultimasRendiciones = await _context.Rendiciones
                .Where(r => r.UsuarioId == userId)
                .OrderByDescending(r => r.FechaCreacion)
                .Take(5)
                .ToListAsync();

            modelo.UltimasRendiciones = ultimasRendiciones;

            return View("Dashboard", modelo);
        }

        public async Task<IActionResult> MisRendiciones()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            ViewBag.UserRole = userRole;
            ViewBag.NotificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "empleado").CountAsync();
            var rendiciones = await _context.Rendiciones
                .Where(r => r.UsuarioId == userId && !r.EliminadaPorEmpleado)
                // No filtrar por estado, así se incluyen las rechazadas
                .OrderByDescending(r => r.FechaCreacion)
                .ToListAsync();
            return View("MisRendiciones", rendiciones);
        }

        [HttpGet]
        public async Task<IActionResult> Notificaciones()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notificaciones = await _context.Notificaciones
                .Where(n => n.UsuarioId == userId && n.TipoRol == "empleado")
                .Include(n => n.Rendicion)
                .OrderByDescending(n => n.FechaCreacion)
                .ToListAsync();
            ViewBag.NotificacionesNoLeidas = notificaciones.Count(n => !n.Leido);
            ViewBag.UserId = userId;
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            return View(notificaciones);
        }

        [HttpGet]
        public async Task<IActionResult> Perfil()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
                return NotFound();
            var viewModel = new InformacionPersonalViewModel
            {
                Nombre = usuario.Nombre ?? string.Empty,
                Apellidos = usuario.Apellidos ?? string.Empty,
                Rut = FormatearRut(usuario.Rut ?? string.Empty),
                Email = usuario.Email ?? string.Empty,
                Telefono = usuario.Telefono ?? string.Empty,
                Cargo = usuario.Cargo ?? string.Empty,
                Departamento = usuario.Departamento ?? string.Empty
            };
            ViewBag.UserName = $"{usuario.Nombre} {usuario.Apellidos}";
            ViewBag.NotificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "empleado").CountAsync();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Perfil(InformacionPersonalViewModel model)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (!ModelState.IsValid)
                return View(model);

            if (!ValidarRut(model.Rut))
            {
                ModelState.AddModelError("Rut", "El RUT ingresado no es válido.");
                return View(model);
            }

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
                return NotFound();

            // Solo actualiza los campos permitidos
            usuario.Nombre = model.Nombre ?? string.Empty;
            usuario.Apellidos = model.Apellidos ?? string.Empty;
            usuario.Rut = FormatearRut(model.Rut ?? string.Empty);
            usuario.Email = model.Email ?? string.Empty;
            usuario.Telefono = model.Telefono ?? string.Empty;
            usuario.Cargo = model.Cargo ?? string.Empty;
            usuario.Departamento = model.Departamento ?? string.Empty;
            // NO toques: password_hash, rol, activo, mfa, etc.

            _context.Entry(usuario).Property(x => x.Nombre).IsModified = true;
            _context.Entry(usuario).Property(x => x.Apellidos).IsModified = true;
            _context.Entry(usuario).Property(x => x.Rut).IsModified = true;
            _context.Entry(usuario).Property(x => x.Email).IsModified = true;
            _context.Entry(usuario).Property(x => x.Telefono).IsModified = true;
            _context.Entry(usuario).Property(x => x.Cargo).IsModified = true;
            _context.Entry(usuario).Property(x => x.Departamento).IsModified = true;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "¡Cambios guardados!";
            return RedirectToAction("PerfilFijo");
        }

        [HttpPost]
        public async Task<IActionResult> MarcarNotificacionLeida(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == userId && n.TipoRol == "empleado");

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
                return RedirectToAction("Notificaciones", "Empleados");
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarcarNotificacionLeidaYVer(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notificacion = await _context.Notificaciones
                .Include(n => n.Rendicion)
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == userId && n.TipoRol == "empleado");

            if (notificacion != null)
            {
                notificacion.Leido = true;
                await _context.SaveChangesAsync();

                var redirectUrl = Url.Action("Detalle", "Rendiciones", new { id = notificacion.RendicionId });

                // Si la petición es AJAX, responde JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, redirectUrl });
                }
                else
                {
                    // Si NO es AJAX, redirige directamente
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
        [Route("/perfil-empleado")]
        public async Task<IActionResult> PerfilFijo()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
                return NotFound();
            var viewModel = new InformacionPersonalViewModel
            {
                Nombre = usuario.Nombre ?? string.Empty,
                Apellidos = usuario.Apellidos ?? string.Empty,
                Rut = FormatearRut(usuario.Rut ?? string.Empty),
                Email = usuario.Email ?? string.Empty,
                Telefono = usuario.Telefono ?? string.Empty,
                Cargo = usuario.Cargo ?? string.Empty,
                Departamento = usuario.Departamento ?? string.Empty
            };
            ViewBag.UserName = $"{usuario.Nombre} {usuario.Apellidos}";
            ViewBag.NotificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "empleado").CountAsync();
            return View("Perfil", viewModel);
        }

        [Authorize(Roles = "empleado")]
        [HttpPost]
        [Route("/perfil-empleado")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PerfilFijo(InformacionPersonalViewModel model)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (!ModelState.IsValid)
                return Content("Modelo inválido");

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
                return Content("Usuario no encontrado");

            // Solo actualiza los campos permitidos
            usuario.Nombre = model.Nombre ?? string.Empty;
            usuario.Apellidos = model.Apellidos ?? string.Empty;
            usuario.Rut = FormatearRut(model.Rut ?? string.Empty);
            usuario.Email = model.Email ?? string.Empty;
            usuario.Telefono = model.Telefono ?? string.Empty;
            usuario.Cargo = model.Cargo ?? string.Empty;
            usuario.Departamento = model.Departamento ?? string.Empty;
            // NO toques: password_hash, rol, activo, mfa, etc.

            _context.Entry(usuario).Property(x => x.Nombre).IsModified = true;
            _context.Entry(usuario).Property(x => x.Apellidos).IsModified = true;
            _context.Entry(usuario).Property(x => x.Rut).IsModified = true;
            _context.Entry(usuario).Property(x => x.Email).IsModified = true;
            _context.Entry(usuario).Property(x => x.Telefono).IsModified = true;
            _context.Entry(usuario).Property(x => x.Cargo).IsModified = true;
            _context.Entry(usuario).Property(x => x.Departamento).IsModified = true;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "¡Cambios guardados!";
            return RedirectToAction("PerfilFijo");
        }

        [HttpGet]
        public async Task<IActionResult> Ayuda()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            ViewBag.NotificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "empleado").CountAsync();
            return View();
        }

        [HttpPost]
        [AjaxAuthorize]
        public async Task<IActionResult> EliminarNotificacion(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == userId && n.TipoRol == "empleado");
            if (notificacion == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "La notificación no existe o ya fue eliminada." });
                TempData["ErrorMessage"] = "La notificación no existe o ya fue eliminada.";
                return RedirectToAction("Notificaciones", "Empleados");
            }
            try
            {
                _context.Notificaciones.Remove(notificacion);
                await _context.SaveChangesAsync();
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true });
                TempData["SuccessMessage"] = "Notificación eliminada correctamente.";
                return RedirectToAction("Notificaciones", "Empleados");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "La notificación ya fue eliminada por otro proceso." });
                TempData["ErrorMessage"] = "La notificación ya fue eliminada por otro proceso.";
                return RedirectToAction("Notificaciones", "Empleados");
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Error inesperado: " + ex.Message });
                TempData["ErrorMessage"] = "Error inesperado: " + ex.Message;
                return RedirectToAction("Notificaciones", "Empleados");
            }
        }

        private async Task<string> ObtenerNombreCompletoUsuario(int userId)
        {
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario != null)
                return $"{usuario.Nombre} {usuario.Apellidos}".Trim();
            return "Usuario";
        }

        private string FormatearRut(string rut)
        {
            if (string.IsNullOrWhiteSpace(rut)) return "";
            rut = rut.Replace(".", "").Replace("-", "").Trim();
            if (rut.Length < 2) return rut;
            string cuerpo = rut.Substring(0, rut.Length - 1);
            string dv = rut.Substring(rut.Length - 1);
            string cuerpoFormateado = "";
            int contador = 0;
            for (int i = cuerpo.Length - 1; i >= 0; i--)
            {
                cuerpoFormateado = cuerpo[i] + cuerpoFormateado;
                contador++;
                if (contador == 3 && i != 0)
                {
                    cuerpoFormateado = "." + cuerpoFormateado;
                    contador = 0;
                }
            }
            return $"{cuerpoFormateado}-{dv}";
        }

        private bool ValidarRut(string rut)
        {
            if (string.IsNullOrWhiteSpace(rut)) return false;
            rut = rut.Replace(".", "").Replace("-", "").ToUpper();
            if (rut.Length < 2) return false;
            string cuerpo = rut.Substring(0, rut.Length - 1);
            string dv = rut.Substring(rut.Length - 1);
            int suma = 0, multiplo = 2;
            for (int i = cuerpo.Length - 1; i >= 0; i--)
            {
                suma += int.Parse(cuerpo[i].ToString()) * multiplo;
                multiplo = multiplo == 7 ? 2 : multiplo + 1;
            }
            int dvEsperado = 11 - (suma % 11);
            string dvCalc = dvEsperado == 11 ? "0" : dvEsperado == 10 ? "K" : dvEsperado.ToString();
            return dv == dvCalc;
        }
    }
}