using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class InventarioMovimientoMap : ClassMap<InventarioMovimiento>
    {
        public InventarioMovimientoMap()
        {
            Table("InventarioMovimientos");
            Id(x => x.Id).GeneratedBy.Identity();
            References(x => x.Activo).Column("ActivoId").Nullable();
            References(x => x.Herramienta).Column("HerramientaId").Nullable();
            References(x => x.Usuario).Column("UsuarioId").Not.Nullable();
            Map(x => x.TipoMovimiento).Column("TipoMovimiento").Length(50).Not.Nullable();
            Map(x => x.FechaMovimiento).Column("FechaMovimiento").Not.Nullable();
            Map(x => x.Descripcion).Column("Descripcion").Length(500).Nullable();
            Map(x => x.ValorAnterior).Column("ValorAnterior").Length(200).Nullable();
            Map(x => x.ValorNuevo).Column("ValorNuevo").Length(200).Nullable();
        }
    }
}
