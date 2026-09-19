using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class TicketCategoriaMap : ClassMap<TicketCategoria>
    {
        public TicketCategoriaMap()
        {
            Table("TicketCategorias");
            Id(x => x.Id).GeneratedBy.Identity();
            Map(x => x.Nombre).Column("Nombre").Length(100).Not.Nullable();
            Map(x => x.Descripcion).Column("Descripcion").Length(200).Nullable();
            Map(x => x.Activo).Column("Activo").Not.Nullable();
        }
    }
}
