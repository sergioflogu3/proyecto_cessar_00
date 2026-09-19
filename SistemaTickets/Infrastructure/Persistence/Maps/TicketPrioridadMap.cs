using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class TicketPrioridadMap : ClassMap<TicketPrioridad>
    {
        public TicketPrioridadMap()
        {
            Table("TicketPrioridades");
            Id(x => x.Id).GeneratedBy.Identity();
            Map(x => x.Nombre).Column("Nombre").Length(50).Not.Nullable();
            Map(x => x.Descripcion).Column("Descripcion").Length(200).Nullable();
            Map(x => x.Color).Column("Color").Length(20).Not.Nullable();
            Map(x => x.Orden).Column("Orden").Not.Nullable();
            Map(x => x.Activo).Column("Activo").Not.Nullable();
        }
    }
}
