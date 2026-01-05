using Microsoft.AspNetCore.SignalR;
using RendicionesPrimar.Services;

namespace RendicionesPrimar
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // ... otros servicios ...
            services.AddSignalR();
            services.AddScoped<RendicionesPrimar.Services.RendicionService>();
            // ... otros servicios ...
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // ... configuración existente ...
            app.UseEndpoints(endpoints =>
            {
                // ... otros endpoints ...
                endpoints.MapHub<NotificacionesHub>("/notificacionesHub");
            });
        }
    }
} 