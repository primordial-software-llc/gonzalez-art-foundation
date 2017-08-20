using System.Web.Http;

namespace WebApiStarter.Template
{
    /// <summary>
    /// Represents route configuration.
    /// </summary>
    public class RouteConfig
    {
        /// <summary>
        /// Configures Web API routes.
        /// </summary>
        /// <param name="configuration"></param>
        public static void Configure(HttpConfiguration configuration)
        {
            configuration.MapHttpAttributeRoutes();
        }
    }
}