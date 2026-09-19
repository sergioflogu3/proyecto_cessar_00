using NHibernate.Linq;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Services;
using SistemaTickets.Models.Dashboard;

namespace SistemaTickets.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly NHibernate.ISession _session;

        public DashboardService(NHibernate.ISession session) => _session = session;

        public async Task<DashboardViewModel> GetDashboardAsync(
            int? tecnicoId, bool esAdminOSupervisor, string? nombreTecnico = null)
        {
            var ahora          = DateTime.Now;   // hora local, coherente con SQL Server
            var hoy            = ahora.Date;
            var seisMesesAtras = ahora.AddMonths(-6);

            // ── Cargar tickets con referencias básicas ─────────────────────
            IQueryable<Ticket> q = _session.Query<Ticket>();

            if (tecnicoId.HasValue)
                q = q.Where(t => t.AsignadoA != null && t.AsignadoA.Id == tecnicoId.Value);

            var tickets = await q
                .Fetch(t => t.Estado)
                .Fetch(t => t.Prioridad)
                .Fetch(t => t.Categoria)
                .ToListAsync();

            var abiertos = tickets.Where(t => !t.FechaCierre.HasValue).ToList();
            var cerrados  = tickets.Where(t =>  t.FechaCierre.HasValue).ToList();

            var vm = new DashboardViewModel
            {
                EsAdminOSupervisor = esAdminOSupervisor,
                NombreTecnico      = nombreTecnico,
                TicketsAbiertos    = abiertos.Count,
                TicketsCerradosHoy = cerrados.Count(t => t.FechaCierre!.Value.Date == hoy),
                TicketsSinAsignar  = esAdminOSupervisor
                    ? abiertos.Count(t => t.AsignadoA == null)
                    : 0
            };

            // Tiempo promedio resolución
            if (cerrados.Any())
                vm.TiempoPromedioHoras = cerrados
                    .Average(t => (t.FechaCierre!.Value - t.FechaCreacion).TotalHours);

            // ── Gráficos por estado/prioridad/categoría ────────────────────
            vm.PorEstado = tickets
                .GroupBy(t => t.Estado.Nombre)
                .Select(g => new ChartItem
                {
                    Label = g.Key,
                    Value = g.Count(),
                    Color = g.First().Estado.Color
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            vm.PorPrioridad = tickets
                .Where(t => t.Prioridad != null)
                .GroupBy(t => t.Prioridad!.Nombre)
                .Select(g => new ChartItem
                {
                    Label = g.Key,
                    Value = g.Count(),
                    Color = g.First().Prioridad!.Color
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            vm.PorCategoria = tickets
                .GroupBy(t => t.Categoria.Nombre)
                .Select(g => new ChartItem { Label = g.Key, Value = g.Count() })
                .OrderByDescending(x => x.Value)
                .ToList();

            // ── Por técnico (solo admin/supervisor) ────────────────────────
            if (esAdminOSupervisor)
            {
                var conTec = await _session.Query<Ticket>()
                    .Where(t => t.AsignadoA != null)
                    .Fetch(t => t.AsignadoA)
                    .ToListAsync();

                vm.PorTecnico = conTec
                    .Where(t => t.AsignadoA != null)
                    .GroupBy(t => t.AsignadoA!.NombreCompleto)
                    .Select(g => new ChartItem { Label = g.Key, Value = g.Count() })
                    .OrderByDescending(x => x.Value)
                    .Take(10)
                    .ToList();
            }

            // ── Por mes: últimos 6 meses ───────────────────────────────────
            var meses = Enumerable.Range(0, 6)
                .Select(i => ahora.AddMonths(-5 + i))
                .ToList();

            var recientes = tickets.Where(t => t.FechaCreacion >= seisMesesAtras).ToList();

            vm.PorMes = meses.Select(m => new MesItem
            {
                Label   = m.ToString("MMM yy", new System.Globalization.CultureInfo("es-ES")),
                Creados  = recientes.Count(t =>
                    t.FechaCreacion.Year == m.Year && t.FechaCreacion.Month == m.Month),
                Cerrados = cerrados.Count(t =>
                    t.FechaCierre!.Value.Year == m.Year && t.FechaCierre.Value.Month == m.Month)
            }).ToList();

            // ── Visitas técnicas ───────────────────────────────────────────
            var visitaQ = _session.Query<VisitaTecnica>();
            if (tecnicoId.HasValue)
                visitaQ = visitaQ.Where(v => v.Tecnico.Id == tecnicoId.Value);

            var visitas = await visitaQ.ToListAsync();

            vm.VisitasPorEstado = visitas
                .GroupBy(v => v.Estado)
                .Select(g => new ChartItem { Label = g.Key, Value = g.Count() })
                .OrderByDescending(x => x.Value)
                .ToList();

            // ── Stock bajo mínimo (solo admin/supervisor) ──────────────────
            if (esAdminOSupervisor)
            {
                var bajo = await _session.Query<StockRepuesto>()
                    .Where(s => s.CantidadActual < s.CantidadMinima)
                    .Fetch(s => s.Repuesto)
                    .ToListAsync();

                vm.StockBajoMinimoCount = bajo.Count;
                vm.StockAlertas = bajo
                    .Select(s => $"{s.Repuesto.Nombre}: {s.CantidadActual}/{s.CantidadMinima}")
                    .ToList();
            }

            return vm;
        }
    }
}
