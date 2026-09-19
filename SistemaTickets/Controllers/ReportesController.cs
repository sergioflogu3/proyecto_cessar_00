using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Domain.Services;
using SistemaTickets.Models.Reportes;

namespace SistemaTickets.Controllers
{
    [Authorize(Roles = "Administrador,Supervisor")]
    public class ReportesController : Controller
    {
        private readonly IReporteService _svc;

        public ReportesController(IReporteService svc)
        {
            _svc = svc;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = new ReporteIndexViewModel
            {
                Estados     = await _svc.GetEstadosAsync(),
                Prioridades = await _svc.GetPrioridadesAsync(),
                Categorias  = await _svc.GetCategoriasAsync(),
                Tecnicos    = await _svc.GetTecnicosAsync(),
                Clientes    = await _svc.GetClientesAsync()
            };
            return View(vm);
        }

        // ── Descargas ─────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Descargar(string tipo, ReporteFiltro filtro)
        {
            byte[] data;
            string nombre;

            var fecha = DateTime.Now.ToString("yyyyMMdd");

            switch (tipo)
            {
                case "general":
                    data   = await _svc.GenerarReporteGeneralAsync(filtro);
                    nombre = $"tickets_general_{fecha}.xlsx";
                    break;
                case "estado":
                    data   = await _svc.GenerarReportePorEstadoAsync(filtro);
                    nombre = $"tickets_por_estado_{fecha}.xlsx";
                    break;
                case "prioridad":
                    data   = await _svc.GenerarReportePorPrioridadAsync(filtro);
                    nombre = $"tickets_por_prioridad_{fecha}.xlsx";
                    break;
                case "sinresolver":
                    data   = await _svc.GenerarReporteSinResolverAsync(filtro);
                    nombre = $"tickets_sin_resolver_{fecha}.xlsx";
                    break;
                case "productividad":
                    data   = await _svc.GenerarReporteProductividadAsync(filtro);
                    nombre = $"productividad_tecnicos_{fecha}.xlsx";
                    break;
                case "historial":
                    data   = await _svc.GenerarReporteHistorialAsync(filtro);
                    nombre = $"historial_tickets_{fecha}.xlsx";
                    break;
                case "comentarios":
                    data   = await _svc.GenerarReporteComentariosAsync(filtro);
                    nombre = $"comentarios_{fecha}.xlsx";
                    break;
                case "adjuntos":
                    data   = await _svc.GenerarReporteAdjuntosAsync(filtro);
                    nombre = $"adjuntos_{fecha}.xlsx";
                    break;
                case "usuarios":
                    data   = await _svc.GenerarReporteUsuariosAsync();
                    nombre = $"usuarios_{fecha}.xlsx";
                    break;
                default:
                    return BadRequest("Tipo de reporte no válido.");
            }

            return File(data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nombre);
        }

        [HttpGet]
        public async Task<IActionResult> DescargarPdf(string tipo, ReporteFiltro filtro)
        {
            var fecha = DateTime.Now.ToString("yyyyMMdd");

            string nombre = tipo switch
            {
                "general"        => $"tickets_general_{fecha}.pdf",
                "estado"         => $"tickets_por_estado_{fecha}.pdf",
                "prioridad"      => $"tickets_por_prioridad_{fecha}.pdf",
                "sinresolver"    => $"tickets_sin_resolver_{fecha}.pdf",
                "productividad"  => $"productividad_tecnicos_{fecha}.pdf",
                "historial"      => $"historial_tickets_{fecha}.pdf",
                "comentarios"    => $"comentarios_{fecha}.pdf",
                "adjuntos"       => $"adjuntos_{fecha}.pdf",
                "usuarios"       => $"usuarios_{fecha}.pdf",
                _                => null!
            };

            if (nombre is null) return BadRequest("Tipo de reporte no válido.");

            var data = await _svc.GenerarPdfAsync(tipo, filtro);
            return File(data, "application/pdf", nombre);
        }
    }
}
