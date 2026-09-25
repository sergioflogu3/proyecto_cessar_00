using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using MiNuevoProyecto.Infrastructure.Persistence.Maps;
using NHibernate;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Tool.hbm2ddl;

namespace SistemaTickets.Infrastructure.Persistence
{
    public static class NHibernateBootstrap
    {
        // B4: ShowSql() escribe cada sentencia SQL (con los valores de cada parámetro
        // enlazado) directo a la consola — NHibernate no lo enruta por Microsoft.Extensions.Logging,
        // así que ningún LogLevel de appsettings.json lo filtra ni lo apaga. Eso incluye
        // título/descripción de tickets, username, y cualquier otro dato que viaje como
        // parámetro — exactamente el riesgo que describe el hallazgo. Solo tiene sentido
        // prendido para depurar localmente; showSql debe venir en false fuera de Development.
        public static ISessionFactory BuildSessionFactory(string cs, bool updateSchema = false, bool showSql = false)
        {
            var db = MsSqlConfiguration.MsSql2012
                .ConnectionString(cs)
                .Driver<MicrosoftDataSqlClientDriver>()
                .Dialect<MsSql2012Dialect>();

            if (showSql)
            {
                db = db.ShowSql();
            }

            return Fluently.Configure()
                .Database(db)
                .Mappings(m =>
                {
                    m.FluentMappings.AddFromAssembly(typeof(UsuarioMap).Assembly);
                })
                .ExposeConfiguration(cfg =>
                {
                    if (updateSchema)
                    {
                        new SchemaUpdate(cfg).Execute(false, true);
                    }
                })
                .BuildSessionFactory();
        }
    }
}