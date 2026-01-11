using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RendicionesPrimar.Data;
using RendicionesPrimar.Models;
using RendicionesPrimar.Models.ViewModels;
using System.Security.Claims;

namespace RendicionesPrimar.Controllers
{
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PerfilController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Método para obtener el nombre del usuario buscando en ambas tablas
        private async Task<string> ObtenerNombreUsuario(int userId)
        {
            // Primero buscar en la tabla usuarios
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario != null)
            {
                return usuario.Nombre + " " + usuario.Apellidos;
            }

            // Si no se encuentra en usuarios, buscar en aprobadores
            var aprobador = await _context.Set<Usuario>().FromSqlRaw("SELECT * FROM aprobadores WHERE id = {0}", userId).FirstOrDefaultAsync();
            return aprobador?.Nombre + " " + aprobador?.Apellidos ?? "Usuario";
        }

        // Método para obtener los datos completos del usuario buscando en ambas tablas
        private async Task<Usuario?> ObtenerUsuarioCompleto(int userId)
        {
            // Primero buscar en la tabla usuarios
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario != null)
            {
                return usuario;
            }

            // Si no se encuentra en usuarios, buscar en aprobadores
            var aprobador = await _context.Set<Usuario>().FromSqlRaw("SELECT * FROM aprobadores WHERE id = {0}", userId).FirstOrDefaultAsync();
            return aprobador;
        }

        [HttpGet]
        public async Task<IActionResult> InformacionPersonal()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var usuario = await ObtenerUsuarioCompleto(userId);

            if (usuario == null)
            {
                return NotFound();
            }

            var viewModel = new InformacionPersonalViewModel
            {
                Nombre = usuario.Nombre,
                Apellidos = usuario.Apellidos,
                Rut = usuario.Rut,
                Email = usuario.Email,
                Telefono = usuario.Telefono,
                Cargo = usuario.Cargo,
                Departamento = usuario.Departamento
            };

            var nombreUsuario = await ObtenerNombreUsuario(userId);
            ViewBag.UserName = nombreUsuario;
            ViewBag.NotificacionesNoLeidas = await _context.Notificaciones
                .Where(n => n.UsuarioId == userId && !n.Leido).CountAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> InformacionPersonal(InformacionPersonalViewModel model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            if (!ModelState.IsValid)
            {
                var usuario = await _context.Usuarios.FindAsync(userId);
                ViewBag.UserName = usuario?.Nombre + " " + usuario?.Apellidos ?? "Usuario";
                ViewBag.NotificacionesNoLeidas = await _context.Notificaciones
                    .Where(n => n.UsuarioId == userId && !n.Leido).CountAsync();
                return View(model);
            }

            var user = await _context.Usuarios.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Actualizar la información personal
            user.Nombre = model.Nombre;
            user.Apellidos = model.Apellidos;
            user.Rut = model.Rut;
            user.Email = model.Email;
            user.Telefono = model.Telefono;
            user.Cargo = model.Cargo;
            user.Departamento = model.Departamento;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Información personal actualizada exitosamente";
            return RedirectToAction("InformacionPersonal");
        }
    }
} 