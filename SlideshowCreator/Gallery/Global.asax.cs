using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Amazon.S3.Model;
using GalleryBackend;
using IndexBackend;

namespace MVC5App
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
        
        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
            var dedniedMsg = string.Empty;

            if (!ApplicationContext.IsLocal(HttpContext.Current.Request) &&
                !IPValidation.IsInSubnet(HttpContext.Current.Request.UserHostAddress, IPValidation.LOAD_BALANCER_VPC))
            {
                dedniedMsg += $"Client IP {HttpContext.Current.Request.UserHostAddress} is not localhost or a known VPC IP range {IPValidation.LOAD_BALANCER_VPC}";
            }

            List<string> forwardedIps = HttpContext.Current.Request.Headers["X-Forwarded-For"]
                .Replace(" ", string.Empty)
                .Split(',')
                .ToList();
            var cfConnectingIp = HttpContext.Current.Request.Headers["CF-Connecting-IP"];
            if (!forwardedIps.First().Equals(cfConnectingIp))
            {
                dedniedMsg += Environment.NewLine + "Load balancer and CloudFlare have conflicting client origin IP's." +
                    $" Load balancer forwarded IP's: {string.Join(", ", forwardedIps)}. CloudFlare connecting IP {cfConnectingIp}";
            }
            
            var ipValidation = new IPValidation(IPValidation.CLOUDFLARE_IP_WHITELIST);
            if (!ipValidation.IsInSubnet(forwardedIps.Last()))
            {
                dedniedMsg += Environment.NewLine + "Load balancer didn't receive request from CloudFlare." +
                       $" Load balancer forwarded IP's: {string.Join(", ", forwardedIps)} CloudFlare IP source list {IPValidation.CLOUDFLARE_IP_WHITELIST}." +
                       $"IP whitelist in memory: {string.Join(", ", ipValidation.IpWhitelist)}";
            }

            if (!string.IsNullOrWhiteSpace(dedniedMsg))
            {
                dedniedMsg += Environment.NewLine + Environment.NewLine +
                    "Headers" + Environment.NewLine + Request.Headers;
                GalleryAwsCredentialsFactory.S3Client.PutObject(new PutObjectRequest
                {
                    BucketName = "tgonzalez-quick-logging",
                    Key = "denied-ip-access-logs/" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ"),
                    ContentBody = dedniedMsg
                });
                HttpContext.Current.Response.StatusCode = 403;
                CompleteRequest();
            }
            else
            {
                GalleryAwsCredentialsFactory.S3Client.PutObject(new PutObjectRequest
                {
                    BucketName = "tgonzalez-quick-logging",
                    Key = "allowed-ip-access-logs/" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ"),
                    ContentBody = $"Client IP: {HttpContext.Current.Request.UserHostAddress}." +
                                  $" Forwarded IP's: {HttpContext.Current.Request.Headers["X-Forwarded-For"]}." +
                                  $" CloudFlare Connecting IP: {cfConnectingIp}"
                });
            }
        }
        
    }
}
