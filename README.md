# Slide Show Creator

[Gallery](https://tgonzalez.net)

This repository is dedicated to documenting my journey to systematically discover fine art paintings.

## Next Steps By Priority
### Backup The Athenaeum Images to s3
### Index Data for National Galley of Art
### Make National Gallery of Art Available from the API's and the Gallery Website
### Data Sources

#### The Athenaeum

    {
        "artist": "dante gabriel rossetti",
        "date": "1869",
        "imageId": 736170,
        "name": "The Mandolin Player",
        "originalArtist": "Dante Gabriel Rossetti",
        "pageId": 33,
        "source": "http://www.the-athenaeum.org"
    }

#### National Gallery of Art

    {
        "pageId": 18392,
        "s3Path": "tgonzalez-image-archive/national-gallery-of-art/image-18392.jpg",
        "source": "http://images.nga.gov"
    }

#### Labels

    {
      "Labels": {
        "SS": [
          "Coffee Cup: 86.64999",
          "Cup: 86.64999",
          "Emblem: 57.37626",
          "Jug: 77.05539",
          "Pitcher: 77.05539",
          "Pottery: 74.03067",
          "Saucer: 74.03067"
        ]
      },
      "normalizedLabels": {
        "SS": [
          "coffee cup",
          "cup",
          "emblem",
          "jug",
          "pitcher",
          "pottery",
          "saucer"
        ]
      },
      "pageId": {
        "N": "26633"
      },
      "s3Path": {
        "S": "tgonzalez-image-archive/national-gallery-of-art/image-26633.jpg"
      },
      "source": {
        "S": "http://images.nga.gov"
      }
    }

### API's

Refer to `SlideshowCreator.DataAccessTests.ApiTests` for complete examples.

### Token

Tokens are created using a salted hash. The salt is a cryptographically secure random base 64 string. The token is created like below. With each UTC calendar day, new tokens are generated.

    TOKEN = HASH(HASH(DATE:BASE_64(RANDOM_BYTES)) + HASH(USERNAME:PASSWORD))

Url

    https://tgonzalez.net/api/Gallery/token?username=[USERNAME]&password=[PASSWORD]

Response

    "Q6AaIcvz25xiF2MgY/QDrBM4lDZ5BV1bKjV9wdbkPUE="

### Search by Exact Artist
    var artist = "Jean-Leon Gerome";
    var url = $"https://tgonzalez.net/api/Gallery/searchExactArtist?token={HttpUtility.UrlEncode(token)}&artist={artist}";
    var response = new WebClient().DownloadString(url);
    var results = JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
    Assert.AreEqual(233, results.Count);

### Search by Like Artist

    var artist = "Jean-Leon Gerome";
    var url = $"https://tgonzalez.net/api/Gallery/searchLikeArtist?token={HttpUtility.UrlEncode(token)}&artist={artist}";
    var response = new WebClient().DownloadString(url);
    var results = JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
    Assert.AreEqual(237, results.Count);

### Page by Id

    var url = $"https://tgonzalez.net/api/Gallery/scan?token={HttpUtility.UrlEncode(token)}&lastPageId=0";
    var response = new WebClient().DownloadString(url);
    var results = JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
    Assert.AreEqual(7350, results.Count);

## DNS

[DNS Registrar - AWS Route53](https://console.aws.amazon.com/route53/home#resource-record-sets:Z1ZQ15FI8WW9I9)

SOA ns-503.awsdns-62.com. awsdns-hostmaster.amazon.com. 1 7200 900 1209600 86400

NS  brad.ns.cloudflare.com

NS  jocelyn.ns.cloudflare.com


[DNS Controller - CloudFlare](https://www.cloudflare.com/a/dns/tgonzalez.net)

CNAME   tgonzalez.net   [Load Balancer Domain](https://console.aws.amazon.com/ec2/v2/home?region=us-east-1#LoadBalancers:)

MX      tgonzalez.net  [AWS Workmail](https://console.aws.amazon.com/workmail/home?region=us-east-1#/mail-domains/details/m-8088b0900e9e496c92b4a20deb5a28df/tgonzalez.net)

## Security

Complimentary security to username/password is underway. This second form of authentication/authorization isn't ordinary 2FA, because the second security mechanism will be through CloudFlare which operates at the network level defeating any application vulnerabilities, provided all requests are sent through CloudFlare. The latest changes ensure all requests follow the route `User -> CloudFlare -> Load Balancer -> Web Server`. The CloudFlare IP address is validated dynamically against CloudFlare's published IP range whitelist. IP's in headers can be trusted, because the application will block requests not from the load balancers VPC subnet. When route enforcement has been vetted, CloudFlare security can be configured to authenticate everything under any path e.g. /api/Gallery and instantly adding two very different forms of strong authentication (CloudFlare's passwordless uses email which requires 2FA, so I technically would need three passwords to gain access which is kind of just showing off).

[Web Server](https://default-environment.m5enr2iugv.us-east-1.elasticbeanstalk.com/) - IP doesn't resolve

[Load Balancer](https://awseb-e-v-AWSEBLoa-1RA0LU9VF65ZA-427719651.us-east-1.elb.amazonaws.com) - 403 forbidden from web application IP validation

[tgonzalez.net](tgonzalez.net) Home page is publicly accessible

### Issues

- Can't proxy traffic for www.tgonzalez.net through CloudFlare. An SSL error will occur, because there is no certificate for www.tgonzalez.net.

- Trying DNS only, will it hide the IP? If not this will not work for CloudFlare authentication.

## Helpful Projects

### Normal Random Numbers for Throttling
http://www.csharpcity.com/2010/normal-distribution-random-generator-available/

### Diacritic Squashing
https://github.com/thomasgalliker/Diacritics.NET
