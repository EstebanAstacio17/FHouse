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

            // Non-blocking async DB seed — errors are logged, never silenced
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    using (var db = new FHouse.Infrastructure.Data.FHouseDbContext())
                    {
                        await FHouse.Infrastructure.Data.FHouseDbSeeder.SeedAsync(db);
                    }
                }
                catch (System.Exception ex)
                {
                    // RESILIENCE: Log seed errors without crashing the app startup
                    System.Diagnostics.Trace.TraceError($"[FHouse] DB Seed falló: {ex.Message}");
                    try
                    {
                        System.Diagnostics.EventLog.WriteEntry("Application",
                            $"FHouse DB Seed Error: {ex.Message}",
                            System.Diagnostics.EventLogEntryType.Warning);
                    }
                    catch { /* EventLog may not be available in all environments */ }
                }
            });
        }
    }
}
