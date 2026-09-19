using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class ActivoMap : ClassMap<Activo>
    {
        public ActivoMap()
        {
            Table("Activos");
            Id(x => x.Id).GeneratedBy.Identity();
            Map(x => x.Nombre).Column("Nombre").Length(150).Not.Nullable();
            Map(x => x.Descripcion).Column("Descripcion").Length(500).Nullable();
            Map(x => x.Tipo).Column("Tipo").Length(50).Not.Nullable();
            Map(x => x.NumeroSerie).Column("NumeroSerie").Length(100).Nullable();
            Map(x => x.Marca).Column("Marca").Length(100).Nullable();
            Map(x => x.Modelo).Column("Modelo").Length(100).Nullable();
            Map(x => x.Estado).Column("Estado").Length(50).Not.Nullable();
            Map(x => x.FechaAdquisicion).Column("FechaAdquisicion").Nullable();
            Map(x => x.FechaGarantia).Column("FechaGarantia").Nullable();
            Map(x => x.EsActivo).Column("Activo").Not.Nullable();
            Map(x => x.CreadoEn).Column("CreadoEn").Not.Nullable();

            References(x => x.Sede).Column("SedeId").Nullable();
            References(x => x.Area).Column("AreaId").Nullable();
            References(x => x.AsignadoA).Column("AsignadoAId").Nullable();

            HasMany(x => x.Movimientos)
                .KeyColumn("ActivoId")
                .Cascade.AllDeleteOrphan()
                .Inverse()
                .OrderBy("FechaMovimiento DESC");
        }
    }
}
