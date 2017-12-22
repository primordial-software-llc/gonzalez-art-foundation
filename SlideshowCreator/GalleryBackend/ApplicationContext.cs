using System.Web;

namespace GalleryBackend
{
    public class ApplicationContext
    {
        public static bool IsLocal(HttpRequest request)
        {
            return request.UserHostAddress == "::1" ||
                   request.UserHostAddress == "127.0.0.1";
        }
    }
}
