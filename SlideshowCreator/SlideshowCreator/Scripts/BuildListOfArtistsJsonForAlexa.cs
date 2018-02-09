using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using GalleryBackend.Model;
using IndexBackend;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace SlideshowCreator.Scripts
{
    class BuildListOfArtistsJsonForAlexa
    {
        [Test]
        public void Build_Json_For_Manual_Copy_Into_Static_Json()
        {
            var client = GalleryAwsCredentialsFactory.DbClient;
            var scanRequest = new ScanRequest(new ArtistModel().GetTable());
            ScanResponse scanResponse = null;
            var artists = new List<ArtistModel>();
			
            do
            {
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
                scanResponse = client.Scan(scanRequest);
                artists.AddRange(
                    Conversion<ArtistModel>.ConvertToPoco(scanResponse.Items));
            } while (scanResponse.LastEvaluatedKey.Any());
            var container = new JArray();
            foreach (var artist in artists.OrderBy(x => x.Artist))
            {
                var item = new JObject();
                var value = new JObject();
                value.Add("value", artist.Artist);
                item.Add("name", value);
                container.Add(item);
            }
            Console.WriteLine(container.ToString());
        }
    }
}
