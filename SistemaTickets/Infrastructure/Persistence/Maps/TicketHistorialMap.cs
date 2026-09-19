using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class TicketHistorialMap : ClassMap<TicketHistorial>
    {
        public TicketHistorialMap()
        {
            Table("TicketHistorial");
            Id(x => x.Id).GeneratedBy.Identity();
            References(x => x.Ticket).Column("TicketId").Not.Nullable();
            References(x => x.Usuario).Column("UsuarioId").Not.Nullable();
            Map(x => x.Accion).Column("Accion").Length(100).Not.Nullable();
            Map(x => x.ValorAnterior).Column("ValorAnterior").Length(200).Nullable();
            Map(x => x.ValorNuevo).Column("ValorNuevo").Length(200).Nullable();
            Map(x => x.FechaAccion).Column("FechaAccion").Not.Nullable();
        }
    }
}
