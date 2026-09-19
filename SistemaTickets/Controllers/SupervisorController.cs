using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Domain.Services;
using SistemaTickets.Models.Supervisor;

namespace SistemaTickets.Controllers
{
    [Authorize(Roles = "Administrador,Supervisor")]
    public class SupervisorController : Controller
    {
        private readonly ITicketService _ticketService;

        public SupervisorController(ITicketService ticketService)
            => _ticketService = ticketService;

        [HttpGet]
        public async Task<IActionResult> Seguimiento()
        {
            var tickets = await _ticketService.GetActivosParaSeguimientoAsync();

            // Columnas de estados únicos presentes
            var estados = tickets
                .Select(t => t.Estado.Nombre)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            // Filas por técnico
            var porTecnico = tickets
                .GroupBy(t => t.AsignadoA != null
                    ? t.AsignadoA.NombreCompleto
                    : "(Sin asignar)")
                .Select(g => new TecnicoRow
                {
                    Nombre   = g.Key,
                    Total    = g.Count(),
                    PorEstado = estados.ToDictionary(
                        e => e,
                        e => g.Count(t => t.Estado.Nombre == e))
                })
                .OrderByDescending(r => r.Nombre == "(Sin asignar)" ? 0 : 1)
                .ThenBy(r => r.Nombre)
                .ToList();

            // Filas por prioridad
            var porPrioridad = tickets
                .Where(t => t.Prioridad != null)
                .GroupBy(t => new { t.Prioridad!.Nombre, t.Prioridad.Color })
                .Select(g => new PrioridadRow
                {
                    Nombre = g.Key.Nombre,
                    Color  = g.Key.Color,
                    Total  = g.Count()
                })
                .OrderByDescending(r => r.Total)
                .ToList();

            var vm = new SeguimientoViewModel
            {
                PorTecnico    = porTecnico,
                PorPrioridad  = porPrioridad,
                Estados       = estados,
                TotalActivos  = tickets.Count,
                SinAsignar    = tickets.Count(t => t.AsignadoA == null)
            };

            return View(vm);
        }
    }
}
