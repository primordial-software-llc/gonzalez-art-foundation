using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MVC5App
{
    public class MvcApplication : HttpApplication
    {
        private static readonly object IP_VALIDATION_LOCK = new object();
        private static IPValidation ipValidation;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private bool IsAuthenticatedByToken()
        {
            string token = HttpUtility.UrlDecode(Request.Cookies.Get("token")?.Value);
            var dbClient = GalleryAwsCredentialsFactory.DbClient;
            var awsToolsClient = new DynamoDbClient<GalleryUser>(dbClient, new ConsoleLogging());
            var userClient = new GalleryUserAccess(dbClient, new ConsoleLogging(),awsToolsClient,
                new S3Logging("token-salt-cycling", GalleryAwsCredentialsFactory.S3Client));
            var auth = new Authentication(userClient);

            var masterUser = new GalleryUser {Id = Authentication.MASTER_USER_ID};
            masterUser =  awsToolsClient.Get(masterUser).Result;

            return auth.IsTokenValid(token, masterUser.Hash);
        }

        private string GetRouteAccessIssues()
        {
            var accessIssues = string.Empty;

            if (ApplicationContext.IsLocal(HttpContext.Current.Request))
            {
                return string.Empty;
            }

            if (!IPValidation.IsInSubnet(HttpContext.Current.Request.UserHostAddress, IPValidation.LOAD_BALANCER_VPC)) // Probably safe to assume IP in headers isn't spoofed without checking sender, because of VPC routing rules, but I'm not very comfortable with VPC's and like clearly documenting/enforcing the relation.
            {
                accessIssues += $"Client IP {HttpContext.Current.Request.UserHostAddress} is not a known VPC IP range {IPValidation.LOAD_BALANCER_VPC}";
            }

            List<string> forwardedIps = HttpContext.Current.Request.Headers["X-Forwarded-For"]
                .Replace(" ", string.Empty)
                .Split(',')
                .ToList();

            if (ipValidation == null) // Method level thread safe static init, because http context isn't set until after class init.
            {
                lock (IP_VALIDATION_LOCK)
                {
                    if (ipValidation == null)
                    {
                        ipValidation = new IPValidation(IPValidation.CLOUDFLARE_IP_WHITELIST, new S3Logging("cloudflare-ip-logs", GalleryAwsCredentialsFactory.S3Client));
                    }
                }
            }
            
            if (!ipValidation.IsInSubnet(forwardedIps.Last()))
            {
                accessIssues += Environment.NewLine + "Load balancer didn't receive request from CloudFlare." +
                              $" Load balancer forwarded IP's: {string.Join(", ", forwardedIps)} CloudFlare IP source list {IPValidation.CLOUDFLARE_IP_WHITELIST}." +
                              $"IP whitelist in memory: {string.Join(", ", ipValidation.IpWhitelist)}";
            }

            return accessIssues;
        }
        
        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
            JObject accessIssuesJson = new JObject();

            var routeIssues = GetRouteAccessIssues();
            if (!string.IsNullOrWhiteSpace(routeIssues))
            {
                accessIssuesJson.Add("route", routeIssues);
            }

            var publicEndpoints = new List<string>();
            publicEndpoints.Add("/api/gallery/token");
            publicEndpoints.Add("/api/gallery/twoFactorAuthenticationRedirect");
            publicEndpoints.Add("/api/twilio/rekognition/sms-pot");

            var isPublicEndpoint = publicEndpoints.Any(x => HttpContext.Current.Request.Url.LocalPath.Equals(x, StringComparison.OrdinalIgnoreCase));
            if (!isPublicEndpoint &&
                HttpContext.Current.Request.Url.LocalPath.StartsWith("/api/Gallery/", StringComparison.OrdinalIgnoreCase) &&
                !IsAuthenticatedByToken())
            {
                accessIssuesJson.Add("token", "Invalid");
                accessIssuesJson.Add("url", HttpContext.Current.Request.Url.AbsoluteUri);
            }

            string accessIssues = accessIssuesJson.Properties().Any()
                ? JsonConvert.SerializeObject(accessIssuesJson)
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(accessIssues))
            {
                accessIssues += Environment.NewLine + Environment.NewLine +
                              "Headers" + Environment.NewLine + Request.Headers;
                var s3Logging = new S3Logging("denied-ip-access-logs", GalleryAwsCredentialsFactory.S3Client);
                s3Logging.Log(accessIssues);
                HttpContext.Current.Response.StatusCode = 403;
                CompleteRequest(); // Early return
            }
            else
            {
                var content = $"Client IP: {HttpContext.Current.Request.UserHostAddress}.";

                if(HttpContext.Current.Request.Headers.AllKeys.Contains("X-Forwarded-For"))
                {
                    content += $" Forwarded IP's: {HttpContext.Current.Request.Headers["X-Forwarded-For"]}.";
                }

                if (HttpContext.Current.Request.Headers.AllKeys.Contains("CF-Connecting-IP"))
                {
                    content += $" CloudFlare Connecting IP: {HttpContext.Current.Request.Headers["CF-Connecting-IP"]}";
                }

                var s3Logging = new S3Logging("allowed-ip-access-logs", GalleryAwsCredentialsFactory.S3Client);
                s3Logging.Log(content);
            }
        }
        
    }
}
