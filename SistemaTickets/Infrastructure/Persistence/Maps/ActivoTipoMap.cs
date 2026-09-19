using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class ActivoTipoMap : ClassMap<ActivoTipo>
    {
        public ActivoTipoMap()
        {
            Table("ActivoTipos");
            Id(x => x.Id).GeneratedBy.Identity();
            Map(x => x.Nombre).Column("Nombre").Length(50).Not.Nullable();
            Map(x => x.Activo).Column("Activo").Not.Nullable();
        }
    }
}
