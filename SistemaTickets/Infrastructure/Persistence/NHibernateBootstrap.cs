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
        public static ISessionFactory BuildSessionFactory(string cs, bool updateSchema = false)
        {
            var db = MsSqlConfiguration.MsSql2012
                .ConnectionString(cs)
                .Driver<MicrosoftDataSqlClientDriver>()
                .Dialect<MsSql2012Dialect>()
                .ShowSql();

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