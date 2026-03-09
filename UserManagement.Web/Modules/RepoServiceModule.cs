using System.Reflection;
using Autofac;
using UserManagement.Core.Repositories;
using UserManagement.Core.Services;
using UserManagement.Core.UnitOfWorks;
using UserManagement.Repository;
using UserManagement.Repository.Repositories;
using UserManagement.Repository.UnitOfWorks;
using UserManagement.Service.Mapping;
using UserManagement.Service.Services;
using Module = Autofac.Module;

namespace UserManagement.Web.Modules
{
    public class RepoServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(GenericRepository<>)).As(typeof(IGenericRepository<>)).InstancePerLifetimeScope();
            builder.RegisterGeneric(typeof(Service<>)).As(typeof(IService<>)).InstancePerLifetimeScope();

            builder.RegisterType<UnitOfWork>().As<IUnitOfWork>();


            var apiAssembly = Assembly.GetExecutingAssembly();

            var repoAssembly = Assembly.GetAssembly(typeof(AppDbContext));
            // burda aslında class adı farketmez
            // buralarda hangi katmana bakacağız onu söylüyoruz class olarak o katmandan bir class okey bizim için
            var serviceAssembly = Assembly.GetAssembly(typeof(MapProfile));

            builder.RegisterAssemblyTypes(apiAssembly, repoAssembly, serviceAssembly).Where(x => x.Name.EndsWith("Repository")).AsImplementedInterfaces().InstancePerLifetimeScope();


            builder.RegisterAssemblyTypes(apiAssembly, repoAssembly, serviceAssembly).Where(x => x.Name.EndsWith("Service")).AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}
