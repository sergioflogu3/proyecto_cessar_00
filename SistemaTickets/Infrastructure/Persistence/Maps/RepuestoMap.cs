using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class RepuestoMap : ClassMap<Repuesto>
    {
        public RepuestoMap()
        {
            Table("Repuestos");
            Id(x => x.Id).GeneratedBy.Identity();
            Map(x => x.Nombre).Column("Nombre").Length(150).Not.Nullable();
            Map(x => x.Descripcion).Column("Descripcion").Length(500).Nullable();
            Map(x => x.Codigo).Column("Codigo").Length(50).Nullable();
            Map(x => x.Categoria).Column("Categoria").Length(50).Not.Nullable();
            Map(x => x.UnidadMedida).Column("UnidadMedida").Length(30).Not.Nullable();
            Map(x => x.PrecioUnitario).Column("PrecioUnitario").Not.Nullable();
            Map(x => x.Activo).Column("Activo").Not.Nullable();
            Map(x => x.CreadoEn).Column("CreadoEn").Not.Nullable();

            HasMany(x => x.Stocks)
                .KeyColumn("RepuestoId")
                .Cascade.AllDeleteOrphan()
                .Inverse();

            HasMany(x => x.Consumos)
                .KeyColumn("RepuestoId")
                .Cascade.AllDeleteOrphan()
                .Inverse()
                .OrderBy("FechaConsumo DESC");
        }
    }
}
