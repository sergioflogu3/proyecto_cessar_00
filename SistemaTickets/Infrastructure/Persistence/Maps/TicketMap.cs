using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class TicketMap : ClassMap<Ticket>
    {
        public TicketMap()
        {
            Table("Tickets");
            Id(x => x.Id).GeneratedBy.Identity();

            Map(x => x.Titulo).Column("Titulo").Length(200).Not.Nullable();
            Map(x => x.Descripcion).Column("Descripcion").Not.Nullable();
            Map(x => x.FechaCreacion).Column("FechaCreacion").Not.Nullable();
            Map(x => x.FechaActualizacion).Column("FechaActualizacion").Not.Nullable();
            Map(x => x.FechaCierre).Column("FechaCierre").Nullable();
            Map(x => x.FechaVencimiento).Column("FechaVencimiento").Nullable();

            References(x => x.Estado).Column("EstadoId").Not.Nullable();
            References(x => x.Prioridad).Column("PrioridadId").Nullable();
            References(x => x.Categoria).Column("CategoriaId").Nullable();
            References(x => x.CreadoPor).Column("CreadoPorId").Not.Nullable();
            References(x => x.AsignadoA).Column("AsignadoAId").Nullable();

            HasMany(x => x.Comentarios)
                .KeyColumn("TicketId")
                .Cascade.AllDeleteOrphan()
                .Inverse()
                .OrderBy("FechaCreacion");

            HasMany(x => x.Adjuntos)
                .KeyColumn("TicketId")
                .Cascade.AllDeleteOrphan()
                .Inverse()
                .OrderBy("FechaSubida");

            HasMany(x => x.Historial)
                .KeyColumn("TicketId")
                .Cascade.AllDeleteOrphan()
                .Inverse()
                .OrderBy("FechaAccion");
        }
    }
}
