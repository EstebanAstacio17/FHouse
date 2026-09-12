using System.Reflection;
using System.Web.Http;
using System.Web.Mvc;
using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using AutoMapper;
using FluentValidation;
using FHouse.Core.Interfaces.Repositories;
using FHouse.Infrastructure.Data;
using FHouse.Infrastructure.Repositories;
using FHouse.Infrastructure.UnitOfWork;
using FHouse.Services.Contracts;
using FHouse.Services.Implementations;
using FHouse.Services.Mapping;
using FHouse.Services.Validators;

namespace FHouse.Web.App_Start
{
    public static class AutofacConfig
    {
        public static void RegisterDependencies()
        {
            var builder = new ContainerBuilder();

            // Register MVC Controllers & Web API Controllers
            builder.RegisterControllers(typeof(AutofacConfig).Assembly);
            builder.RegisterApiControllers(typeof(AutofacConfig).Assembly);

            // Register DbContext
            builder.RegisterType<FHouseDbContext>().AsSelf().InstancePerRequest();

            // Register Unit of Work & Repositories
            builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerRequest();
            builder.RegisterGeneric(typeof(Repository<>)).As(typeof(IRepository<>)).InstancePerRequest();
            builder.RegisterType<TransaccionRepository>().As<ITransaccionRepository>().InstancePerRequest();
            builder.RegisterType<FuenteIngresoRepository>().As<IFuenteIngresoRepository>().InstancePerRequest();
            builder.RegisterType<CuentaRepository>().As<ICuentaRepository>().InstancePerRequest();
            builder.RegisterType<CategoriaRepository>().As<ICategoriaRepository>().InstancePerRequest();
            builder.RegisterType<PresupuestoRepository>().As<IPresupuestoRepository>().InstancePerRequest();
            builder.RegisterType<TasaCambioRepository>().As<ITasaCambioRepository>().InstancePerRequest();
            builder.RegisterType<AuditLogRepository>().As<IAuditLogRepository>().InstancePerRequest();

            // Register Services
            builder.RegisterType<TransaccionService>().As<ITransaccionService>().InstancePerRequest();
            builder.RegisterType<FuenteIngresoService>().As<IFuenteIngresoService>().InstancePerRequest();
            builder.RegisterType<CuentaService>().As<ICuentaService>().InstancePerRequest();
            builder.RegisterType<DashboardService>().As<IDashboardService>().InstancePerRequest();
            builder.RegisterType<CategoriaService>().As<ICategoriaService>().InstancePerRequest();
            builder.RegisterType<PresupuestoService>().As<IPresupuestoService>().InstancePerRequest();
            builder.RegisterType<TasaCambioService>().As<ITasaCambioService>().InstancePerRequest();
            builder.RegisterType<AuditService>().As<IAuditService>().InstancePerRequest();
            builder.RegisterType<UsuarioFamiliaService>().As<IUsuarioFamiliaService>().InstancePerRequest();
            builder.RegisterType<ReporteService>().As<IReporteService>().InstancePerRequest();
            builder.RegisterType<AuthService>().As<IAuthService>().InstancePerRequest();

            // Register FluentValidation Validators
            builder.RegisterAssemblyTypes(typeof(CrearTransaccionValidator).Assembly)
                   .AsClosedTypesOf(typeof(IValidator<>))
                   .InstancePerRequest();

            // Register AutoMapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<FHouseMappingProfile>();
            });
            builder.RegisterInstance(mapperConfig.CreateMapper()).As<IMapper>().SingleInstance();

            // Build container
            var container = builder.Build();

            // Set MVC and Web API Resolvers
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}
