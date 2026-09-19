using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class StockRepuestoMap : ClassMap<StockRepuesto>
    {
        public StockRepuestoMap()
        {
            Table("StockRepuestos");
            Id(x => x.Id).GeneratedBy.Identity();
            References(x => x.Repuesto).Column("RepuestoId").Not.Nullable();
            References(x => x.Sede).Column("SedeId").Not.Nullable();
            Map(x => x.CantidadActual).Column("CantidadActual").Not.Nullable();
            Map(x => x.CantidadMinima).Column("CantidadMinima").Not.Nullable();
            Map(x => x.FechaActualizacion).Column("FechaActualizacion").Not.Nullable();
        }
    }
}
