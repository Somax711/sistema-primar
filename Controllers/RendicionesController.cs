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
    [Authorize]
    public class RendicionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly RendicionService _rendicionService;
        private readonly EmailService _emailService;

        public RendicionesController(ApplicationDbContext context, IWebHostEnvironment environment, RendicionService rendicionService, EmailService emailService)
        {
            _context = context;
            _environment = environment;
            _rendicionService = rendicionService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("MisRendiciones", "Empleados");
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            if (userRole.ToLower().Contains("gerente") || userRole.Equals("aprobador2", StringComparison.OrdinalIgnoreCase))
            {
                var notificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "gerente").CountAsync();
                ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;
            }
            var usuario = await _context.Usuarios.FindAsync(userId);
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            await ActualizarNotificacionesNoLeidas(userId);

            var rendicion = await _context.Rendiciones.FirstOrDefaultAsync(r => r.Id == id);

            if (rendicion == null)
            {
                TempData["ErrorMessage"] = "La rendición solicitada no fue encontrada.";
                return RedirectToAction("Index");
            }

            // Obtener datos relacionados por separado
            var usuarioRendicion = await _context.Usuarios.FindAsync(rendicion.UsuarioId);
            var archivosAdjuntos = await _context.ArchivosAdjuntos
                .Where(a => a.RendicionId == rendicion.Id)
                .ToListAsync();

            // Si no se encuentra el usuario, crear uno temporal con datos básicos
            if (usuarioRendicion == null)
            {
                usuarioRendicion = new Usuario
                {
                    Id = rendicion.UsuarioId,
                    Email = "usuario@primar.cl", // Email por defecto
                    Nombre = rendicion.Nombre ?? "Usuario",
                    Apellidos = rendicion.Apellidos ?? string.Empty
                };
            }

            // Verificar permisos
            if (userRole == "empleado" && rendicion.UsuarioId != userId)
            {
                return Forbid();
            }

            // Determinar qué acciones puede realizar
            bool canApprove1 = (userRole == "aprobador1" || userRole == "supervisor") && rendicion.Estado == "pendiente";
            bool canApprove2 = (userRole == "aprobador2" || userRole == "gerente") && (rendicion.Estado == "aprobado_1" || rendicion.Estado == "rechazado_1");
            bool canMarkPaid = (userRole == "aprobador1" || userRole == "supervisor") && rendicion.Estado == "aprobado_2";
            bool canEdit = userRole == "empleado" && rendicion.Estado == "pendiente" && rendicion.UsuarioId == userId;

            var rendicionDetalleViewModel = new RendicionDetalleViewModel
            {
                Id = rendicion.Id,
                NumeroTicket = rendicion.NumeroTicket,
                Titulo = rendicion.Titulo ?? string.Empty,
                Descripcion = rendicion.Descripcion ?? string.Empty,
                MontoTotal = rendicion.MontoTotal,
                Estado = rendicion.Estado ?? string.Empty,
                FechaCreacion = rendicion.FechaCreacion,
                Nombre = rendicion.Nombre ?? string.Empty,
                Apellidos = rendicion.Apellidos ?? string.Empty,
                Rut = rendicion.Rut ?? string.Empty,
                Telefono = rendicion.Telefono ?? string.Empty,
                Cargo = rendicion.Cargo ?? string.Empty,
                Departamento = rendicion.Departamento ?? string.Empty,
                ComentariosAprobador = rendicion.ComentariosAprobador ?? string.Empty,
                Usuario = usuarioRendicion,
                ArchivosAdjuntos = archivosAdjuntos,
                CanApprove1 = canApprove1,
                CanApprove2 = canApprove2,
                CanMarkPaid = canMarkPaid,
                CanEdit = canEdit,
                UserRole = userRole ?? string.Empty,
                TimelineEvents = new List<string>(),
                MotivoRechazoSupervisor = rendicion.MotivoRechazoSupervisor ?? string.Empty,
                MotivoRechazoGerente = rendicion.MotivoRechazoGerente ?? string.Empty
            };

            // Redirigir a la vista correspondiente según el rol
            if (userRole == "empleado")
                return View("DetalleEmpleado", rendicionDetalleViewModel);
            else if (userRole == "aprobador1" || userRole == "supervisor")
                return View("DetalleSupervisor", rendicionDetalleViewModel);
            else if (userRole == "aprobador2" || userRole == "gerente")
            {
                var notificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "gerente").CountAsync();
                ViewBag.NotificacionesNoLeidas = notificacionesNoLeidas;
                return View("DetalleGerente", rendicionDetalleViewModel);
            }
            else
                return View("DetalleEmpleado", rendicionDetalleViewModel); // fallback
        }

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            ViewBag.UserName = await ObtenerNombreCompletoUsuario(userId);
            ViewBag.NotificacionesNoLeidas = await _context.Notificaciones.Where(n => n.UsuarioId == userId && !n.Leido && n.TipoRol == "empleado").CountAsync();
            if (userId == 0)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction("Index", "Home");
            }
            var usuario = await _context.Usuarios.FindAsync(userId);
            bool perfilIncompleto = string.IsNullOrWhiteSpace(usuario.Telefono) ||
                                    string.IsNullOrWhiteSpace(usuario.Cargo) ||
                                    string.IsNullOrWhiteSpace(usuario.Departamento);
            var model = new CrearRendicionViewModel
            {
                Nombre = usuario.Nombre,
                Apellidos = usuario.Apellidos,
                Rut = usuario.Rut,
                Telefono = usuario.Telefono,
                Cargo = usuario.Cargo,
                Departamento = usuario.Departamento
            };
            ViewBag.PerfilIncompleto = perfilIncompleto;
            if (perfilIncompleto)
            {
                ViewBag.MensajeEmergente = "<strong>¡Atención!</strong> Debe ingresar todos sus datos de perfil (Teléfono, Cargo y Departamento) para poder crear una nueva rendición.";
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(CrearRendicionViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var usuario = await _context.Usuarios.FindAsync(userId);

            if (usuario == null)
            {
                ModelState.AddModelError("", "No se pudo verificar la identidad del usuario.");
            }

            if (!ModelState.IsValid)
            {
                // Repoblar datos de solo lectura si el modelo no es válido
                var userForView = await _context.Usuarios.FindAsync(userId);
                if (userForView != null)
                {
                    model.Nombre = userForView.Nombre;
                    model.Apellidos = userForView.Apellidos;
                    model.Rut = userForView.Rut;
                    model.Telefono = userForView.Telefono;
                    model.Cargo = userForView.Cargo;
                    model.Departamento = userForView.Departamento;
                    ViewBag.UserName = userForView.Nombre;
                }
                return View(model);
            }

            var rendicion = await _rendicionService.CrearRendicionAsync(userId, model.Titulo, model.Descripcion, model.MontoTotal);

            if (model.Archivos != null && model.Archivos.Count > 0)
            {
                await ProcesarArchivosAdjuntos(rendicion.Id, model.Archivos);
            }

            TempData["SuccessMessage"] = $"Rendición {rendicion.NumeroTicket} creada exitosamente";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Aprobar(int id, string? comentarios)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";
            
            if (userRole == "aprobador1")
            {
                return await Aprobar1(id, comentarios);
            }
            else if (userRole == "aprobador2")
            {
                return await Aprobar2(id, comentarios);
            }
            
            return Forbid();
        }

        [HttpPost]
        public async Task<IActionResult> Rechazar(int id, string? comentarios)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";
            
            if (userRole == "aprobador1")
            {
                return await Rechazar1(id, comentarios);
            }
            else if (userRole == "aprobador2")
            {
                return await Rechazar2(id, comentarios);
            }
            
            return Forbid();
        }

        [HttpPost]
        public async Task<IActionResult> AprobarAjax([FromBody] AprobarRendicionRequest request)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                if (userRole == "aprobador1" || userRole == "supervisor")
                {
                    var ok = await _rendicionService.AprobarPrimeraInstanciaAsync(request.RendicionId, userId, request.Comentario);
                    if (ok)
                    {
                        return Json(new { success = true, message = "Rendición aprobada en primera instancia" });
                    }
                }
                else if (userRole == "aprobador2" || userRole == "gerente")
                {
                    var ok = await _rendicionService.AprobarSegundaInstanciaAsync(request.RendicionId, userId, request.Comentario);
                    if (ok)
                    {
                        return Json(new { success = true, message = "Rendición aprobada finalmente por el gerente" });
                    }
                }
                
                return Json(new { success = false, message = "No tienes permisos para aprobar rendiciones" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al aprobar la rendición: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RechazarAjax([FromBody] AprobarRendicionRequest request)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                if (userRole == "aprobador1" || userRole == "supervisor")
                {
                    var ok = await _rendicionService.RechazarAsync(request.RendicionId, request.Comentario ?? "Rechazada por el supervisor", userId);
                    if (ok)
                    {
                        return Json(new { success = true, message = "Rendición rechazada por el supervisor" });
                    }
                }
                else if (userRole == "aprobador2" || userRole == "gerente")
                {
                    var ok = await _rendicionService.RechazarAsync(request.RendicionId, request.Comentario ?? "Rechazada por el gerente", userId);
                    if (ok)
                    {
                        return Json(new { success = true, message = "Rendición rechazada por el gerente" });
                    }
                }
                
                return Json(new { success = false, message = "No tienes permisos para rechazar rendiciones" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al rechazar la rendición: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Aprobar1(int id, string? comentarios)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";
                if (userRole != "aprobador1" && userRole != "supervisor")
                {
                    TempData["ErrorMessage"] = "No tienes permisos para aprobar rendiciones";
                    return RedirectToAction("Detalle", new { id });
                }
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var ok = await _rendicionService.AprobarPrimeraInstanciaAsync(id, userId, comentarios);
                if (!ok)
                {
                    TempData["ErrorMessage"] = "No se pudo aprobar la rendición";
                    return RedirectToAction("Detalle", new { id });
                }
                TempData["SuccessMessage"] = "Rendición aprobada en primera instancia";
                return RedirectToAction("Detalle", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al aprobar la rendición: {ex.Message}";
                return RedirectToAction("Detalle", new { id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Aprobar2(int id, string? comentarios)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";
                if (userRole != "aprobador2" && userRole != "gerente")
                {
                    TempData["ErrorMessage"] = "No tienes permisos para aprobar rendiciones";
                    return RedirectToAction("Detalle", new { id });
                }
                var rendicion = await _context.Rendiciones.FindAsync(id);
                if (rendicion == null || (rendicion.Estado != "aprobado_1" && rendicion.Estado != "rechazado_1"))
                {
                    TempData["ErrorMessage"] = "No puedes aprobar esta rendición en su estado actual.";
                    return RedirectToAction("Detalle", new { id });
                }
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var ok = await _rendicionService.AprobarSegundaInstanciaAsync(id, userId, comentarios);
                if (!ok)
                {
                    TempData["ErrorMessage"] = "No se pudo aprobar la rendición";
                    return RedirectToAction("Detalle", new { id });
                }
                TempData["SuccessMessage"] = "Rendición aprobada finalmente";
                return RedirectToAction("Detalle", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al aprobar la rendición: {ex.Message}";
                return RedirectToAction("Detalle", new { id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarcarPagada(int id)
        {
            try
            {
                // Verificar que el usuario sea aprobador1
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";
                if (userRole != "aprobador1" && userRole != "supervisor")
                {
                    TempData["ErrorMessage"] = "No tienes permisos para marcar rendiciones como pagadas";
                    return RedirectToAction("Detalle", new { id });
                }

                var rendicion = await _context.Rendiciones.FirstOrDefaultAsync(r => r.Id == id);

                if (rendicion == null)
                {
                    TempData["ErrorMessage"] = "Rendición no encontrada";
                    return RedirectToAction("Index");
                }

                if (rendicion.Estado != "aprobado_2")
                {
                    TempData["ErrorMessage"] = "La rendición debe estar aprobada finalmente para marcarla como pagada";
                    return RedirectToAction("Detalle", new { id });
                }

                rendicion.Estado = "pagado";
                rendicion.FechaPago = DateTime.Now;

                await _context.SaveChangesAsync();

                // Notificar al empleado
                await CrearNotificacionEmpleado(rendicion, $"Tu rendición {rendicion.NumeroTicket} ha sido marcada como pagada.");
                await CrearNotificacionPagado(rendicion);

                // Enviar correo de rendición pagada
                var usuario = await _context.Usuarios.FindAsync(rendicion.UsuarioId);
                if (usuario != null && !string.IsNullOrEmpty(usuario.Email))
                {
                    await _emailService.EnviarRendicionPagadaAsync(
                        usuario.Email,
                        rendicion.NumeroTicket ?? "",
                        rendicion.MontoTotal.ToString(),
                        $"{usuario.Nombre} {usuario.Apellidos}"
                    );
                }

                TempData["SuccessMessage"] = "Rendición marcada como pagada";
                return RedirectToAction("Detalle", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al marcar la rendición como pagada: {ex.Message}";
                return RedirectToAction("Detalle", new { id });
            }
        }

        public async Task<IActionResult> DescargarArchivo(int id)
        {
            var archivo = await _context.ArchivosAdjuntos.FirstOrDefaultAsync(a => a.Id == id);

            if (archivo == null)
            {
                return NotFound();
            }

            // Verificar permisos - Obtener rendición por separado
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";
            var rendicionDelArchivo = await _context.Rendiciones.FindAsync(archivo.RendicionId);

            if (userRole == "empleado" && rendicionDelArchivo != null && rendicionDelArchivo.UsuarioId != userId)
            {
                return Forbid();
            }

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", archivo.RutaArchivo ?? "");
            
            if (!System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "El archivo no se encuentra disponible";
                return RedirectToAction("Detalle", new { id = archivo.RendicionId });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            
            return File(fileBytes, archivo.TipoArchivo ?? "application/octet-stream", archivo.NombreArchivo);
        }

        private async Task<string> GenerarNumeroTicket()
        {
            string ticket;
            bool existe;
            
            do
            {
                var random = new Random();
                ticket = $"RND-{random.Next(100000, 999999)}";
                existe = await _context.Rendiciones.AnyAsync(r => r.NumeroTicket == ticket);
            }
            while (existe);
            
            return ticket;
        }

        private async Task ProcesarArchivosAdjuntos(int rendicionId, List<IFormFile> archivos)
        {
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            foreach (var archivo in archivos)
            {
                if (archivo.Length > 0)
                {
                    var fileName = $"{Guid.NewGuid()}_{archivo.FileName}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await archivo.CopyToAsync(stream);
                    }

                    var archivoAdjunto = new ArchivoAdjunto
                    {
                        RendicionId = rendicionId,
                        NombreArchivo = archivo.FileName,
                        RutaArchivo = fileName,
                        TipoArchivo = archivo.ContentType,
                        TamanoArchivo = archivo.Length,
                        FechaSubida = DateTime.Now
                    };

                    _context.ArchivosAdjuntos.Add(archivoAdjunto);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task CrearNotificacionAprobacion(Rendicion rendicion, Usuario? usuario)
        {
            var supervisores = await _context.Usuarios
                .Where(a => a.Rol == "aprobador1")
                .ToListAsync();

            var nombreUsuario = usuario?.Nombre + " " + usuario?.Apellidos ?? "Usuario";
            foreach (var supervisor in supervisores)
            {
                var notificacion = new Notificacion
                {
                    UsuarioId = supervisor.Id,
                    RendicionId = rendicion.Id,
                    Mensaje = $"Nueva rendición {rendicion.NumeroTicket} de {nombreUsuario} requiere aprobación.",
                    Leido = false,
                    FechaCreacion = DateTime.Now,
                    TipoRol = "supervisor"
                };
                _context.Notificaciones.Add(notificacion);
            }
            await _context.SaveChangesAsync();
        }

        private async Task CrearNotificacionSegundaAprobacion(Rendicion rendicion)
        {
            var gerente = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == rendicion.UsuarioId && (u.Rol == "aprobador2" || u.Rol == "gerente"));

            if (gerente != null)
            {
                // Obtener usuario por separado
                var nombreUsuario = gerente.Nombre + " " + gerente.Apellidos ?? "Usuario";
                var notificacion = new Notificacion
                {
                    UsuarioId = gerente.Id,
                    RendicionId = rendicion.Id,
                    Mensaje = $"Rendición {rendicion.NumeroTicket} de {nombreUsuario} requiere tu aprobación final.",
                    Leido = false,
                    FechaCreacion = DateTime.Now,
                    TipoRol = "gerente"
                };
                _context.Notificaciones.Add(notificacion);
                await _context.SaveChangesAsync();
            }
        }

        private async Task CrearNotificacionParaPago(Rendicion rendicion)
        {
            var supervisor = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == rendicion.UsuarioId && (u.Rol == "aprobador1" || u.Rol == "supervisor"));

            if (supervisor != null)
            {
                var notificacion = new Notificacion
                {
                    UsuarioId = supervisor.Id,
                    RendicionId = rendicion.Id,
                    Mensaje = $"Rendición {rendicion.NumeroTicket} aprobada finalmente. Proceder con el pago.",
                    Leido = false,
                    FechaCreacion = DateTime.Now,
                    TipoRol = "supervisor"
                };
                _context.Notificaciones.Add(notificacion);
                await _context.SaveChangesAsync();
            }
        }

        private async Task CrearNotificacionPagado(Rendicion rendicion)
        {
            var notificacion = new Notificacion
            {
                UsuarioId = rendicion.UsuarioId,
                RendicionId = rendicion.Id,
                Mensaje = $"Su rendición {rendicion.NumeroTicket} ha sido pagada exitosamente.",
                Leido = false,
                FechaCreacion = DateTime.Now,
                TipoRol = "empleado"
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();
        }

        private async Task CrearNotificacionEmpleado(Rendicion rendicion, string mensaje)
        {
            var notificacion = new Notificacion
            {
                UsuarioId = rendicion.UsuarioId,
                RendicionId = rendicion.Id,
                Mensaje = mensaje,
                Leido = false,
                FechaCreacion = DateTime.Now,
                TipoRol = "empleado"
            };
            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            // Enviar correo bonito al empleado
            var usuario = await _context.Usuarios.FindAsync(rendicion.UsuarioId);
            if (usuario != null && !string.IsNullOrEmpty(usuario.Email))
            {
                var cuerpoCorreo = $@"
                    <div style='font-family:Segoe UI,Arial,sans-serif; background:#f7fafd; border-radius:16px; box-shadow:0 2px 12px #0001; max-width:520px; margin:32px auto; padding:32px 28px;'>
                        <div style='text-align:center; margin-bottom:18px;'>
                            <img src='/images/image.png' alt='Primar Logo' style='width:140px; height:auto; margin-bottom:10px; display:block; margin-left:auto; margin-right:auto;'>
                        </div>
                        <div style='background:#fff; border-radius:12px; padding:28px 22px 18px 22px; box-shadow:0 1px 4px #0001;'>
                            <p style='font-size:1.1rem; margin-bottom:10px;'>Hola <strong>{usuario.Nombre} {usuario.Apellidos}</strong>,</p>
                            <p style='font-size:1.05rem; color:#222; margin-bottom:18px;'>{mensaje}</p>
                            <div style='text-align:center; margin:24px 0;'>
                                <a href='#' style='background:#2a3b8f; color:#fff; text-decoration:none; padding:12px 28px; border-radius:6px; font-weight:600; font-size:1rem; display:inline-block;'>Ingresar al sistema</a>
                            </div>
                            <hr style='border:none; border-top:1px solid #e0e0e0; margin:18px 0;'>
                            <div style='color:#888; font-size:0.95rem; text-align:center;'>
                                Este es un mensaje automático del Sistema de Rendiciones Primar.<br>Por favor, no respondas a este correo.
                            </div>
                        </div>
                    </div>";
                await _emailService.EnviarNotificacionAsync(usuario.Email, "Tu rendición fue pagada", cuerpoCorreo);
            }

            // Enviar correo bonito a Don Juan (buscar por nombre o email)
            var donJuan = await _context.Usuarios.FirstOrDefaultAsync(u => u.Nombre.Contains("Juan") || u.Email.Contains("juan@primar.cl"));
            if (donJuan != null && !string.IsNullOrEmpty(donJuan.Email))
            {
                var cuerpoCorreo = $@"
                    <div style='font-family:Segoe UI,Arial,sans-serif; background:#f7fafd; border-radius:16px; box-shadow:0 2px 12px #0001; max-width:520px; margin:32px auto; padding:32px 28px;'>
                        <div style='text-align:center; margin-bottom:18px;'>
                            <img src='/images/image.png' alt='Primar Logo' style='width:140px; height:auto; margin-bottom:10px; display:block; margin-left:auto; margin-right:auto;'>
                        </div>
                        <div style='background:#fff; border-radius:12px; padding:28px 22px 18px 22px; box-shadow:0 1px 4px #0001;'>
                            <p style='font-size:1.1rem; margin-bottom:10px;'>Hola <strong>{donJuan.Nombre} {donJuan.Apellidos}</strong>,</p>
                            <p style='font-size:1.05rem; color:#222; margin-bottom:18px;'>La rendición {rendicion.NumeroTicket} de {usuario?.Nombre} {usuario?.Apellidos} ha sido marcada como pagada.</p>
                            <div style='text-align:center; margin:24px 0;'>
                                <a href='#' style='background:#2a3b8f; color:#fff; text-decoration:none; padding:12px 28px; border-radius:6px; font-weight:600; font-size:1rem; display:inline-block;'>Ingresar al sistema</a>
                            </div>
                            <hr style='border:none; border-top:1px solid #e0e0e0; margin:18px 0;'>
                            <div style='color:#888; font-size:0.95rem; text-align:center;'>
                                Este es un mensaje automático del Sistema de Rendiciones Primar.<br>Por favor, no respondas a este correo.
                            </div>
                        </div>
                    </div>";
                await _emailService.EnviarNotificacionAsync(donJuan.Email, "Se pagó una rendición", cuerpoCorreo);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Rechazar1(int id, string? comentarios)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";
            if (userRole != "aprobador1" && userRole != "supervisor")
            {
                TempData["ErrorMessage"] = "No tienes permisos para rechazar rendiciones";
                return RedirectToAction("Detalle", new { id });
            }
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var ok = await _rendicionService.RechazarAsync(id, comentarios ?? "Rechazada por el supervisor", userId);
            if (!ok)
            {
                TempData["ErrorMessage"] = "No se pudo rechazar la rendición";
                return RedirectToAction("Detalle", new { id });
            }
            TempData["SuccessMessage"] = "Rendición rechazada";
            return RedirectToAction("Detalle", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Rechazar2(int id, string? comentarios)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";
            if (userRole != "aprobador2" && userRole != "gerente")
            {
                TempData["ErrorMessage"] = "No tienes permisos para rechazar rendiciones";
                return RedirectToAction("Detalle", new { id });
            }
            var rendicion = await _context.Rendiciones.FindAsync(id);
            if (rendicion == null || (rendicion.Estado != "aprobado_1" && rendicion.Estado != "rechazado_1"))
            {
                TempData["ErrorMessage"] = "No puedes rechazar esta rendición en su estado actual.";
                return RedirectToAction("Detalle", new { id });
            }
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var ok = await _rendicionService.RechazarAsync(id, comentarios ?? "Rechazada por el gerente", userId);
            if (!ok)
            {
                TempData["ErrorMessage"] = "No se pudo rechazar la rendición";
                return RedirectToAction("Detalle", new { id });
            }
            TempData["SuccessMessage"] = "Rendición rechazada";
            return RedirectToAction("Detalle", new { id });
        }

        [HttpPost]
        [AjaxAuthorize]
        public async Task<IActionResult> EliminarRendicion(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";

            var rendicion = await _context.Rendiciones
                .Include(r => r.ArchivosAdjuntos)
                .Include(r => r.Notificaciones)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rendicion == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "La rendición no fue encontrada o ya fue eliminada." });
                TempData["ErrorMessage"] = "La rendición no fue encontrada o ya fue eliminada.";
                
                // Redirigir según el rol del usuario
                if (userRole == "empleado")
                {
                    return RedirectToAction("MisRendiciones", "Empleados");
                }
                else
                {
                    return RedirectToAction("Historial", "Supervisores");
                }
            }

            // Verificar que solo se pueda eliminar rendiciones en estado "aprobado_2", "rechazado" o "pagado"
            if (rendicion.Estado != "aprobado_2" && rendicion.Estado != "rechazado" && rendicion.Estado != "pagado")
            {
                string mensajeError;
                if (userRole == "empleado")
                {
                    mensajeError = "No puedes eliminar una rendición pendiente. Solo puedes eliminar rendiciones que ya estén aprobadas o rechazadas.";
                }
                else
                {
                    mensajeError = "Solo se pueden eliminar rendiciones que estén aprobadas, rechazadas o pagadas.";
                }
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = mensajeError });
                TempData["ErrorMessage"] = mensajeError;
                
                // Redirigir según el rol del usuario
                if (userRole == "empleado")
                {
                    return RedirectToAction("MisRendiciones", "Empleados");
                }
                else
                {
                    return RedirectToAction("Historial", "Supervisores");
                }
            }

            // Verificar permisos
            bool canDelete = false;
            if (userRole == "empleado")
            {
                canDelete = rendicion.UsuarioId == userId;
            }
            else if (userRole == "aprobador1" || userRole == "supervisor" || userRole == "aprobador2" || userRole == "gerente")
            {
                canDelete = true;
            }

            if (!canDelete)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "No tienes permisos para eliminar esta rendición." });
                TempData["ErrorMessage"] = "No tienes permisos para eliminar esta rendición.";
                
                // Redirigir según el rol del usuario
                if (userRole == "empleado")
                {
                    return RedirectToAction("MisRendiciones", "Empleados");
                }
                else
                {
                    return RedirectToAction("Historial", "Supervisores");
                }
            }

            try
            {
                if (rendicion.Estado == "pagado")
                {
                    // Solo marcar como eliminada por el empleado
                    rendicion.EliminadaPorEmpleado = true;
                    await _context.SaveChangesAsync();

                                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = "Rendición eliminada correctamente (solo para el empleado, sigue visible para aprobadores)." });
                TempData["SuccessMessage"] = "Rendición eliminada correctamente (solo para el empleado, sigue visible para aprobadores).";
                
                // Redirigir según el rol del usuario
                if (userRole == "empleado")
                {
                    return RedirectToAction("MisRendiciones", "Empleados");
                }
                else
                {
                    return RedirectToAction("Historial", "Supervisores");
                }
                }

                // Eliminar archivos físicos primero
                foreach (var archivo in rendicion.ArchivosAdjuntos)
                {
                    var rutaCompleta = Path.Combine(_environment.WebRootPath, "uploads", archivo.RutaArchivo);
                    if (System.IO.File.Exists(rutaCompleta))
                    {
                        System.IO.File.Delete(rutaCompleta);
                    }
                }

                // Eliminar notificaciones relacionadas manualmente
                var notificaciones = await _context.Notificaciones
                    .Where(n => n.RendicionId == rendicion.Id)
                    .ToListAsync();
                
                if (notificaciones.Any())
                {
                    _context.Notificaciones.RemoveRange(notificaciones);
                    await _context.SaveChangesAsync();
                }

                // Eliminar archivos adjuntos de la base de datos
                var archivosAdjuntos = await _context.ArchivosAdjuntos
                    .Where(a => a.RendicionId == rendicion.Id)
                    .ToListAsync();
                
                if (archivosAdjuntos.Any())
                {
                    _context.ArchivosAdjuntos.RemoveRange(archivosAdjuntos);
                    await _context.SaveChangesAsync();
                }

                // Finalmente eliminar la rendición
                _context.Rendiciones.Remove(rendicion);
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = "Rendición eliminada correctamente." });
                TempData["SuccessMessage"] = "Rendición eliminada correctamente.";
                
                // Redirigir según el rol del usuario
                if (userRole == "empleado")
                {
                    return RedirectToAction("MisRendiciones", "Empleados");
                }
                else
                {
                    return RedirectToAction("Historial", "Supervisores");
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "La rendición ya fue eliminada por otro proceso." });
                TempData["ErrorMessage"] = "La rendición ya fue eliminada por otro proceso.";
                
                // Redirigir según el rol del usuario
                if (userRole == "empleado")
                {
                    return RedirectToAction("MisRendiciones", "Empleados");
                }
                else
                {
                    return RedirectToAction("Historial", "Supervisores");
                }
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Error inesperado: " + ex.Message });
                TempData["ErrorMessage"] = "Error inesperado: " + ex.Message;
                
                // Redirigir según el rol del usuario
                if (userRole == "empleado")
                {
                    return RedirectToAction("MisRendiciones", "Empleados");
                }
                else
                {
                    return RedirectToAction("Historial", "Supervisores");
                }
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
        public async Task<IActionResult> Editar(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "empleado";

            var rendicion = await _context.Rendiciones.FindAsync(id);
            if (rendicion == null)
            {
                TempData["ErrorMessage"] = "La rendición solicitada no fue encontrada.";
                return RedirectToAction("Index");
            }

            // Verificar permisos - solo el empleado que creó la rendición puede editarla
            if (rendicion.UsuarioId != userId)
            {
                TempData["ErrorMessage"] = "No tienes permisos para editar esta rendición.";
                return RedirectToAction("Index");
            }

            // Verificar que la rendición esté en estado editable
            if (rendicion.Estado != "pendiente")
            {
                TempData["ErrorMessage"] = "Solo se pueden editar rendiciones pendientes.";
                return RedirectToAction("Detalle", new { id });
            }

            // Obtener información del usuario
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            // Obtener archivos adjuntos existentes
            var archivosExistentes = await _context.ArchivosAdjuntos
                .Where(a => a.RendicionId == rendicion.Id)
                .ToListAsync();

            var model = new CrearRendicionViewModel
            {
                Titulo = rendicion.Titulo ?? "",
                Descripcion = rendicion.Descripcion ?? "",
                MontoTotal = rendicion.MontoTotal,
                Nombre = usuario.Nombre ?? "",
                Apellidos = usuario.Apellidos ?? "",
                Rut = usuario.Rut ?? "",
                Telefono = usuario.Telefono ?? "",
                Cargo = usuario.Cargo ?? "",
                Departamento = usuario.Departamento ?? ""
            };
            
            ViewBag.RendicionId = id;
            ViewBag.ArchivosExistentes = archivosExistentes;
            ViewBag.UserName = usuario.Nombre;
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, CrearRendicionViewModel model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (id <= 0) return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.RendicionId = id;
                return View(model);
            }

            var rendicion = await _context.Rendiciones.FindAsync(id);
            if (rendicion == null)
            {
                TempData["ErrorMessage"] = "La rendición solicitada no fue encontrada.";
                return RedirectToAction("Index");
            }

            // Verificar permisos
            if (rendicion.UsuarioId != userId)
            {
                TempData["ErrorMessage"] = "No tienes permisos para editar esta rendición.";
                return RedirectToAction("Index");
            }

            // Verificar que la rendición esté en estado editable
            if (rendicion.Estado != "pendiente")
            {
                TempData["ErrorMessage"] = "Solo se pueden editar rendiciones pendientes.";
                return RedirectToAction("Detalle", new { id });
            }

            // Actualizar la rendición
            rendicion.Titulo = model.Titulo;
            rendicion.Descripcion = model.Descripcion;
            rendicion.MontoTotal = model.MontoTotal;
            
            await _context.SaveChangesAsync();

            // Procesar archivos adjuntos si se proporcionaron
            if (Request.Form.Files.Count > 0)
            {
                var archivos = Request.Form.Files.ToList();
                await ProcesarArchivosAdjuntos(rendicion.Id, archivos);
            }
            
            TempData["SuccessMessage"] = "Rendición actualizada exitosamente.";
            return RedirectToAction("Detalle", new { id = id });
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