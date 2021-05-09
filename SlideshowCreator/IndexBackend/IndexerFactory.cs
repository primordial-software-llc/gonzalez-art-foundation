using System;
using System.Net.Http;
using Amazon.DynamoDBv2;
using Amazon.S3;
using IndexBackend.Indexing;
using IndexBackend.Sources.Christies;
using IndexBackend.Sources.MetropolitanMuseumOfArt;
using IndexBackend.Sources.MinistereDeLaCulture;
using IndexBackend.Sources.MuseeOrsay;
using IndexBackend.Sources.MuseumOfModernArt;
using IndexBackend.Sources.NationalGalleryOfArt;
using IndexBackend.Sources.TheAthenaeum;

namespace IndexBackend
{
    public class IndexerFactory
    {
        public IIndex GetIndexer(string source, HttpClient httpClient, IAmazonS3 s3Client, IAmazonDynamoDB dynamoDbClient)
        {
            var log = new ConsoleLogging();
            if (string.Equals(source, TheAthenaeumIndexer.Source, StringComparison.OrdinalIgnoreCase))
            {
                return new TheAthenaeumIndexer(s3Client, dynamoDbClient);
            }
            if (string.Equals(source, ChristiesArtIndexer.Source, StringComparison.OrdinalIgnoreCase))
            {
                return new ChristiesArtIndexer(httpClient, log);
            }
            if (string.Equals(source, MuseeOrsayIndexer.Source, StringComparison.OrdinalIgnoreCase))
            {
                return new MuseeOrsayIndexer(httpClient, log);
            }
            if (string.Equals(source, MinistereDeLaCultureIndexer.SourceMuseeDuLouvre, StringComparison.OrdinalIgnoreCase))
            {
                return new MinistereDeLaCultureIndexer(
                    httpClient,
                    log,
                    MinistereDeLaCultureIndexer.SourceMuseeDuLouvre,
                    MinistereDeLaCultureIndexer.S3PathLouvre);
            }
            if (string.Equals(source, MinistereDeLaCultureIndexer.SourceMinistereDeLaCulture, StringComparison.OrdinalIgnoreCase))
            {
                return new MinistereDeLaCultureIndexer(
                    httpClient,
                    log,
                    MinistereDeLaCultureIndexer.SourceMinistereDeLaCulture,
                    MinistereDeLaCultureIndexer.S3PathMinistereDeLaCulture);
            }
            if (string.Equals(source, MuseumOfModernArtIndexer.Source, StringComparison.OrdinalIgnoreCase))
            {
                return new MuseumOfModernArtIndexer(httpClient, log);
            }
            if (string.Equals(source, MetropolitanMuseumOfArtIndexer.Source, StringComparison.OrdinalIgnoreCase))
            {
                // Getting all types of issues. There is crawl protection on this site.
                var client = new HttpClient(new HttpClientHandler { UseCookies = false });
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
                client.DefaultRequestHeaders.Add("Cookie", "cro_segment_referrer=https://www.google.com/; cro_segment_utm_source=none; cro_segment_utm_medium=none; cro_segment_utm_campaign=none; cro_segment_utm_term=none; cro_segment_utm_content=none; cro_segment_utm_source=none; cro_segment_utm_medium=none; cro_segment_utm_campaign=none; cro_segment_utm_term=none; cro_segment_utm_content=none; visid_incap_1661977=M4oyH59ZTKSSkyFSPkvdphLEsF8AAAAAQUIPAAAAAAC9tLNpk/QnK+d3TKYQ83Po; optimizelyEndUserId=oeu1606285167990r0.264774550334149; _gcl_au=1.1.839559341.1606285168; _ga=GA1.2.393842284.1606285168; __qca=P0-2109895867-1606285168517; _fbp=fb.1.1606285168963.1824677521; ki_s=; visid_incap_1662004=8EJkRP3WQNKlVTHHXvW3/80hw18AAAAAQUIPAAAAAADnP/0pYQEd/Re3Ey/5gS5V; ki_r=; _gid=GA1.2.922936791.1607842600; visid_incap_1661922=5DiNZXBqTlKcQ4BE0M03KiTvvV8AAAAAQkIPAAAAAACAmOWYAa2q/9+nf0qHCWKV4RLUkMhshU/T; ObjectPageSet=0.52134515881601; incap_ses_8079_1661977=kCmlU6pN9gMmCmG9tl8ecJwf2F8AAAAARYSG3TEBR02t1tKp/uM96A==; incap_ses_8079_1661922=pIRePO7GOxdk9HO9tl8ecOlL2F8AAAAAadP51qdvgJwSltYOgrDYPw==; _parsely_session={%22sid%22:24%2C%22surl%22:%22https://www.metmuseum.org/art/collection/search/75677%22%2C%22sref%22:%22%22%2C%22sts%22:1608010729453%2C%22slts%22:1608004125716}; _parsely_visitor={%22id%22:%22pid=9d6d01751afd92539c1812e48d9fd162%22%2C%22session_count%22:24%2C%22last_session_ts%22:1608010729453}; incap_ses_8079_1662004=RAP5N9biz0FJAnS9tl8ecOlL2F8AAAAACJUe63j2I7ZhRyn98DyfGg==; _dc_gtm_UA-72292701-1=1; ki_t=1606285169173%3B1607995013254%3B1608010729902%3B6%3B96");
                return new MetropolitanMuseumOfArtIndexer(httpClient, log);
            }
            if (string.Equals(source, NationalGalleryOfArtIndexer.Source, StringComparison.OrdinalIgnoreCase))
            {
                return new NationalGalleryOfArtIndexer(s3Client, dynamoDbClient, new NationalGalleryOfArtDataAccess());
            }
            return null;
        }
    }
}
