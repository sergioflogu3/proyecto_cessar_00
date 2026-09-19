using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace SistemaTickets.Infrastructure.Persistence.Maps
{
    public class AsignacionHerramientaMap : ClassMap<AsignacionHerramienta>
    {
        public AsignacionHerramientaMap()
        {
            Table("AsignacionesHerramientas");
            Id(x => x.Id).GeneratedBy.Identity();
            References(x => x.Herramienta).Column("HerramientaId").Not.Nullable();
            References(x => x.Tecnico).Column("TecnicoId").Not.Nullable();
            References(x => x.AsignadoPor).Column("AsignadoPorId").Not.Nullable();
            Map(x => x.FechaAsignacion).Column("FechaAsignacion").Not.Nullable();
            Map(x => x.FechaDevolucion).Column("FechaDevolucion").Nullable();
            Map(x => x.Observaciones).Column("Observaciones").Length(500).Nullable();
            Map(x => x.Activa).Column("Activa").Not.Nullable();
        }
    }
}
