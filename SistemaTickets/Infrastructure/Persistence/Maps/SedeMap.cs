using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class SedeMap : ClassMap<Sede>
    {
        public SedeMap()
        {
            Table("Sedes");
            Id(x => x.Id).GeneratedBy.Identity();
            Map(x => x.Nombre).Column("Nombre").Length(100).Not.Nullable();
            Map(x => x.Ciudad).Column("Ciudad").Length(100).Not.Nullable();
            Map(x => x.Direccion).Column("Direccion").Length(200).Nullable();
            Map(x => x.Activa).Column("Activa").Not.Nullable();
        }
    }
}
