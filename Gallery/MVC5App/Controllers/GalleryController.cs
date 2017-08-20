using System.Collections.Generic;
using System.Web.Http;

namespace MVC5App.Controllers
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/aspnet/web-api/overview/web-api-routing-and-actions/create-a-rest-api-with-attribute-routing
    /// </summary>
    [RoutePrefix("api/Gallery")]
    public class GalleryController : ApiController
    {
        [Route("")]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        [Route("{id:int}")]
        public string Get(int id)
        {
            return "value";
        }

        // Read-only. I want a highly hardened website. Data operations will be done from a non globally accessible resource.
        // Likely this will be done in the SlideShowCreator project, which has been doing the heavy lifting for the data classification.
    }
}
