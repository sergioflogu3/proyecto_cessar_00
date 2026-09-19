using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class VisitaTecnicaMap : ClassMap<VisitaTecnica>
    {
        public VisitaTecnicaMap()
        {
            Table("VisitasTecnicas");
            Id(x => x.Id).GeneratedBy.Identity();
            References(x => x.Ticket).Column("TicketId").Nullable();
            References(x => x.Tecnico).Column("TecnicoId").Not.Nullable();
            Map(x => x.FechaVisita).Column("FechaVisita").Not.Nullable();
            Map(x => x.Direccion).Column("Direccion").Length(300).Not.Nullable();
            Map(x => x.Descripcion).Column("Descripcion").Length(1000).Nullable();
            Map(x => x.Estado).Column("Estado").Length(50).Not.Nullable();
            Map(x => x.Observaciones).Column("Observaciones").Length(1000).Nullable();
            Map(x => x.FechaCreacion).Column("FechaCreacion").Not.Nullable();

            HasMany(x => x.Consumos)
                .KeyColumn("VisitaId")
                .Cascade.AllDeleteOrphan()
                .Inverse()
                .OrderBy("FechaConsumo DESC");
        }
    }
}
