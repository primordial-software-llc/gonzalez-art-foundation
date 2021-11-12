﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Amazon.SQS;
using ArtApi.Model;

namespace IndexBackend.Sources.Rijksmuseum
{
    public class Harvester
    {
        private IAmazonSQS SqsClient { get; }
        private string ApiKey { get; }
        public Harvester(IAmazonSQS sqsClient, string apiKey)
        {
            SqsClient = sqsClient;
            ApiKey = apiKey;
        }
        public async Task Harvest()
        {
            var client = new HttpClient();
            string resumptionToken = string.Empty;
            do
            {
                var harvestUrl = $"https://www.rijksmuseum.nl/api/oai/{ApiKey}?verb=ListRecords&set=subject:EntirePublicDomainSet&metadataPrefix=dc";
                if (!string.IsNullOrWhiteSpace(resumptionToken))
                {
                    harvestUrl += $"&resumptionToken={resumptionToken}";
                }
                var harvestXml = await GetHarvestXml(client, harvestUrl, 0, 5);
                harvestXml = harvestXml
                    .Replace(@"xmlns=""http://www.openarchives.org/OAI/2.0/""", "") // Simplify xmlns
                    .Replace("oai_dc:dc", "oai_dc_dc")
                    .Replace("dc:identifier", "dc_identifier");
                var doc = new XmlDocument();
                doc.LoadXml(harvestXml);
                var records = doc.DocumentElement.SelectNodes("//ListRecords/record/metadata/oai_dc_dc/dc_identifier[2]")
                    .Cast<XmlNode>()
                    .Select(x =>
                        new ClassificationModel
                        {
                            Source = RijksmuseumIndexer.Source,
                            PageId = x.InnerText
                        }
                    )
                    .ToList();
                var batches = Batcher.Batch(10, records);
                Parallel.ForEach(batches, new ParallelOptions { MaxDegreeOfParallelism = 10 }, batch =>
                {
                    IndexBackend.Harvester.SendBatch(SqsClient, batch);
                });
                resumptionToken = doc.DocumentElement.SelectSingleNode("//ListRecords/resumptionToken")?.InnerText;
            } while (!string.IsNullOrWhiteSpace(resumptionToken));
        }

        public async Task<string> GetHarvestXml(HttpClient client, string url, int attempt, int maxAttempts)
        {
            try
            {
                return await client.GetStringAsync(url);
            }
            catch (Exception e) when (e is HttpRequestException)
            {
                if (attempt < maxAttempts)
                {
                    return await GetHarvestXml(client, url, attempt + 1, maxAttempts);
                }
                throw;
            }
        }
    }
}