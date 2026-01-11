using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RendicionesPrimar.Data;
using RendicionesPrimar.Models;
using RendicionesPrimar.Models.ViewModels;
using RendicionesPrimar.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RendicionesPrimar.Controllers
{
    [Authorize]
    public class ControlUsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public ControlUsuariosController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Método para obtener el nombre del usuario buscando en ambas tablas
        private async Task<string> ObtenerNombreUsuario(int userId)
        {
            var usuario = await _context.Usuarios.FindAsync(userId);
            return usuario != null ? $"{usuario.Nombre} {usuario.Apellidos}" : "Usuario";
        }

        // Método para obtener los datos completos del usuario buscando en ambas tablas
        private async Task<Usuario?> ObtenerUsuarioCompleto(int userId)
        {
            var usuario = await _context.Usuarios.FindAsync(userId);
            return usuario;
        }

        // Verificar que el usuario sea Camila (aprobador1)
        private async Task<bool> EsCamila()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";
            
            // Temporalmente permitir acceso a cualquier supervisor (aprobador1)
            if (userRole == "aprobador1")
                return true;
            
            if (userRole != "aprobador1")
                return false;

            // Primero buscar en la tabla usuarios
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario != null)
            {
                return usuario.Email == "camila.flores@primar.cl";
            }

            return false;
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private async Task<string> ObtenerNombreCompletoUsuario(int userId)
        {
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario != null)
                return $"{usuario.Nombre} {usuario.Apellidos}".Trim();
            return "Usuario";
        }

        // === UTILIDADES DE VALIDACIÓN Y FORMATEO ===
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
        private string FormatearTelefono(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return "";
            telefono = telefono.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            // Chile
            if (telefono.StartsWith("+56")) telefono = telefono.Substring(3);
            if (telefono.Length == 9 && telefono.StartsWith("9"))
                return $"+56 9 {telefono.Substring(1,4)} {telefono.Substring(5,4)}";
            // Argentina
            if (telefono.StartsWith("+54")) telefono = telefono.Substring(3);
            if (telefono.Length == 10 && telefono.StartsWith("11"))
                return $"+54 9 11 {telefono.Substring(2,4)} {telefono.Substring(6,4)}";
            // Perú
            if (telefono.StartsWith("+51")) telefono = telefono.Substring(3);
            if (telefono.Length == 9 && telefono.StartsWith("9"))
                return $"+51 9 {telefono.Substring(1,4)} {telefono.Substring(5,4)}";
            return telefono;
        }
        private bool ValidarTelefono(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return false;
            string t = telefono.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            // Chile: 9 dígitos, empieza con 9
            if ((t.StartsWith("+569") && t.Length == 12) || (t.StartsWith("9") && t.Length == 9)) return true;
            // Argentina: 11 dígitos, empieza con 11
            if ((t.StartsWith("+54911") && t.Length == 13) || (t.StartsWith("11") && t.Length == 10)) return true;
            // Perú: 9 dígitos, empieza con 9
            if ((t.StartsWith("+519") && t.Length == 12) || (t.StartsWith("9") && t.Length == 9)) return true;
            return false;
        }

        public async Task<IActionResult> Index()
        {
            if (!await EsCamila())
            {
                return Forbid();
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            await ActualizarNotificacionesNoLeidas(userId);

            var usuarios = await _context.Usuarios
                .Where(u => u.Rol == "empleado")
                .OrderBy(u => u.Nombre)
                .ToListAsync();

            return View(usuarios);
        }

        public async Task<IActionResult> CrearUsuario()
        {
            if (!await EsCamila())
            {
                return Forbid();
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            await ActualizarNotificacionesNoLeidas(userId);

            return View(new CrearUsuarioViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CrearUsuario(CrearUsuarioViewModel model)
        {
            // TEMPORAL: Desactivar restricción para pruebas
            // if (!await EsCamila())
            // {
            //     return Forbid();
            // }

            if (!ModelState.IsValid)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
                await ActualizarNotificacionesNoLeidas(userId);
                Console.WriteLine("ModelState no es válido");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error de validación: {error.ErrorMessage}");
                }
                return View(model);
            }

            // Validación manual de contraseña
            if (string.IsNullOrEmpty(model.Password) || model.Password.Length < 6)
            {
                TempData["ErrorMessage"] = "La contraseña debe tener al menos 6 caracteres.";
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
                await ActualizarNotificacionesNoLeidas(userId);
                return View(model);
            }

            // Verificar si el email ya existe
            if (await _context.Usuarios.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "El email ya está registrado");
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
                await ActualizarNotificacionesNoLeidas(userId);
                Console.WriteLine($"Email ya existe: {model.Email}");
                TempData["ErrorMessage"] = $"El email {model.Email} ya está registrado";
                return View(model);
            }

            // Crear hash de la contraseña
            var passwordHash = HashPassword(model.Password ?? string.Empty);

            if (!ValidarRut(model.Rut))
            {
                ModelState.AddModelError("Rut", "El RUT ingresado no es válido.");
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
                await ActualizarNotificacionesNoLeidas(userId);
                return View(model);
            }
            if (!ValidarTelefono(model.Telefono))
            {
                ModelState.AddModelError("Telefono", "El teléfono ingresado no es válido para Chile, Argentina o Perú.");
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
                await ActualizarNotificacionesNoLeidas(userId);
                return View(model);
            }

            var nuevoUsuario = new Usuario
            {
                Nombre = model.Nombre ?? string.Empty,
                Apellidos = model.Apellidos ?? string.Empty,
                Rut = FormatearRut(model.Rut ?? string.Empty),
                Email = model.Email ?? string.Empty,
                Telefono = FormatearTelefono(model.Telefono ?? string.Empty),
                PasswordHash = passwordHash,
                Rol = "empleado",
                Cargo = model.Cargo ?? string.Empty,
                Departamento = model.Departamento ?? string.Empty,
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            try
            {
                Console.WriteLine($"Intentando crear usuario: {model.Nombre} {model.Apellidos}");
                _context.Usuarios.Add(nuevoUsuario);
                Console.WriteLine("Usuario agregado al contexto");
                await _context.SaveChangesAsync();
                Console.WriteLine("Usuario guardado en la base de datos");
                
                // Enviar email con credenciales
                if (!string.IsNullOrEmpty(model.Password))
                {
                    await EnviarEmailCredenciales(nuevoUsuario, model.Password);
                    Console.WriteLine("Email enviado");
                }
                
                TempData["SuccessMessage"] = $"Usuario {model.Nombre} creado exitosamente. Se han enviado las credenciales a su correo electrónico.";
                Console.WriteLine("Usuario creado exitosamente");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Loguear el error y mostrar mensaje en TempData
                Console.WriteLine($"Error al guardar usuario: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                TempData["ErrorMessage"] = $"Error al guardar usuario: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" | Error interno: {ex.InnerException.Message}";
                }
                return RedirectToAction("Index");
            }
        }

        private async Task EnviarEmailCredenciales(Usuario usuario, string password)
        {
            try
            {
                var asunto = "Credenciales de Acceso - Sistema de Rendiciones Primar";
                var mensaje = $@"
                    <h3>¡Bienvenido al Sistema de Rendiciones Primar!</h3>
                    <p>Hola <strong>{usuario.Nombre} {usuario.Apellidos}</strong>,</p>
                    <p>Tu cuenta ha sido creada exitosamente en el Sistema de Rendiciones Primar.</p>
                    
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h4 style='color: #495057; margin-top: 0;'>Tus credenciales de acceso:</h4>
                        <p><strong>Email:</strong> {usuario.Email}</p>
                        <p><strong>Contraseña:</strong> {password}</p>
                        <p><strong>Rol:</strong> {ObtenerNombreRol(usuario.Rol)}</p>
                        <p><strong>Cargo:</strong> {usuario.Cargo ?? "No especificado"}</p>
                        <p><strong>Departamento:</strong> {usuario.Departamento ?? "No especificado"}</p>
                    </div>
                    
                    <div style='background-color: #e7f3ff; padding: 15px; border-radius: 8px; border-left: 4px solid #007bff;'>
                        <h4 style='color: #0056b3; margin-top: 0;'>Instrucciones importantes:</h4>
                        <ul style='margin: 10px 0; padding-left: 20px;'>
                            <li>Cambia tu contraseña en tu primer ingreso por seguridad</li>
                            <li>Mantén tus credenciales en un lugar seguro</li>
                            <li>Si olvidas tu contraseña, usa la opción '¿Olvidaste tu contraseña?'</li>
                        </ul>
                    </div>
                    
                    <p style='color: #6c757d; font-size: 14px; margin-top: 30px;'>
                        Si tienes alguna pregunta, contacta al administrador del sistema.
                    </p>";

                await _emailService.EnviarNotificacionAsync(usuario.Email, asunto, mensaje);
            }
            catch (Exception ex)
            {
                // Log del error pero no fallar la creación del usuario
                // En un entorno de producción, podrías usar un logger real
                Console.WriteLine($"Error enviando email de credenciales: {ex.Message}");
            }
        }

        private string ObtenerNombreRol(string rol)
        {
            return rol switch
            {
                "empleado" => "Empleado",
                "aprobador1" => "Supervisor",
                "aprobador2" => "Gerente",
                "admin" => "Administrador",
                _ => rol
            };
        }

        public async Task<IActionResult> EditarUsuario(int id)
        {
            if (!await EsCamila())
            {
                return Forbid();
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            await ActualizarNotificacionesNoLeidas(userId);

            var usuarioEditar = await _context.Usuarios.FindAsync(id);
            if (usuarioEditar == null)
            {
                return NotFound();
            }

            var viewModel = new EditarUsuarioViewModel
            {
                Id = usuarioEditar.Id,
                Nombre = usuarioEditar.Nombre ?? string.Empty,
                Apellidos = usuarioEditar.Apellidos ?? string.Empty,
                Rut = usuarioEditar.Rut ?? string.Empty,
                Email = usuarioEditar.Email ?? string.Empty,
                Telefono = usuarioEditar.Telefono ?? string.Empty,
                Cargo = usuarioEditar.Cargo ?? string.Empty,
                Departamento = usuarioEditar.Departamento ?? string.Empty,
                Activo = usuarioEditar.Activo
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditarUsuario(EditarUsuarioViewModel model)
        {
            if (!await EsCamila())
            {
                TempData["ErrorMessage"] = "No tienes permisos para editar usuarios.";
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Hay errores en el formulario. Por favor revisa los campos.";
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
                await ActualizarNotificacionesNoLeidas(userId);
                return View(model);
            }

            if (!ValidarRut(model.Rut))
            {
                ModelState.AddModelError("Rut", "El RUT ingresado no es válido.");
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
                await ActualizarNotificacionesNoLeidas(userId);
                return View(model);
            }
            if (!ValidarTelefono(model.Telefono))
            {
                ModelState.AddModelError("Telefono", "El teléfono ingresado no es válido para Chile, Argentina o Perú.");
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
                await ActualizarNotificacionesNoLeidas(userId);
                return View(model);
            }

            var usuarioEditar = await _context.Usuarios.FindAsync(model.Id);
            if (usuarioEditar == null)
            {
                TempData["ErrorMessage"] = "El usuario no existe.";
                return NotFound();
            }

            // Verificar si el email ya existe en otro usuario
            if (await _context.Usuarios.AnyAsync(u => u.Email == model.Email && u.Id != model.Id))
            {
                TempData["ErrorMessage"] = "El email ya está registrado por otro usuario.";
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
                await ActualizarNotificacionesNoLeidas(userId);
                return View(model);
            }

            // Actualizar datos
            usuarioEditar.Nombre = model.Nombre ?? string.Empty;
            usuarioEditar.Apellidos = model.Apellidos ?? string.Empty;
            usuarioEditar.Rut = FormatearRut(model.Rut ?? string.Empty);
            usuarioEditar.Email = model.Email ?? string.Empty;
            usuarioEditar.Telefono = model.Telefono ?? string.Empty;
            usuarioEditar.Cargo = model.Cargo ?? string.Empty;
            usuarioEditar.Departamento = model.Departamento ?? string.Empty;
            usuarioEditar.Activo = model.Activo;

            // Actualizar contraseña si se proporcionó una nueva
            if (!string.IsNullOrEmpty(model.Password))
            {
                usuarioEditar.PasswordHash = HashPassword(model.Password);
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Usuario {model.Nombre} actualizado exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al guardar los cambios: {ex.Message}";
                return View(model);
            }
        }

        [HttpPost]
        [AjaxAuthorize]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            if (!await EsCamila())
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "No tienes permisos para eliminar usuarios." });
                return Forbid();
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "El usuario no existe o ya fue eliminado." });
                return NotFound();
            }

            // No permitir eliminar a Camila a sí misma
            if (usuario.Email == "camila.flores@primar.cl")
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "No puedes eliminar tu propio usuario" });
                TempData["ErrorMessage"] = "No puedes eliminar tu propio usuario";
                return RedirectToAction("Index");
            }

            try
            {
                // Eliminar notificaciones asociadas
                var notificaciones = _context.Notificaciones.Where(n => n.UsuarioId == id);
                _context.Notificaciones.RemoveRange(notificaciones);

                // Eliminar usuario físicamente
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = $"Usuario {usuario.Nombre} eliminado exitosamente" });
                TempData["SuccessMessage"] = $"Usuario {usuario.Nombre} eliminado exitosamente";
                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "El usuario ya fue eliminado por otro proceso." });
                TempData["ErrorMessage"] = "El usuario ya fue eliminado por otro proceso.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Error inesperado: " + ex.Message });
                TempData["ErrorMessage"] = "Error inesperado: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private async Task ActualizarNotificacionesNoLeidas(int userId)
        {
            var notificacionesNoLeidas = await _context.Notificaciones
                .Where(n => n.UsuarioId == userId && !n.Leido)
                .CountAsync();
            
            ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;
        }

        [HttpGet]
        public async Task<IActionResult> Historial(string estado = null)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            var query = _context.Rendiciones.Include(r => r.Usuario).AsQueryable();
            query = query.Where(r => r.Aprobador1Id == userId);
            if (!string.IsNullOrEmpty(estado))
                query = query.Where(r => r.Estado == estado);
            var rendiciones = await query.OrderByDescending(r => r.FechaCreacion).ToListAsync();
            ViewBag.Estado = estado;
            return View("HistorialSupervisor", rendiciones);
        }
    }
} 