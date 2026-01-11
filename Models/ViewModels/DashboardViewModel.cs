namespace RendicionesPrimar.Models.ViewModels
{
    public class DashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int NotificacionesNoLeidas { get; set; }
        
        // Métricas principales
        public int TotalRendiciones { get; set; }
        public int? RendicionesPendientes { get; set; }
        public int? RendicionesAprobadas { get; set; }
        public int? RendicionesRechazadas { get; set; }
        public decimal MontoTotal { get; set; }
        
        // Para empleados
        public List<string> EstadisticasEmpleado { get; set; } = new List<string>();
        
        // Para aprobadores
        public List<Rendicion>? UltimasRendiciones { get; set; }
    }

    public class EstadisticaRendicion
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }
}
