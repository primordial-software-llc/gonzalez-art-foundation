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
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var clientDomain = "http://www.gonzalez-art-foundation.org";
            var response = new APIGatewayProxyResponse
            {
                Headers = new Dictionary<string, string>
                {
                    {"access-control-allow-origin", clientDomain}
                },
                StatusCode = 200
            };
            Console.WriteLine(request.Path);
            try
            {
                List<IRoute> routes = new List<IRoute>
                {
                    new Routes.Unauthenticated.GetImage()
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
