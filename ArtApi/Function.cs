using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ArtApi.Routes;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))] // Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
namespace ArtApi
{
    public class Function
    {
        private static readonly object IP_VALIDATION_LOCK = new object();
        private static CloudFlareIpValidation ipValidation;

        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var clientDomain = "https://www.gonzalez-art-foundation.org";
            var response = new APIGatewayProxyResponse
            {
                Headers = new Dictionary<string, string>
                {
                    {"access-control-allow-origin", clientDomain},
                    {"Access-Control-Allow-Credentials", "true" }
                },
                StatusCode = 200
            };
            if (ipValidation == null) // Method level thread safe static init, because http context isn't set until after class init.
            {
                lock (IP_VALIDATION_LOCK)
                {
                    if (ipValidation == null)
                    {
                        ipValidation = new CloudFlareIpValidation(CloudFlareIpValidation.CLOUDFLARE_IP_WHITELIST, new Logger());
                    }
                }
            }
            string accessIssues = string.Empty;
            var ip = request.RequestContext.Identity.SourceIp;
            if (!ipValidation.IsInSubnet(ip))
            {
                accessIssues += Environment.NewLine + "Didn't receive request from CloudFlare." +
                                $" Source IP: {ip}. CloudFlare IP source list {CloudFlareIpValidation.CLOUDFLARE_IP_WHITELIST}." +
                                $"IP whitelist in memory: {string.Join(", ", ipValidation.IpWhitelist)}";
            }
            if (!string.IsNullOrWhiteSpace(accessIssues))
            {
                response.Body = new JObject { { "error", accessIssues }}.ToString();
                response.StatusCode = 500;
                return response;
            }
            Console.WriteLine($"{request.HttpMethod} - {request.Path}");
            try
            {
                List<IRoute> routes = new List<IRoute>
                {
                    new Routes.Unauthenticated.GetSearch(),
                    new Routes.Unauthenticated.CacheEverything.GetImageClassification(),
                    new Routes.Unauthenticated.CacheEverything.GetImage(),
                    new Routes.Unauthenticated.CacheEverything.GetArtist()
                };
                var matchedRoute = routes.FirstOrDefault(route => string.Equals(request.HttpMethod, route.HttpMethod, StringComparison.OrdinalIgnoreCase) &&
                                                                  string.Equals(request.Path, route.Path, StringComparison.OrdinalIgnoreCase));
                if (matchedRoute != null)
                {
                    matchedRoute.Run(request, response);
                }
                else
                {
                    response.StatusCode = 404;
                    response.Body = new JObject().ToString();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                response.StatusCode = 500;
                response.Body = new JObject {{"error", exception.ToString()}}.ToString();
            }
            return response;
        }
    }
}
