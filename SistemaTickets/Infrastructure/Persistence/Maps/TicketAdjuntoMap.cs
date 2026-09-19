using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class TicketAdjuntoMap : ClassMap<TicketAdjunto>
    {
        public TicketAdjuntoMap()
        {
            Table("TicketAdjuntos");
            Id(x => x.Id).GeneratedBy.Identity();
            References(x => x.Ticket).Column("TicketId").Not.Nullable();
            References(x => x.SubidoPor).Column("SubidoPorId").Not.Nullable();
            Map(x => x.NombreArchivo).Column("NombreArchivo").Length(255).Not.Nullable();
            Map(x => x.RutaArchivo).Column("RutaArchivo").Length(500).Not.Nullable();
            Map(x => x.TipoContenido).Column("TipoContenido").Length(100).Nullable();
            Map(x => x.TamanioBytes).Column("TamanioBytes").Not.Nullable();
            Map(x => x.FechaSubida).Column("FechaSubida").Not.Nullable();
        }
    }
}
