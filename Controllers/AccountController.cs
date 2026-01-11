using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using RendicionesPrimar.Data;
using RendicionesPrimar.Models;
using System.Net.Mail;
using System.Net;
using RendicionesPrimar.Services;
using MimeKit;
using MailKit.Net.Smtp;

namespace RendicionesPrimar.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMfaService _mfaService;

        public AccountController(ApplicationDbContext context, IConfiguration configuration, IMfaService mfaService)
        {
            _context = context;
            _configuration = configuration;
            _mfaService = mfaService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Si el usuario ya está autenticado, redirigir al home
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Validaciones básicas
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Email y contraseña son requeridos";
                return View();
            }

            // Validar formato de email
            if (!IsValidEmail(email))
            {
                ViewBag.Error = "Formato de email inválido";
                return View();
            }

            try
            {
                var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email && u.Activo);
                if (user == null)
                {
                    ViewBag.Error = "Usuario no encontrado o cuenta inactiva";
                    Console.WriteLine($"Login fallido: usuario no encontrado para {email}");
                    return View();
                }

                if (!VerifyPassword(password, user.PasswordHash))
                {
                    ViewBag.Error = "Credenciales inválidas";
                    Console.WriteLine($"Login fallido: contraseña inválida para {email}");
                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Nombre),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Rol)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                Console.WriteLine($"Login exitoso para {email} (ID: {user.Id}, Rol: {user.Rol})");
                // Redirección personalizada según rol
                switch (user.Rol?.ToLower())
                {
                    case "aprobador1":
                    case "supervisor":
                        return RedirectToAction("DashboardSupervisor", "Home");
                    case "aprobador2":
                    case "gerente":
                        return RedirectToAction("DashboardGerente", "Home");
                    case "empleado":
                        return RedirectToAction("DashboardEmpleado", "Home");
                    default:
                        ViewBag.Error = "Rol no reconocido. Contacte al administrador.";
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error interno del servidor. Por favor, intente nuevamente.";
                Console.WriteLine($"Error en Login para {email}: {ex.Message}\n{ex.StackTrace}");
                return View();
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;
            try
            {
                using var sha256 = SHA256.Create();
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hashedPassword = Convert.ToBase64String(hashedBytes);
                return hashedPassword == hash;
            }
            catch
            {
                return false;
            }
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet]
        public IActionResult RecuperarContrasena()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EnviarCodigoRecuperacion(string email)
        {
            try
            {
                // Validar que el email no esté vacío
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Json(new { success = false, message = "Por favor, ingresa un correo electrónico válido." });
                }

                // Validar formato de email
                if (!IsValidEmail(email))
                {
                    return Json(new { success = false, message = "Por favor, ingresa un formato de correo electrónico válido." });
                }

                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email && u.Activo);
                if (usuario == null)
                {
                    return Json(new { success = false, message = "No se encontró una cuenta activa asociada a este correo electrónico. Verifica que el correo esté correctamente escrito o contacta al administrador del sistema." });
                }

                // Generar código MFA de 6 dígitos
                var codigoMfa = _mfaService.GenerarCodigoVerificacion();
                var expira = DateTime.UtcNow.AddMinutes(10); // 10 minutos de validez

                // Guardar el código MFA en la base de datos
                usuario.MfaCodigoVerificacion = codigoMfa;
                usuario.MfaCodigoExpira = expira;
                await _context.SaveChangesAsync();

                // Mostrar el código en la consola para pruebas
                Console.WriteLine($"=== CÓDIGO MFA PARA {email} ===");
                Console.WriteLine($"Código: {codigoMfa}");
                Console.WriteLine($"Expira: {expira}");
                Console.WriteLine("==========================================");

                // Enviar el código por correo real
                await EnviarCorreoCodigoMfa(email, codigoMfa, expira);

                return Json(new { 
                    success = true, 
                    message = $"Código de verificación enviado. Para pruebas, revisa la consola del servidor. Código: {codigoMfa}",
                    email = email 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general en EnviarCodigoRecuperacion: {ex.Message}");
                return Json(new { success = false, message = "Ocurrió un error interno. Por favor, intenta nuevamente o contacta al administrador del sistema." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReenviarCodigoMfa(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Json(new { success = false, message = "Por favor, ingresa un correo electrónico válido." });
                }
                if (!IsValidEmail(email))
                {
                    return Json(new { success = false, message = "Por favor, ingresa un formato de correo electrónico válido." });
                }
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email && u.Activo);
                if (usuario == null)
                {
                    return Json(new { success = false, message = "No se encontró una cuenta activa asociada a este correo electrónico." });
                }
                // Generar nuevo código MFA
                var codigoMfa = _mfaService.GenerarCodigoVerificacion();
                var expira = DateTime.UtcNow.AddMinutes(10);
                usuario.MfaCodigoVerificacion = codigoMfa;
                usuario.MfaCodigoExpira = expira;
                await _context.SaveChangesAsync();
                await EnviarCorreoCodigoMfa(email, codigoMfa, expira);
                return Json(new { success = true, message = "Nuevo código de verificación enviado. Revisa tu correo electrónico." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al reenviar el código MFA: {ex.Message}");
                return Json(new { success = false, message = "No se pudo reenviar el código. Intenta nuevamente." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerificarCodigoMfa(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    ViewBag.Error = "Email inválido.";
                    return View("RecuperarContrasena");
                }

                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email && u.Activo);
                if (usuario == null)
                {
                    ViewBag.Error = "Usuario no encontrado.";
                    return View("RecuperarContrasena");
                }

                if (string.IsNullOrEmpty(usuario.MfaCodigoVerificacion) || !usuario.MfaCodigoExpira.HasValue)
                {
                    ViewBag.Error = "No hay un código de verificación válido. Solicita uno nuevo.";
                    return View("RecuperarContrasena");
                }

                if (usuario.MfaCodigoExpira.Value < DateTime.UtcNow)
                {
                    ViewBag.Error = "El código de verificación ha expirado. Solicita uno nuevo.";
                    return View("RecuperarContrasena");
                }

                ViewBag.Email = email;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al procesar la verificación.";
                return View("RecuperarContrasena");
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerificarCodigoMfa(string email, string codigo)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(codigo))
                {
                    return Json(new { success = false, message = "Por favor, completa todos los campos." });
                }

                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email && u.Activo);
                if (usuario == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado." });
                }

                // Verificar el código MFA
                if (!_mfaService.VerificarCodigoVerificacion(codigo, usuario.MfaCodigoVerificacion, usuario.MfaCodigoExpira))
                {
                    return Json(new { success = false, message = "Código de verificación inválido o expirado." });
                }

                // Código válido, limpiar el código usado
                usuario.MfaCodigoVerificacion = null;
                usuario.MfaCodigoExpira = null;
                await _context.SaveChangesAsync();

                // Redirigir al formulario de nueva contraseña
                return Json(new { 
                    success = true, 
                    message = "Código verificado correctamente.",
                    redirectUrl = Url.Action("NuevaContrasenaConMfa", new { email = email })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al verificar el código." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> NuevaContrasenaConMfa(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    ViewBag.Error = "Email inválido.";
                    return View("RecuperarContrasena");
                }

                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email && u.Activo);
                if (usuario == null)
                {
                    ViewBag.Error = "Usuario no encontrado.";
                    return View("RecuperarContrasena");
                }

                ViewBag.Email = email;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al procesar la solicitud.";
                return View("RecuperarContrasena");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CambiarContrasenaConMfa(string email, string nuevaContrasena, string confirmarContrasena)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Email inválido." });
                }

                if (string.IsNullOrEmpty(nuevaContrasena) || string.IsNullOrEmpty(confirmarContrasena))
                {
                    return Json(new { success = false, message = "Por favor, completa todos los campos." });
                }

                if (nuevaContrasena != confirmarContrasena)
                {
                    return Json(new { success = false, message = "Las contraseñas no coinciden." });
                }

                if (nuevaContrasena.Length < 6)
                {
                    return Json(new { success = false, message = "La contraseña debe tener al menos 6 caracteres." });
                }

                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email && u.Activo);
                if (usuario == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado." });
                }

                // Actualizar contraseña
                usuario.PasswordHash = HashPassword(nuevaContrasena);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Contraseña cambiada exitosamente." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error al cambiar la contraseña." });
            }
        }

        // Métodos legacy para compatibilidad (mantener por ahora)
        [HttpGet]
        public async Task<IActionResult> RecuperarContrasenaConToken(string token, string email)
        {
            try
            {
                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                {
                    ViewBag.Error = "Enlace inválido o expirado.";
                    return View("RecuperarContrasena");
                }

                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => 
                    u.Email == email && 
                    u.CodigoRecuperacion == token && 
                    u.CodigoRecuperacionExpira > DateTime.UtcNow &&
                    u.Activo);

                if (usuario == null)
                {
                    ViewBag.Error = "Enlace inválido o expirado. Por favor, solicita un nuevo enlace de recuperación.";
                    return View("RecuperarContrasena");
                }

                // Token válido, mostrar formulario de nueva contraseña
                ViewBag.Token = token;
                ViewBag.Email = email;
                return View("NuevaContrasena");
            }
            catch (Exception)
            {
                ViewBag.Error = "Error al procesar el enlace de recuperación.";
                return View("RecuperarContrasena");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CambiarContrasenaConToken(string token, string email, string nuevaContrasena, string confirmarContrasena)
        {
            try
            {
                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Datos inválidos." });
                }

                if (string.IsNullOrEmpty(nuevaContrasena) || string.IsNullOrEmpty(confirmarContrasena))
                {
                    return Json(new { success = false, message = "Por favor, completa todos los campos." });
                }

                if (nuevaContrasena != confirmarContrasena)
                {
                    return Json(new { success = false, message = "Las contraseñas no coinciden." });
                }

                if (nuevaContrasena.Length < 6)
                {
                    return Json(new { success = false, message = "La contraseña debe tener al menos 6 caracteres." });
                }

                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => 
                    u.Email == email && 
                    u.CodigoRecuperacion == token && 
                    u.CodigoRecuperacionExpira > DateTime.UtcNow &&
                    u.Activo);

                if (usuario == null)
                {
                    return Json(new { success = false, message = "Enlace inválido o expirado. Por favor, solicita un nuevo enlace de recuperación." });
                }

                // Actualizar contraseña
                usuario.PasswordHash = HashPassword(nuevaContrasena);
                usuario.CodigoRecuperacion = null;
                usuario.CodigoRecuperacionExpira = null;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Contraseña cambiada exitosamente." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error al cambiar la contraseña." });
            }
        }

        private string GenerarTokenRecuperacion()
        {
            // Generar token único de 32 caracteres
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, 32).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void EnviarCorreoRecuperacion(string email, string codigo, DateTime expira)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("Email");
                
                // Validar configuración SMTP
                var host = smtpSettings["Host"];
                var port = smtpSettings["Port"];
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];
                var fromEmail = smtpSettings["From"];

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port) || 
                    string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || 
                    string.IsNullOrEmpty(fromEmail))
                {
                    throw new InvalidOperationException("Configuración SMTP incompleta. Verifica el archivo appsettings.json");
                }

                if (!int.TryParse(port, out int portNumber))
                {
                    throw new InvalidOperationException("Puerto SMTP inválido en la configuración");
                }

                // Verificar si la contraseña es la placeholder
                if (password == "tu_app_password_gmail")
                {
                    throw new InvalidOperationException("Configura la contraseña de aplicación de Gmail en appsettings.json");
                }

                // Aquí iría la lógica de envío de email usando MailKit
                // Por ahora, solo mostrar en consola
                Console.WriteLine($"=== EMAIL DE RECUPERACIÓN PARA {email} ===");
                Console.WriteLine($"Asunto: Recuperación de Contraseña - Sistema de Rendiciones");
                Console.WriteLine($"Mensaje: Hola, has solicitado recuperar tu contraseña. Haz clic en el siguiente enlace:");
                Console.WriteLine($"Enlace: {codigo}");
                Console.WriteLine($"Este enlace expira en {expira:dd-MM-yyyy HH:mm}.");
                Console.WriteLine("==========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar email: {ex.Message}");
                throw;
            }
        }

        private async Task EnviarCorreoCodigoMfa(string email, string codigo, DateTime expira)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("Email");
                var host = smtpSettings["Host"];
                var port = smtpSettings["Port"];
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];
                var fromEmail = smtpSettings["From"];

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fromEmail))
                    throw new InvalidOperationException("Configuración SMTP incompleta. Verifica el archivo appsettings.json");

                if (!int.TryParse(port, out int portNumber))
                    throw new InvalidOperationException("Puerto SMTP inválido en la configuración");

                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress("Sistema de Rendiciones Primar", fromEmail));
                message.To.Add(new MimeKit.MailboxAddress(email, email));
                message.Subject = "Código de verificación - Recuperación de Contraseña";

                var builder = new MimeKit.BodyBuilder();
                builder.HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; background: #f6f6f6; padding: 30px;'>
                    <div style='max-width: 480px; margin: auto; background: #fff; border-radius: 12px; box-shadow: 0 2px 12px rgba(0,0,0,0.08); padding: 32px;'>
                        <div style='text-align: center; margin-bottom: 24px;'>
                            <h2 style='color: #136489; margin: 0;'>Sistema de Rendiciones</h2>
                        </div>
                        <h3 style='color: #136489; text-align: center;'>Código de Verificación</h3>
                        <p style='font-size: 16px; color: #333; text-align: center;'>
                            Usa el siguiente código para recuperar tu contraseña:
                        </p>
                        <div style='font-size: 32px; font-weight: bold; color: #21759b; text-align: center; letter-spacing: 6px; margin: 24px 0;'>
                            {codigo}
                        </div>
                        <p style='font-size: 14px; color: #666; text-align: center;'>
                            Este código es válido hasta <b>{expira:dd-MM-yyyy HH:mm}</b>.<br>
                            Si no solicitaste este código, ignora este mensaje.
                        </p>
                        <hr style='margin: 24px 0;'>
                        <p style='font-size: 12px; color: #aaa; text-align: center;'>
                            © {DateTime.Now.Year} Primar S.A. - Sistema de Rendiciones
                        </p>
                    </div>
                </div>
                ";
                message.Body = builder.ToMessageBody();

                using var client = new MailKit.Net.Smtp.SmtpClient();
                await client.ConnectAsync(host, portNumber, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar el correo MFA: {ex.Message}");
                throw;
            }
        }

        [HttpGet]
        public IActionResult CambiarContrasenaEmpleado()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = _context.Usuarios.Find(userId);
            ViewBag.UserName = usuario != null ? ($"{usuario.Nombre} {usuario.Apellidos}").Trim() : "Usuario";
            ViewBag.NotificacionesNoLeidas = _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "empleado").Count();
            return View("CambiarContrasenaEmpleado");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasenaEmpleado(string contrasenaActual, string nuevaContrasena, string confirmarContrasena)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null || usuario.Rol != "empleado")
            {
                ModelState.AddModelError("", "Usuario no válido. Contacta al administrador.");
                Console.WriteLine($"[CambiarContrasenaEmpleado] Usuario no válido. userId={userId}");
                return View("CambiarContrasenaEmpleado");
            }
            if (string.IsNullOrEmpty(contrasenaActual) || string.IsNullOrEmpty(nuevaContrasena) || string.IsNullOrEmpty(confirmarContrasena))
            {
                ModelState.AddModelError("", "Todos los campos son obligatorios.");
                return View("CambiarContrasenaEmpleado");
            }
            if (!VerifyPassword(contrasenaActual, usuario.PasswordHash))
            {
                ModelState.AddModelError("", "La contraseña actual es incorrecta.");
                Console.WriteLine($"[CambiarContrasenaEmpleado] Contraseña actual incorrecta para usuario {usuario.Email}");
                return View("CambiarContrasenaEmpleado");
            }
            if (nuevaContrasena != confirmarContrasena)
            {
                ModelState.AddModelError("", "Las contraseñas nuevas no coinciden.");
                return View("CambiarContrasenaEmpleado");
            }
            if (nuevaContrasena.Length < 6)
            {
                ModelState.AddModelError("", "La nueva contraseña debe tener al menos 6 caracteres.");
                return View("CambiarContrasenaEmpleado");
            }
            if (VerifyPassword(nuevaContrasena, usuario.PasswordHash))
            {
                ModelState.AddModelError("", "La nueva contraseña no puede ser igual a la anterior.");
                return View("CambiarContrasenaEmpleado");
            }
            usuario.PasswordHash = HashPassword(nuevaContrasena);
            await _context.SaveChangesAsync();
            ViewBag.Success = "¡Contraseña cambiada exitosamente!";
            Console.WriteLine($"[CambiarContrasenaEmpleado] Contraseña cambiada para usuario {usuario.Email}");
            return View("CambiarContrasenaEmpleado");
        }

        [HttpGet]
        public IActionResult CambiarContrasenaSupervisor()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = _context.Usuarios.Find(userId);
            ViewBag.UserName = usuario != null ? ($"{usuario.Nombre} {usuario.Apellidos}").Trim() : "Usuario";
            ViewBag.NotificacionesNoLeidas = _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "supervisor").Count();
            return View("CambiarContrasenaSupervisor");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasenaSupervisor(string contrasenaActual, string nuevaContrasena, string confirmarContrasena)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = await _context.Usuarios.FindAsync(userId);
            ViewBag.UserName = usuario != null ? ($"{usuario.Nombre} {usuario.Apellidos}").Trim() : "Usuario";
            if (usuario == null || usuario.Rol != "aprobador1")
            {
                ModelState.AddModelError("", "Usuario no válido. Contacta al administrador.");
                Console.WriteLine($"[CambiarContrasenaSupervisor] Usuario no válido. userId={userId}");
                return View("CambiarContrasenaSupervisor");
            }
            if (string.IsNullOrEmpty(contrasenaActual) || string.IsNullOrEmpty(nuevaContrasena) || string.IsNullOrEmpty(confirmarContrasena))
            {
                ModelState.AddModelError("", "Todos los campos son obligatorios.");
                return View("CambiarContrasenaSupervisor");
            }
            if (!VerifyPassword(contrasenaActual, usuario.PasswordHash))
            {
                ModelState.AddModelError("", "La contraseña actual es incorrecta.");
                Console.WriteLine($"[CambiarContrasenaSupervisor] Contraseña actual incorrecta para usuario {usuario.Email}");
                return View("CambiarContrasenaSupervisor");
            }
            if (nuevaContrasena != confirmarContrasena)
            {
                ModelState.AddModelError("", "Las contraseñas nuevas no coinciden.");
                return View("CambiarContrasenaSupervisor");
            }
            if (nuevaContrasena.Length < 6)
            {
                ModelState.AddModelError("", "La nueva contraseña debe tener al menos 6 caracteres.");
                return View("CambiarContrasenaSupervisor");
            }
            if (VerifyPassword(nuevaContrasena, usuario.PasswordHash))
            {
                ModelState.AddModelError("", "La nueva contraseña no puede ser igual a la anterior.");
                return View("CambiarContrasenaSupervisor");
            }
            usuario.PasswordHash = HashPassword(nuevaContrasena);
            await _context.SaveChangesAsync();
            ViewBag.Success = "¡Contraseña cambiada exitosamente!";
            Console.WriteLine($"[CambiarContrasenaSupervisor] Contraseña cambiada para usuario {usuario.Email}");
            return View("CambiarContrasenaSupervisor");
        }

        [HttpGet]
        public IActionResult CambiarContrasenaGerente()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            var usuario = _context.Usuarios.Find(userId);
            ViewBag.UserName = usuario != null ? ($"{usuario.Nombre} {usuario.Apellidos}").Trim() : "Usuario";
            if (userRole.ToLower().Contains("gerente") || userRole.Equals("aprobador2", StringComparison.OrdinalIgnoreCase))
            {
                var notificacionesNoLeidas = _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "gerente").Count();
                ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;
            }
            return View("CambiarContrasenaGerente");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasenaGerente(string contrasenaActual, string nuevaContrasena, string confirmarContrasena)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            if (userRole.ToLower().Contains("gerente") || userRole.Equals("aprobador2", StringComparison.OrdinalIgnoreCase))
            {
                var notificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "gerente").CountAsync();
                ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;
            }
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null || usuario.Rol != "aprobador2")
            {
                ModelState.AddModelError("", "Usuario no válido. Contacta al administrador.");
                Console.WriteLine($"[CambiarContrasenaGerente] Usuario no válido. userId={userId}");
                return View("CambiarContrasenaGerente");
            }
            if (string.IsNullOrEmpty(contrasenaActual) || string.IsNullOrEmpty(nuevaContrasena) || string.IsNullOrEmpty(confirmarContrasena))
            {
                ModelState.AddModelError("", "Todos los campos son obligatorios.");
                return View("CambiarContrasenaGerente");
            }
            if (!VerifyPassword(contrasenaActual, usuario.PasswordHash))
            {
                ModelState.AddModelError("", "La contraseña actual es incorrecta.");
                Console.WriteLine($"[CambiarContrasenaGerente] Contraseña actual incorrecta para usuario {usuario.Email}");
                return View("CambiarContrasenaGerente");
            }
            if (nuevaContrasena != confirmarContrasena)
            {
                ModelState.AddModelError("", "Las contraseñas nuevas no coinciden.");
                return View("CambiarContrasenaGerente");
            }
            if (nuevaContrasena.Length < 6)
            {
                ModelState.AddModelError("", "La nueva contraseña debe tener al menos 6 caracteres.");
                return View("CambiarContrasenaGerente");
            }
            if (VerifyPassword(nuevaContrasena, usuario.PasswordHash))
            {
                ModelState.AddModelError("", "La nueva contraseña no puede ser igual a la anterior.");
                return View("CambiarContrasenaGerente");
            }
            usuario.PasswordHash = HashPassword(nuevaContrasena);
            await _context.SaveChangesAsync();
            ViewBag.Success = "¡Contraseña cambiada exitosamente!";
            Console.WriteLine($"[CambiarContrasenaGerente] Contraseña cambiada para usuario {usuario.Email}");
            return View("CambiarContrasenaGerente");
        }
    }
}