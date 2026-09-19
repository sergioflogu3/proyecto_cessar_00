using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaTickets.Domain.Entities;
using SistemaTickets.Domain.Repositories;
using SistemaTickets.Domain.Services;
using SistemaTickets.Models.Reportes;

namespace SistemaTickets.Infrastructure.Services
{
    public class ReporteService : IReporteService
    {
        private readonly IReporteRepository _repo;
        private readonly ITicketRepository  _ticketRepo;

        public ReporteService(IReporteRepository repo, ITicketRepository ticketRepo)
        {
            _repo       = repo;
            _ticketRepo = ticketRepo;
        }

        // ── Catálogos ─────────────────────────────────────────────────────────

        public Task<IList<TicketEstado>>    GetEstadosAsync()    => _ticketRepo.GetEstadosAsync();
        public Task<IList<TicketPrioridad>> GetPrioridadesAsync() => _ticketRepo.GetPrioridadesAsync();
        public Task<IList<TicketCategoria>> GetCategoriasAsync()  => _ticketRepo.GetCategoriasAsync();
        public Task<IList<Usuario>>         GetTecnicosAsync()    => _repo.GetTecnicosAsync();
        public Task<IList<Usuario>>         GetClientesAsync()    => _repo.GetClientesAsync();

        // ── Helpers Excel ─────────────────────────────────────────────────────

