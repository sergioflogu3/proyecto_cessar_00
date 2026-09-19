using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class ConsumoRepuestoMap : ClassMap<ConsumoRepuesto>
    {
        public ConsumoRepuestoMap()
        {
            Table("ConsumoRepuestos");
            Id(x => x.Id).GeneratedBy.Identity();
            References(x => x.Repuesto).Column("RepuestoId").Not.Nullable();
            References(x => x.Ticket).Column("TicketId").Nullable();
            References(x => x.Visita).Column("VisitaId").Nullable();
            References(x => x.Tecnico).Column("TecnicoId").Not.Nullable();
            Map(x => x.Cantidad).Column("Cantidad").Not.Nullable();
            Map(x => x.FechaConsumo).Column("FechaConsumo").Not.Nullable();
            Map(x => x.Observaciones).Column("Observaciones").Length(500).Nullable();
        }
    }
}
