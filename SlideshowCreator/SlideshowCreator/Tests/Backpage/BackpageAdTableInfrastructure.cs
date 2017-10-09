using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using IndexBackend;
using IndexBackend.DataAccess;
using NUnit.Framework;
using SlideshowCreator.InfrastructureAsCode;

namespace SlideshowCreator.Tests.Backpage
{
    class BackpageAdTableInfrastructure
    {
        AmazonDynamoDBClient client = new AwsClientFactory().CreateDynamoDbClient();

        //[Test]
        public void Build_Backpage_Ad_Table()
        {
            var tableFactory = new DynamoDbTableFactoryBackpageAd(client);
            tableFactory.CreateTableWithIndexes(client, BackpageAdAccess.TABLE_NAME);
        }

        [Test]
        public void Insert_Then_Get()
        {
            var access = new BackpageAdAccess();
            var sample =
                access.CreateModel(
                    new Uri(
                        "http://youngstown.backpage.com/WomenSeekMen/thick-and-curvy-beautiful-brunnette-available-now-24/50609427"),
                    24);
            access.Insert(client, sample);

            Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>();
            key.Add("source", new AttributeValue{ S = BackpageAdAccess.SOURCE});
            key.Add("url", new AttributeValue { S = sample.Uri.AbsoluteUri });
            var refreshedConversion = client.GetItem(BackpageAdAccess.TABLE_NAME, key);
            var refreshed = access.ConvertToPoco(refreshedConversion.Item);

            Assert.AreEqual(BackpageAdAccess.SOURCE, refreshed.Source);
            Assert.AreEqual("http://youngstown.backpage.com/WomenSeekMen/thick-and-curvy-beautiful-brunnette-available-now-24/50609427", refreshed.Uri.AbsoluteUri);
            Assert.AreEqual(24, refreshed.Age);
            Assert.AreEqual(sample.Date, refreshed.Date);
        }

    }
}