        private static void EstilizarEncabezado(IXLWorksheet ws, int columnas)
        {
            var rng = ws.Range(1, 1, 1, columnas);
            rng.Style.Font.Bold = true;
            rng.Style.Font.FontColor = XLColor.White;
            rng.Style.Fill.BackgroundColor = XLColor.FromHtml("#198754");
            rng.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void FinalizarHoja(IXLWorksheet ws)
        {
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }

        private static byte[] ToBytes(XLWorkbook wb)
        {
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private static string F(DateTime? dt) => dt.HasValue
            ? dt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "—";

        private static string FD(DateTime? dt) => dt.HasValue
            ? dt.Value.ToLocalTime().ToString("dd/MM/yyyy") : "—";

        private static int DiasAbierto(Ticket t)
            => t.FechaCierre.HasValue
                ? (int)(t.FechaCierre.Value - t.FechaCreacion).TotalDays
                : (int)(DateTime.Now - t.FechaCreacion).TotalDays;

        // ── 1. Reporte General ────────────────────────────────────────────────

        public async Task<byte[]> GenerarReporteGeneralAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var tickets = await _repo.GetTicketsAsync(desde, hasta,
                filtro.EstadoId, filtro.PrioridadId, filtro.CategoriaId,
                filtro.ClienteId, filtro.TecnicoId);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("General");

            string[] headers = { "#", "Código", "Título", "Categoría", "Prioridad",
                                  "Estado", "Solicitante", "Técnico", "Fecha Creación",
                                  "Fecha Cierre", "Días Abierto" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
            EstilizarEncabezado(ws, headers.Length);

            int row = 2, num = 1;
            foreach (var t in tickets)
            {
                ws.Cell(row, 1).Value  = num++;
                ws.Cell(row, 2).Value  = $"#{t.Id}";
                ws.Cell(row, 3).Value  = t.Titulo;
                ws.Cell(row, 4).Value  = t.Categoria?.Nombre ?? "Sin categoría";
                ws.Cell(row, 5).Value  = t.Prioridad?.Nombre ?? "Sin prioridad";
                ws.Cell(row, 6).Value  = t.Estado.Nombre;
                ws.Cell(row, 7).Value  = t.CreadoPor.NombreCompleto;
                ws.Cell(row, 8).Value  = t.AsignadoA?.NombreCompleto ?? "Sin asignar";
                ws.Cell(row, 9).Value  = F(t.FechaCreacion);
                ws.Cell(row, 10).Value = F(t.FechaCierre);
                ws.Cell(row, 11).Value = DiasAbierto(t);
                row++;
            }

            FinalizarHoja(ws);
            return ToBytes(wb);
        }

        // ── 2. Tickets por Estado ─────────────────────────────────────────────

        public async Task<byte[]> GenerarReportePorEstadoAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var tickets = await _repo.GetTicketsAsync(desde, hasta, null, null, null, null, null);

            var grupos = tickets
                .GroupBy(t => t.Estado.Nombre)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .ToList();
            int total = grupos.Sum(x => x.Cantidad);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Por Estado");

            ws.Cell(1, 1).Value = "Estado";
            ws.Cell(1, 2).Value = "Cantidad";
            ws.Cell(1, 3).Value = "Porcentaje";
            EstilizarEncabezado(ws, 3);

            int row = 2;
            foreach (var g in grupos)
            {
                ws.Cell(row, 1).Value = g.Estado;
                ws.Cell(row, 2).Value = g.Cantidad;
                ws.Cell(row, 3).Value = total > 0
                    ? $"{(g.Cantidad * 100.0 / total):N1}%"
                    : "0%";
                row++;
            }

            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Value = total;
            ws.Cell(row, 2).Style.Font.Bold = true;
            ws.Cell(row, 3).Value = "100%";
            ws.Cell(row, 3).Style.Font.Bold = true;

            FinalizarHoja(ws);
            return ToBytes(wb);
        }

        // ── 3. Tickets por Prioridad ──────────────────────────────────────────

        public async Task<byte[]> GenerarReportePorPrioridadAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var tickets = await _repo.GetTicketsAsync(desde, hasta,
                filtro.EstadoId, null, null, null, null);

            var grupos = tickets
                .GroupBy(t => t.Prioridad?.Nombre ?? "Sin prioridad")
                .Select(g => new
                {
                    Prioridad  = g.Key,
                    Total      = g.Count(),
                    Abiertos   = g.Count(t => !t.FechaCierre.HasValue),
                    Cerrados   = g.Count(t =>  t.FechaCierre.HasValue)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Por Prioridad");

            ws.Cell(1, 1).Value = "Prioridad";
            ws.Cell(1, 2).Value = "Total";
            ws.Cell(1, 3).Value = "Abiertos";
            ws.Cell(1, 4).Value = "Cerrados/Resueltos";
            EstilizarEncabezado(ws, 4);

            int row = 2;
            foreach (var g in grupos)
            {
                ws.Cell(row, 1).Value = g.Prioridad;
                ws.Cell(row, 2).Value = g.Total;
                ws.Cell(row, 3).Value = g.Abiertos;
                ws.Cell(row, 4).Value = g.Cerrados;
                row++;
            }

            FinalizarHoja(ws);
            return ToBytes(wb);
        }

        // ── 4. Tickets sin resolver ───────────────────────────────────────────

        public async Task<byte[]> GenerarReporteSinResolverAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var todos = await _repo.GetTicketsAsync(desde, hasta, null, null, null, null, null);

            var sinResolver = todos
                .Where(t => !t.FechaCierre.HasValue)
                .OrderBy(t => t.FechaCreacion)
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Sin Resolver");

            string[] headers = { "#", "Código", "Título", "Prioridad", "Estado",
                                  "Técnico", "Fecha Creación", "Días Abierto" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
            EstilizarEncabezado(ws, headers.Length);

            // Resaltar filas con más de 7 días
            int row = 2, num = 1;
            foreach (var t in sinResolver)
            {
                int dias = DiasAbierto(t);
                ws.Cell(row, 1).Value = num++;
                ws.Cell(row, 2).Value = $"#{t.Id}";
                ws.Cell(row, 3).Value = t.Titulo;
                ws.Cell(row, 4).Value = t.Prioridad?.Nombre ?? "Sin prioridad";
                ws.Cell(row, 5).Value = t.Estado.Nombre;
                ws.Cell(row, 6).Value = t.AsignadoA?.NombreCompleto ?? "Sin asignar";
                ws.Cell(row, 7).Value = F(t.FechaCreacion);
                ws.Cell(row, 8).Value = dias;

                if (dias > 7)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");
                if (dias > 30)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8d7da");

                row++;
            }

            FinalizarHoja(ws);
            return ToBytes(wb);
        }

        // ── 5. Productividad por Técnico ──────────────────────────────────────

        public async Task<byte[]> GenerarReporteProductividadAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var tickets = await _repo.GetTicketsAsync(desde, hasta, null, null, null, null, null);

            var filas = tickets
                .Where(t => t.AsignadoA != null)
                .GroupBy(t => t.AsignadoA!.Id)
                .Select(g =>
                {
                    var resueltos = g.Where(t => t.FechaCierre.HasValue).ToList();
                    double promedio = resueltos.Count > 0
                        ? resueltos.Average(t => (t.FechaCierre!.Value - t.FechaCreacion).TotalDays)
                        : 0;
                    return new
                    {
                        Tecnico          = g.First().AsignadoA!.NombreCompleto,
                        Total            = g.Count(),
                        Resueltos        = resueltos.Count,
                        Pendientes       = g.Count() - resueltos.Count,
                        TiempoPromDias   = Math.Round(promedio, 1)
                    };
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Productividad");

            string[] headers = { "Técnico", "Asignados", "Resueltos",
                                  "Pendientes", "Tiempo Prom. (días)" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
            EstilizarEncabezado(ws, headers.Length);

            int row = 2;
            foreach (var f in filas)
            {
                ws.Cell(row, 1).Value = f.Tecnico;
                ws.Cell(row, 2).Value = f.Total;
                ws.Cell(row, 3).Value = f.Resueltos;
                ws.Cell(row, 4).Value = f.Pendientes;
                ws.Cell(row, 5).Value = f.TiempoPromDias;
                row++;
            }

            FinalizarHoja(ws);
            return ToBytes(wb);
        }

        // ── 6. Historial ──────────────────────────────────────────────────────

        public async Task<byte[]> GenerarReporteHistorialAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var items = await _repo.GetHistorialAsync(desde, hasta, filtro.TicketId);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Historial");

            string[] headers = { "#", "Ticket", "Título", "Usuario",
                                  "Acción", "Valor Anterior", "Valor Nuevo", "Fecha" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
            EstilizarEncabezado(ws, headers.Length);

            int row = 2, num = 1;
            foreach (var h in items)
            {
                ws.Cell(row, 1).Value = num++;
                ws.Cell(row, 2).Value = $"#{h.Ticket.Id}";
                ws.Cell(row, 3).Value = h.Ticket.Titulo;
                ws.Cell(row, 4).Value = h.Usuario.NombreCompleto;
                ws.Cell(row, 5).Value = h.Accion;
                ws.Cell(row, 6).Value = h.ValorAnterior ?? "—";
                ws.Cell(row, 7).Value = h.ValorNuevo    ?? "—";
                ws.Cell(row, 8).Value = F(h.FechaAccion);
                row++;
            }

            FinalizarHoja(ws);
            return ToBytes(wb);
        }

        // ── 7. Comentarios ────────────────────────────────────────────────────

        public async Task<byte[]> GenerarReporteComentariosAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var items = await _repo.GetComentariosAsync(desde, hasta);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Comentarios");

            string[] headers = { "#", "Ticket", "Título", "Usuario",
                                  "Tipo", "Comentario", "Fecha" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
            EstilizarEncabezado(ws, headers.Length);

            int row = 2, num = 1;
            foreach (var c in items)
            {
                ws.Cell(row, 1).Value = num++;
                ws.Cell(row, 2).Value = $"#{c.Ticket.Id}";
                ws.Cell(row, 3).Value = c.Ticket.Titulo;
                ws.Cell(row, 4).Value = c.Usuario.NombreCompleto;
                ws.Cell(row, 5).Value = c.EsInterno ? "Nota interna" : "Público";
                ws.Cell(row, 6).Value = c.Contenido;
                ws.Cell(row, 7).Value = F(c.FechaCreacion);

                if (c.EsInterno)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");

                row++;
            }

            FinalizarHoja(ws);
            return ToBytes(wb);
        }

        // ── 8. Adjuntos ───────────────────────────────────────────────────────

        public async Task<byte[]> GenerarReporteAdjuntosAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var items = await _repo.GetAdjuntosAsync(desde, hasta);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Adjuntos");

            string[] headers = { "#", "Ticket", "Título", "Nombre Archivo",
                                  "Tipo", "Tamaño (KB)", "Subido Por", "Fecha" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
            EstilizarEncabezado(ws, headers.Length);

            int row = 2, num = 1;
            foreach (var a in items)
            {
                ws.Cell(row, 1).Value = num++;
                ws.Cell(row, 2).Value = $"#{a.Ticket.Id}";
                ws.Cell(row, 3).Value = a.Ticket.Titulo;
                ws.Cell(row, 4).Value = a.NombreArchivo;
                ws.Cell(row, 5).Value = a.TipoContenido ?? "—";
                ws.Cell(row, 6).Value = Math.Round(a.TamanioBytes / 1024.0, 1);
                ws.Cell(row, 7).Value = a.SubidoPor.NombreCompleto;
                ws.Cell(row, 8).Value = F(a.FechaSubida);
                row++;
            }

            FinalizarHoja(ws);
            return ToBytes(wb);
        }

        // ── 9. Usuarios ───────────────────────────────────────────────────────

        public async Task<byte[]> GenerarReporteUsuariosAsync()
        {
            var usuarios = await _repo.GetTodosUsuariosAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Usuarios");

            string[] headers = { "#", "Nombre", "Apellido", "Email",
                                  "Teléfono", "Rol", "Activo", "Fecha Creación" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
            EstilizarEncabezado(ws, headers.Length);

            int row = 2, num = 1;
            foreach (var u in usuarios)
            {
                ws.Cell(row, 1).Value = num++;
                ws.Cell(row, 2).Value = u.Nombre;
                ws.Cell(row, 3).Value = u.Apellido;
                ws.Cell(row, 4).Value = u.Email;
                ws.Cell(row, 5).Value = u.NumeroTelefono ?? "—";
                ws.Cell(row, 6).Value = u.Rol.ToString();
                ws.Cell(row, 7).Value = u.Activo ? "Sí" : "No";
                ws.Cell(row, 8).Value = FD(u.CreadoEn);
                row++;
            }

            FinalizarHoja(ws);
            return ToBytes(wb);
        }

        // ════════════════════════════════════════════════════════════════════
        // PDF — helpers + dispatcher + 9 métodos privados
        // ════════════════════════════════════════════════════════════════════

        private static string FiltroInfo(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            return $"Período: {desde:dd/MM/yyyy} — {hasta:dd/MM/yyyy}";
        }

        private static byte[] BuildPdf(
            string titulo,
            string filtroInfo,
            string[] headers,
            IList<string[]> rows,
            float[]? colWidths = null,
            IDictionary<int, string>? rowColors = null)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(29.7f, 21.0f, Unit.Centimetre); // A4 Landscape
                    page.MarginHorizontal(1.5f, Unit.Centimetre);
                    page.MarginVertical(1.2f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem()
                                .Text(t => t.Span(titulo).Bold().FontSize(13).FontColor("#198754"));
                            row.ConstantItem(160)
                                .AlignRight()
                                .Text(t => t.Span($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(7).FontColor(Colors.Grey.Medium));
                        });

                        if (!string.IsNullOrEmpty(filtroInfo))
                            col.Item().PaddingTop(3)
                                .Text(t => t.Span(filtroInfo).FontSize(7.5f).FontColor(Colors.Grey.Darken2));

                        col.Item().PaddingTop(5).Height(2).Background("#198754");
                    });

                    page.Content().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            if (colWidths is { Length: > 0 })
                            {
                                foreach (var w in colWidths)
                                    cols.RelativeColumn(w);
                            }
                            else
                            {
                                for (int i = 0; i < headers.Length; i++)
                                    cols.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (var h in headers)
                            {
                                header.Cell()
                                    .Background("#198754")
                                    .Padding(4)
                                    .AlignCenter()
                                    .Text(t => t.Span(h).Bold().FontColor(Colors.White).FontSize(8));
                            }
                        });

                        if (rows.Count == 0)
                        {
                            table.Cell()
                                .ColumnSpan((uint)headers.Length)
                                .Padding(10)
                                .AlignCenter()
                                .Text(t => t.Span("Sin datos para el período seleccionado.")
                                    .Italic().FontColor(Colors.Grey.Medium));
                        }
                        else
                        {
                            for (int i = 0; i < rows.Count; i++)
                            {
                                string defaultBg = i % 2 == 0 ? Colors.White : "#F8F9FA";
                                string bg = rowColors != null && rowColors.TryGetValue(i, out var cb) ? cb : defaultBg;

                                foreach (var cell in rows[i])
                                {
                                    table.Cell()
                                        .Background(bg)
                                        .Padding(3)
                                        .Text(cell ?? "—");
                                }
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("SistemaTickets  ·  Página ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                        x.Span(" de ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf();
        }

        // ── PDF dispatcher ────────────────────────────────────────────────

        public async Task<byte[]> GenerarPdfAsync(string tipo, ReporteFiltro filtro) => tipo switch
        {
            "general"        => await Pdf_GeneralAsync(filtro),
            "estado"         => await Pdf_EstadoAsync(filtro),
            "prioridad"      => await Pdf_PrioridadAsync(filtro),
            "sinresolver"    => await Pdf_SinResolverAsync(filtro),
            "productividad"  => await Pdf_ProductividadAsync(filtro),
            "historial"      => await Pdf_HistorialAsync(filtro),
            "comentarios"    => await Pdf_ComentariosAsync(filtro),
            "adjuntos"       => await Pdf_AdjuntosAsync(filtro),
            "usuarios"       => await Pdf_UsuariosAsync(),
            _                => throw new ArgumentException($"Tipo PDF no válido: {tipo}")
        };

        // ── PDF 1: General ────────────────────────────────────────────────

        private async Task<byte[]> Pdf_GeneralAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var tickets = await _repo.GetTicketsAsync(desde, hasta,
                filtro.EstadoId, filtro.PrioridadId, filtro.CategoriaId,
                filtro.ClienteId, filtro.TecnicoId);

            string[] headers = { "#", "Código", "Título", "Categoría", "Prioridad",
                                  "Estado", "Solicitante", "Técnico", "F. Creación",
                                  "F. Cierre", "Días" };
            float[] widths = { 0.5f, 0.7f, 2.8f, 1.5f, 1.2f, 1.2f, 1.5f, 1.5f, 1.4f, 1.4f, 0.8f };

            var rows = tickets.Select((t, i) => new string[]
            {
                (i + 1).ToString(),
                $"#{t.Id}",
                t.Titulo,
                t.Categoria?.Nombre ?? "Sin categoría",
                t.Prioridad?.Nombre ?? "Sin prioridad",
                t.Estado.Nombre,
                t.CreadoPor.NombreCompleto,
                t.AsignadoA?.NombreCompleto ?? "Sin asignar",
                F(t.FechaCreacion),
                F(t.FechaCierre),
                DiasAbierto(t).ToString()
            }).ToList<string[]>();

            return BuildPdf("Reporte General de Tickets", FiltroInfo(filtro), headers, rows, widths);
        }

        // ── PDF 2: Por Estado ─────────────────────────────────────────────

        private async Task<byte[]> Pdf_EstadoAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var tickets = await _repo.GetTicketsAsync(desde, hasta, null, null, null, null, null);

            var grupos = tickets
                .GroupBy(t => t.Estado.Nombre)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .ToList();
            int total = grupos.Sum(x => x.Cantidad);

            string[] headers = { "Estado", "Cantidad", "Porcentaje" };

            var rows = grupos.Select(g => new string[]
            {
                g.Estado,
                g.Cantidad.ToString(),
                total > 0 ? $"{(g.Cantidad * 100.0 / total):N1}%" : "0%"
            }).ToList<string[]>();
            rows.Add(new[] { "TOTAL", total.ToString(), "100%" });

            return BuildPdf("Tickets por Estado", FiltroInfo(filtro), headers, rows);
        }

        // ── PDF 3: Por Prioridad ──────────────────────────────────────────

        private async Task<byte[]> Pdf_PrioridadAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var tickets = await _repo.GetTicketsAsync(desde, hasta,
                filtro.EstadoId, null, null, null, null);

            var grupos = tickets
                .GroupBy(t => t.Prioridad?.Nombre ?? "Sin prioridad")
                .Select(g => new
                {
                    Prioridad = g.Key,
                    Total     = g.Count(),
                    Abiertos  = g.Count(t => !t.FechaCierre.HasValue),
                    Cerrados  = g.Count(t =>  t.FechaCierre.HasValue)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            string[] headers = { "Prioridad", "Total", "Abiertos", "Cerrados/Resueltos" };

            var rows = grupos.Select(g => new string[]
            {
                g.Prioridad,
                g.Total.ToString(),
                g.Abiertos.ToString(),
                g.Cerrados.ToString()
            }).ToList<string[]>();

            return BuildPdf("Tickets por Prioridad", FiltroInfo(filtro), headers, rows);
        }

        // ── PDF 4: Sin Resolver ───────────────────────────────────────────

        private async Task<byte[]> Pdf_SinResolverAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var todos = await _repo.GetTicketsAsync(desde, hasta, null, null, null, null, null);

            var sinResolver = todos
                .Where(t => !t.FechaCierre.HasValue)
                .OrderBy(t => t.FechaCreacion)
                .ToList();

            string[] headers = { "#", "Código", "Título", "Prioridad", "Estado",
                                  "Técnico", "F. Creación", "Días" };
            float[] widths = { 0.5f, 0.7f, 3.0f, 1.2f, 1.2f, 1.8f, 1.4f, 0.8f };

            var rowColors = new Dictionary<int, string>();
            var rows      = new List<string[]>();

            for (int i = 0; i < sinResolver.Count; i++)
            {
                var t    = sinResolver[i];
                int dias = DiasAbierto(t);
                rows.Add(new[]
                {
                    (i + 1).ToString(),
                    $"#{t.Id}",
                    t.Titulo,
                    t.Prioridad?.Nombre ?? "Sin prioridad",
                    t.Estado.Nombre,
                    t.AsignadoA?.NombreCompleto ?? "Sin asignar",
                    F(t.FechaCreacion),
                    dias.ToString()
                });
                if (dias > 30) rowColors[i] = "#F8D7DA";
                else if (dias > 7) rowColors[i] = "#FFF3CD";
            }

            return BuildPdf("Tickets sin Resolver", FiltroInfo(filtro), headers, rows, widths, rowColors);
        }

        // ── PDF 5: Productividad ──────────────────────────────────────────

        private async Task<byte[]> Pdf_ProductividadAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var tickets = await _repo.GetTicketsAsync(desde, hasta, null, null, null, null, null);

            var filas = tickets
                .Where(t => t.AsignadoA != null)
                .GroupBy(t => t.AsignadoA!.Id)
                .Select(g =>
                {
                    var resueltos = g.Where(t => t.FechaCierre.HasValue).ToList();
                    double promedio = resueltos.Count > 0
                        ? resueltos.Average(t => (t.FechaCierre!.Value - t.FechaCreacion).TotalDays)
                        : 0;
                    return new
                    {
                        Tecnico    = g.First().AsignadoA!.NombreCompleto,
                        Total      = g.Count(),
                        Resueltos  = resueltos.Count,
                        Pendientes = g.Count() - resueltos.Count,
                        TiempoProm = Math.Round(promedio, 1)
                    };
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            string[] headers = { "Técnico", "Asignados", "Resueltos", "Pendientes", "Tiempo Prom. (días)" };

            var rows = filas.Select(f => new string[]
            {
                f.Tecnico,
                f.Total.ToString(),
                f.Resueltos.ToString(),
                f.Pendientes.ToString(),
                f.TiempoProm.ToString("N1")
            }).ToList<string[]>();

            return BuildPdf("Productividad por Técnico", FiltroInfo(filtro), headers, rows);
        }

        // ── PDF 6: Historial ──────────────────────────────────────────────

        private async Task<byte[]> Pdf_HistorialAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var items = await _repo.GetHistorialAsync(desde, hasta, filtro.TicketId);

            string[] headers = { "#", "Ticket", "Título", "Usuario",
                                  "Acción", "Valor Anterior", "Valor Nuevo", "Fecha" };
            float[] widths = { 0.4f, 0.6f, 2.2f, 1.5f, 2.0f, 1.8f, 1.8f, 1.3f };

            var rows = items.Select((h, i) => new string[]
            {
                (i + 1).ToString(),
                $"#{h.Ticket.Id}",
                h.Ticket.Titulo,
                h.Usuario.NombreCompleto,
                h.Accion,
                h.ValorAnterior ?? "—",
                h.ValorNuevo    ?? "—",
                F(h.FechaAccion)
            }).ToList<string[]>();

            return BuildPdf("Historial de Tickets", FiltroInfo(filtro), headers, rows, widths);
        }

        // ── PDF 7: Comentarios ────────────────────────────────────────────

        private async Task<byte[]> Pdf_ComentariosAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var items = await _repo.GetComentariosAsync(desde, hasta);

            string[] headers = { "#", "Ticket", "Título", "Usuario", "Tipo", "Comentario", "Fecha" };
            float[] widths = { 0.4f, 0.6f, 2.0f, 1.5f, 1.0f, 4.5f, 1.3f };

            var rowColors = new Dictionary<int, string>();
            var rows      = new List<string[]>();

            for (int i = 0; i < items.Count; i++)
            {
                var c = items[i];
                rows.Add(new[]
                {
                    (i + 1).ToString(),
                    $"#{c.Ticket.Id}",
                    c.Ticket.Titulo,
                    c.Usuario.NombreCompleto,
                    c.EsInterno ? "Nota interna" : "Público",
                    c.Contenido,
                    F(c.FechaCreacion)
                });
                if (c.EsInterno) rowColors[i] = "#FFF3CD";
            }

            return BuildPdf("Comentarios y Seguimiento", FiltroInfo(filtro), headers, rows, widths, rowColors);
        }

        // ── PDF 8: Adjuntos ───────────────────────────────────────────────

        private async Task<byte[]> Pdf_AdjuntosAsync(ReporteFiltro filtro)
        {
            var (desde, hasta) = filtro.ResolverFechas();
            var items = await _repo.GetAdjuntosAsync(desde, hasta);

            string[] headers = { "#", "Ticket", "Título", "Nombre Archivo",
                                  "Tipo", "Tamaño (KB)", "Subido Por", "Fecha" };
            float[] widths = { 0.4f, 0.6f, 2.5f, 2.5f, 1.2f, 0.9f, 1.5f, 1.3f };

            var rows = items.Select((a, i) => new string[]
            {
                (i + 1).ToString(),
                $"#{a.Ticket.Id}",
                a.Ticket.Titulo,
                a.NombreArchivo,
                a.TipoContenido ?? "—",
                Math.Round(a.TamanioBytes / 1024.0, 1).ToString("N1"),
                a.SubidoPor.NombreCompleto,
                F(a.FechaSubida)
            }).ToList<string[]>();

            return BuildPdf("Evidencias Adjuntas", FiltroInfo(filtro), headers, rows, widths);
        }

        // ── PDF 9: Usuarios ───────────────────────────────────────────────

        private async Task<byte[]> Pdf_UsuariosAsync()
        {
            var usuarios = await _repo.GetTodosUsuariosAsync();

            string[] headers = { "#", "Nombre", "Apellido", "Email",
                                  "Teléfono", "Rol", "Activo", "F. Creación" };
            float[] widths = { 0.4f, 1.5f, 1.5f, 2.8f, 1.5f, 1.2f, 0.7f, 1.2f };

            var rows = usuarios.Select((u, i) => new string[]
            {
                (i + 1).ToString(),
                u.Nombre,
                u.Apellido,
                u.Email,
                u.NumeroTelefono ?? "—",
                u.Rol.ToString(),
                u.Activo ? "Sí" : "No",
                FD(u.CreadoEn)
            }).ToList<string[]>();

            return BuildPdf("Usuarios del Sistema", "Todos los usuarios registrados", headers, rows, widths);
        }
    }
}
