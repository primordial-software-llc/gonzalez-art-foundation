using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.SQS;
using Amazon.SQS.Model;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.Indexing;
using IndexBackend.MetropolitanMuseumOfArt;
using IndexBackend.MinistereDeLaCulture;
using IndexBackend.MuseeOrsay;
using IndexBackend.MuseumOfModernArt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Console = System.Console;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ArtIndexer
{
    public class Function
    {
        private IAmazonSQS QueueClient { get; }
        private IAmazonDynamoDB DbClient { get; }
        private HttpClient HttpClient { get; }
        private IAmazonS3 S3Client { get; }
        private ElasticSearchClient ElasticSearchClient { get; }
        private const string QUEUE_URL = "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler";
        private const int SQS_MAX_BATCH = 10;

        public Function()
            : this(
                new AmazonSQSClient(new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.USEast1 }),
                new AmazonDynamoDBClient(new AmazonDynamoDBConfig { RegionEndpoint = RegionEndpoint.USEast1}),
                new AmazonS3Client(),
                new HttpClient(),
                new ElasticSearchClient(
                    new HttpClient(),
                    Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION"),
                    Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN"))
            )
        {

        }

        public Function(IAmazonSQS queueClient, IAmazonDynamoDB dbClient, IAmazonS3 s3Client, HttpClient httpClient,
            ElasticSearchClient elasticSearchClient)
        {
            QueueClient = queueClient;
            DbClient = dbClient;
            S3Client = s3Client;
            HttpClient = httpClient;
            ElasticSearchClient = elasticSearchClient;
        }

        public string FunctionHandler(ILambdaContext context)
        {
            ReceiveMessageResponse batch;
            do
            {
                batch = QueueClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    MaxNumberOfMessages = SQS_MAX_BATCH,
                    QueueUrl = QUEUE_URL
                }).Result;
                if (!batch.Messages.Any())
                {
                    break;
                }
                var tasks = new List<Task>();
                foreach (var message in batch.Messages)
                {
                    tasks.Add(IndexAndMarkComplete(message));
                }
                Task.WaitAll(tasks.ToArray());
            } while (batch.Messages.Any());
            return $"No additional SQS messages found in {QUEUE_URL}";
        }

        private async Task IndexAndMarkComplete(Message message)
        {
            var model = JsonConvert.DeserializeObject<ClassificationModel>(message.Body);
            var indexer = GetIndexer(model.Source);
            if (indexer == null)
            {
                Console.WriteLine($"Failed to process message due to unknown source {model.Source} for message: {message.Body}");
                return;
            }
            try
            {
                var classification = await indexer.Index(model.PageId);
                if (classification == null)
                {
                    Console.WriteLine($"Skipped {message.Body} due to not finding content.");
                }
                else
                {
                    classification.Name = HttpUtility.HtmlDecode(classification.Name);
                    classification.Date = HttpUtility.HtmlDecode(classification.Date);
                    classification.OriginalArtist = HttpUtility.HtmlDecode(classification.OriginalArtist);
                    classification.Artist = Classifier.NormalizeArtist(HttpUtility.HtmlDecode(classification.OriginalArtist));
                    classification.TimeStamp = DateTime.UtcNow.ToString("O");
                    var json = JObject.FromObject(classification, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                    await DbClient.PutItemAsync(
                        new ClassificationModel().GetTable(),
                        Document.FromJson(json.ToString()).ToAttributeMap()
                    );
                    await ElasticSearchClient.SendToElasticSearch(classification);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to process message due to error {e.Message} for message: {message.Body}. Error: " + e);
                return;
            }
            await QueueClient.DeleteMessageAsync(QUEUE_URL, message.ReceiptHandle);
        }

        private IIndex GetIndexer(string source)
        {
            if (string.Equals(source, MuseeOrsayIndexer.Source, StringComparison.OrdinalIgnoreCase))
            {
                return new MuseeOrsayIndexer(S3Client, HttpClient, new ConsoleLogging());
            }
            if (string.Equals(source, MinistereDeLaCultureIndexer.SourceMuseeDuLouvre, StringComparison.OrdinalIgnoreCase))
            {
                return new MinistereDeLaCultureIndexer(
                    S3Client,
                    HttpClient,
                    new ConsoleLogging(),
                    MinistereDeLaCultureIndexer.SourceMuseeDuLouvre,
                    MinistereDeLaCultureIndexer.S3PathLouvre);
            }
            if (string.Equals(source, MinistereDeLaCultureIndexer.SourceMinistereDeLaCulture, StringComparison.OrdinalIgnoreCase))
            {
                return new MinistereDeLaCultureIndexer(
                    S3Client,
                    HttpClient,
                    new ConsoleLogging(),
                    MinistereDeLaCultureIndexer.SourceMinistereDeLaCulture,
                    MinistereDeLaCultureIndexer.S3PathMinistereDeLaCulture);
            }
            if (string.Equals(source, MuseumOfModernArtIndexer.Source, StringComparison.OrdinalIgnoreCase))
            {
                return new MuseumOfModernArtIndexer(S3Client, HttpClient, new ConsoleLogging());
            }
            if (string.Equals(source, MetropolitanMuseumOfArtIndexer.Source, StringComparison.OrdinalIgnoreCase))
            {
                // Getting all types of issues. There is crawl protection on this site. Might need to use a headless browser, but I need to see how bad it is first.
                var client = new HttpClient(new HttpClientHandler { UseCookies = false });
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
                client.DefaultRequestHeaders.Add("Cookie", "cro_segment_referrer=https://www.google.com/; cro_segment_utm_source=none; cro_segment_utm_medium=none; cro_segment_utm_campaign=none; cro_segment_utm_term=none; cro_segment_utm_content=none; cro_segment_utm_source=none; cro_segment_utm_medium=none; cro_segment_utm_campaign=none; cro_segment_utm_term=none; cro_segment_utm_content=none; visid_incap_1661977=M4oyH59ZTKSSkyFSPkvdphLEsF8AAAAAQUIPAAAAAAC9tLNpk/QnK+d3TKYQ83Po; optimizelyEndUserId=oeu1606285167990r0.264774550334149; _gcl_au=1.1.839559341.1606285168; _ga=GA1.2.393842284.1606285168; __qca=P0-2109895867-1606285168517; _fbp=fb.1.1606285168963.1824677521; ki_s=; visid_incap_1662004=8EJkRP3WQNKlVTHHXvW3/80hw18AAAAAQUIPAAAAAADnP/0pYQEd/Re3Ey/5gS5V; ki_r=; _gid=GA1.2.922936791.1607842600; visid_incap_1661922=5DiNZXBqTlKcQ4BE0M03KiTvvV8AAAAAQkIPAAAAAACAmOWYAa2q/9+nf0qHCWKV4RLUkMhshU/T; ObjectPageSet=0.52134515881601; incap_ses_8079_1661977=kCmlU6pN9gMmCmG9tl8ecJwf2F8AAAAARYSG3TEBR02t1tKp/uM96A==; incap_ses_8079_1661922=pIRePO7GOxdk9HO9tl8ecOlL2F8AAAAAadP51qdvgJwSltYOgrDYPw==; _parsely_session={%22sid%22:24%2C%22surl%22:%22https://www.metmuseum.org/art/collection/search/75677%22%2C%22sref%22:%22%22%2C%22sts%22:1608010729453%2C%22slts%22:1608004125716}; _parsely_visitor={%22id%22:%22pid=9d6d01751afd92539c1812e48d9fd162%22%2C%22session_count%22:24%2C%22last_session_ts%22:1608010729453}; incap_ses_8079_1662004=RAP5N9biz0FJAnS9tl8ecOlL2F8AAAAACJUe63j2I7ZhRyn98DyfGg==; _dc_gtm_UA-72292701-1=1; ki_t=1606285169173%3B1607995013254%3B1608010729902%3B6%3B96");
                return new MetropolitanMuseumOfArtIndexer(S3Client, HttpClient, new ConsoleLogging());
            }
            return null;
        }

    }
}
