using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class TicketComentarioMap : ClassMap<TicketComentario>
    {
        public TicketComentarioMap()
        {
            Table("TicketComentarios");
            Id(x => x.Id).GeneratedBy.Identity();
            References(x => x.Ticket).Column("TicketId").Not.Nullable();
            References(x => x.Usuario).Column("UsuarioId").Not.Nullable();
            Map(x => x.Contenido).Column("Contenido").Not.Nullable();
            Map(x => x.EsInterno).Column("EsInterno").Not.Nullable();
            Map(x => x.FechaCreacion).Column("FechaCreacion").Not.Nullable();
        }
    }
}
