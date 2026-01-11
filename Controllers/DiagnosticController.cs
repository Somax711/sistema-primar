using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RendicionesPrimar.Data;
using RendicionesPrimar.Models;
using System.Text;

namespace RendicionesPrimar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosticController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public DiagnosticController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            try
            {
                var result = new
                {
                    Status = "OK",
                    Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    Database = new
                    {
                        CanConnect = await _context.Database.CanConnectAsync(),
                        ConnectionString = _context.Database.GetConnectionString()?.Replace("Pwd=", "Pwd=***"),
                        TablesExist = await CheckTablesExist()
                    },
                    Configuration = new
                    {
                        HasEmailConfig = !string.IsNullOrEmpty(_configuration["Email:Host"]),
                        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = "ERROR",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace.Length))
                });
            }
        }

        [HttpGet("database")]
        public async Task<IActionResult> Database()
        {
            try
            {
                var tables = await CheckTablesExist();
                var counts = new Dictionary<string, int>();

                if (tables.ContainsKey("usuarios") && tables["usuarios"])
                {
                    counts["usuarios"] = await _context.Usuarios.CountAsync();
                }
                if (tables.ContainsKey("rendiciones") && tables["rendiciones"])
                {
                    counts["rendiciones"] = await _context.Rendiciones.CountAsync();
                }
                if (tables.ContainsKey("notificaciones") && tables["notificaciones"])
                {
                    counts["notificaciones"] = await _context.Notificaciones.CountAsync();
                }

                return Ok(new
                {
                    TablesExist = tables,
                    RecordCounts = counts,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    StackTrace = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace.Length))
                });
            }
        }

        [HttpPost("create-test-user")]
        public async Task<IActionResult> CreateTestUser()
        {
            try
            {
                // Verificar si ya existe un usuario de prueba
                var existingUser = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == "admin@test.com");

                if (existingUser != null)
                {
                    return Ok(new
                    {
                        Message = "Usuario de prueba ya existe",
                        User = new
                        {
                            existingUser.Id,
                            existingUser.Nombre,
                            existingUser.Email,
                            existingUser.Rol
                        }
                    });
                }

                // Crear usuario de prueba
                var testUser = new Usuario
                {
                    Nombre = "Admin",
                    Apellidos = "Test",
                    Email = "admin@test.com",
                    PasswordHash = "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=", // 123456
                    Rol = "admin",
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    Cargo = "Administrador",
                    Departamento = "IT"
                };

                _context.Usuarios.Add(testUser);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Usuario de prueba creado exitosamente",
                    User = new
                    {
                        testUser.Id,
                        testUser.Nombre,
                        testUser.Email,
                        testUser.Rol
                    },
                    Credentials = new
                    {
                        Email = "admin@test.com",
                        Password = "123456"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    StackTrace = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace.Length))
                });
            }
        }

        [HttpGet("routes")]
        public IActionResult Routes()
        {
            try
            {
                var routes = new List<string>
                {
                    "/Account/Login",
                    "/Home/Index",
                    "/api/diagnostic/health",
                    "/api/diagnostic/database",
                    "/api/diagnostic/create-test-user"
                };

                return Ok(new
                {
                    AvailableRoutes = routes,
                    CurrentUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        private async Task<Dictionary<string, bool>> CheckTablesExist()
        {
            var tables = new Dictionary<string, bool>();

            try
            {
                // Verificar cada tabla individualmente
                tables["usuarios"] = await TableExists("usuarios");
                tables["rendiciones"] = await TableExists("rendiciones");
                tables["notificaciones"] = await TableExists("notificaciones");
                tables["archivos_adjuntos"] = await TableExists("archivos_adjuntos");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking tables: {ex.Message}");
            }

            return tables;
        }

        private async Task<bool> TableExists(string tableName)
        {
            try
            {
                var sql = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{tableName}'";
                var count = await _context.Database.SqlQueryRaw<int>(sql).FirstOrDefaultAsync();
                return count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}