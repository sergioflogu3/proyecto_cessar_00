using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace MiNuevoProyecto.Infrastructure.Persistence.Maps
{
    public class AuditoriaUsuarioMap : ClassMap<AuditoriaUsuario>
    {
        public AuditoriaUsuarioMap()
        {
            Table("AuditoriaUsuarios");

            Id(x => x.Id)
                .Column("Id")
                .GeneratedBy.Identity();

            References(x => x.Usuario).Column("UsuarioId").Not.Nullable();
            References(x => x.ModificadoPor).Column("ModificadoPorId").Not.Nullable();

            Map(x => x.Campo)
                .Column("Campo")
                .Length(50)
                .Not.Nullable();

            Map(x => x.ValorAnterior)
                .Column("ValorAnterior")
                .Length(100)
                .Nullable();

            Map(x => x.ValorNuevo)
                .Column("ValorNuevo")
                .Length(100)
                .Nullable();

            Map(x => x.FechaCambio)
                .Column("FechaCambio")
                .Not.Nullable();
        }
    }
}
