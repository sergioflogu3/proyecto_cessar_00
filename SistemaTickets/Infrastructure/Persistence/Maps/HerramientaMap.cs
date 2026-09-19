using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class HerramientaMap : ClassMap<Herramienta>
    {
        public HerramientaMap()
        {
            Table("Herramientas");
            Id(x => x.Id).GeneratedBy.Identity();
            Map(x => x.Nombre).Column("Nombre").Length(150).Not.Nullable();
            Map(x => x.Descripcion).Column("Descripcion").Length(500).Nullable();
            Map(x => x.Codigo).Column("Codigo").Length(50).Nullable();
            Map(x => x.Tipo).Column("Tipo").Length(50).Not.Nullable();
            Map(x => x.Estado).Column("Estado").Length(50).Not.Nullable();
            Map(x => x.Activa).Column("Activa").Not.Nullable();
            Map(x => x.CreadoEn).Column("CreadoEn").Not.Nullable();

            HasMany(x => x.Asignaciones)
                .KeyColumn("HerramientaId")
                .Cascade.AllDeleteOrphan()
                .Inverse()
                .OrderBy("FechaAsignacion DESC");

            HasMany(x => x.Movimientos)
                .KeyColumn("HerramientaId")
                .Cascade.AllDeleteOrphan()
                .Inverse()
                .OrderBy("FechaMovimiento DESC");
        }
    }
}
