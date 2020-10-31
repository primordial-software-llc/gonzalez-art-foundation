﻿using System;
using System.IO;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.S3;
using Newtonsoft.Json.Linq;

namespace ArtApi.Routes.Unauthenticated.CacheEverything
{
    class GetImageBase64 : IRoute
    {
        public string HttpMethod => "GET";
        public string Path => "/unauthenticated/cache-everything/image-base-64";

        public void Run(APIGatewayProxyRequest request, APIGatewayProxyResponse response)
        {
            var path = request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey("path")
                ? request.QueryStringParameters["path"]
                : string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                response.Body = new JObject { { "error", "path is required" } }.ToString();
                response.StatusCode = 400;
                return;
            }
            else if (path.Contains("..") || !path.StartsWith("collections/", StringComparison.OrdinalIgnoreCase))
            {
                response.Body = new JObject { { "error", "path is invalid" } }.ToString();
                response.StatusCode = 400;
                return;
            }
            var s3 = new AmazonS3Client();
            var bucket = "gonzalez-art-foundation";
            var objectImage = s3.GetObjectAsync(bucket, $"{path}").Result;
            byte[] bytes;
            using (var stream = objectImage.ResponseStream)
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }
            var contentType = objectImage.Headers["Content-Type"] + ";base64";
            response.Headers["Content-Type"] = contentType;
            response.Body = $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }
    }
}
