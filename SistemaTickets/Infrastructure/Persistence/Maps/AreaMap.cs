using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class AreaMap : ClassMap<Area>
    {
        public AreaMap()
        {
            Table("Areas");
            Id(x => x.Id).GeneratedBy.Identity();
            Map(x => x.Nombre).Column("Nombre").Length(100).Not.Nullable();
            Map(x => x.Activa).Column("Activa").Not.Nullable();
            References(x => x.Sede).Column("SedeId").Not.Nullable();
        }
    }
}
