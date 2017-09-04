﻿using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace GalleryBackend.DataAccess
{
    public class ImageClassificationAccess
    {
        public const string IMAGE_CLASSIFICATION_V2 = "ImageClassificationV2";
        public const string ARTIST_NAME_INDEX = "ArtistNameIndex";
        public const string THE_ATHENAEUM = "http://www.the-athenaeum.org";


        private AmazonDynamoDBClient Client { get; }

        public ImageClassificationAccess(AmazonDynamoDBClient client)
        {
            Client = client;
        }

        public int FindAllForExactArtist( string artist)
        {
            artist = artist.ToLower();

            var queryRequest = new QueryRequest(IMAGE_CLASSIFICATION_V2)
            {
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":artist", new AttributeValue {S = artist}}
                },
                KeyConditionExpression = "artist = :artist",
                IndexName = ARTIST_NAME_INDEX
            };

            QueryResponse queryResponse = null;

            var allMatches = new List<Dictionary<string, AttributeValue>>();
            do
            {
                if (queryResponse != null)
                {
                    queryRequest.ExclusiveStartKey = queryResponse.LastEvaluatedKey;
                }
                queryResponse = Client.Query(queryRequest);

                if (queryResponse.Items.Any())
                {
                    allMatches.AddRange(queryResponse.Items);
                }
            } while (queryResponse.LastEvaluatedKey.Any());

            return allMatches.Count;
        }

        public int FindAllForLikeArtist(string artist)
        {
            artist = artist.ToLower();

            var scanRequest = new ScanRequest(IMAGE_CLASSIFICATION_V2)
            {
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":artist", new AttributeValue {S = artist}}
                },
                FilterExpression = "contains(artist, :artist)",
                IndexName = ARTIST_NAME_INDEX
            };

            ScanResponse scanResponse = null;

            var allMatches = new List<Dictionary<string, AttributeValue>>();
            do
            {
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
                scanResponse = Client.Scan(scanRequest);

                if (scanResponse.Items.Any())
                {
                    allMatches.AddRange(scanResponse.Items);
                }
            } while (scanResponse.LastEvaluatedKey.Any());

            return allMatches.Count;
        }
    }
}