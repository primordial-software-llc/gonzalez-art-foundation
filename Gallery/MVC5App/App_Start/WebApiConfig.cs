using System.Net.Http.Headers;
using System.Web.Http;

namespace MVC5App
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Formatters.JsonFormatter.SupportedMediaTypes
                .Add(new MediaTypeHeaderValue("text/html"));
            config.MapHttpAttributeRoutes();
        }
    }
}
