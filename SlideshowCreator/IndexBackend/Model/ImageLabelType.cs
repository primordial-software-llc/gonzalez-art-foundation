using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using Newtonsoft.Json;

namespace IndexBackend.Model
{
    public class ImageLabelType : IModel
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        public Dictionary<string, AttributeValue> GetKey()
        {
            throw new NotImplementedException();
        }

        public string GetTable()
        {
            return "ImageLabelType";
        }
    }
}
