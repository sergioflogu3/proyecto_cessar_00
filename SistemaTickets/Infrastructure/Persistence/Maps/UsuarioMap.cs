using FluentNHibernate.Mapping;
using SistemaTickets.Domain.Entities;

namespace MiNuevoProyecto.Infrastructure.Persistence.Maps
{
    public class UsuarioMap : ClassMap<Usuario>
    {
        public UsuarioMap()
        {
            Table("Usuarios");

            Id(x => x.Id)
                .Column("Id")
                .GeneratedBy.Identity();

            Map(x => x.Username)
                .Column("Username")
                .Length(50)
                .Not.Nullable();

            Map(x => x.PasswordHaseado)
                .Column("PasswordHaseado")
                .Length(256)
                .Not.Nullable();

            Map(x => x.Email)
                .Column("Email")
                .Length(120)
                .Not.Nullable();

            Map(x => x.NumeroTelefono)
                .Column("NumeroTelefono")
                .Length(30)
                .Nullable();

            Map(x => x.Nombre)
                .Column("Nombre")
                .Length(80)
                .Not.Nullable();

            Map(x => x.Apellido)
                .Column("Apellido")
                .Length(80)
                .Not.Nullable();

            Map(x => x.Activo)
                .Column("Activo")
                .Not.Nullable();

            Map(x => x.CreadoEn)
                .Column("CreadoEn")
                .Not.Nullable();

            Map(x => x.Rol)
                .Column("Rol")
                .CustomType<int>()
                .Not.Nullable();
        }
    }
}