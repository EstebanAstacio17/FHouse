using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using FHouse.Web.App_Start;

namespace FHouse.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Inyección de dependencias con Autofac
            AutofacConfig.RegisterDependencies();

            // Configurar AntiForgery para ClaimsIdentity
            System.Web.Helpers.AntiForgeryConfig.UniqueClaimTypeIdentifier = System.Security.Claims.ClaimTypes.NameIdentifier;

            // Sembrado inicial de base de datos asíncrono no bloqueante
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    using (var db = new FHouse.Infrastructure.Data.FHouseDbContext())
                    {
                        await FHouse.Infrastructure.Data.FHouseDbSeeder.SeedAsync(db);
                    }
                }
                catch { }
            });
        }
    }
}
