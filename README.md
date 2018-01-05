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

2FA is implemented requiring authentication with both CloudFlare Access and the websites own token endpoint.

1. [CloudFlare Access](https://community.cloudflare.com/t/cloudflare-access-beta-feedback/4092) Identity Provider
   1. [Passwordless](https://passwordless.net/)
   2. [Github Oauth](https://developer.github.com/apps/building-oauth-apps/)
2. Token from API e.g. `https://tgonzalez.net/api/Gallery/token?username=[USERNAME]&password=[PASSWORD]`

### Login Proccess

1. When the user clicks login and they don't have a CloudFlare cookie redirect to https://tgonzalez.net/api/Gallery/twoFactorAuthenticationRedirect?galleryPath
    1. A CloudFlare proxy may perform an auto-login if a CloudFlare login was recently performed and the cookie was removed, but if it's a clean browser session the CloudFlare login page will always be shown. Go here [https://tgonzalez.net/api](https://tgonzalez.net/api) in a clean browser session to see the CloudFlare login page.
    2. When the CloudFlare login is completed, CloudFlare will redirect back to the original url https://tgonzalez.net/api/Gallery/twoFactorAuthenticationRedirect?galleryPath. `galleryPath` holds the relative url, username and password at the time the login button was clicked.  *Right now gallery path is called with the home page as the only relative url not the actual relative url at the time the login button was clicked which it should be sending.*
2. If the usernamae/password url variables are present, the username/password is pre-populated and the login button is automatically clicked.
3. If the user already has a CloudFlare cookie when the login button is clicked, then the token API is called and the user should be fully authenticated when the request completes.

### Route Enforcement

CloudFlare Access is enforced by CloudFlare's proxy servers, so it's imperative that all traffic is routed through CloudFlare or 2FA will effectively be disabled and only a token would be required to access an API. The route is enforced in global.asax prior to every request. CloudFlare IP's are loaded/cached/refreshed (every 15 minutes) at runtime from CloudFlare's [published list](https://www.cloudflare.com/ips/).

    User -> CloudFlare -> Load Balancer VPC -> Web Server


[Web Server](https://default-environment.m5enr2iugv.us-east-1.elasticbeanstalk.com/) - IP doesn't resolve

[Load Balancer](https://awseb-e-v-AWSEBLoa-1RA0LU9VF65ZA-427719651.us-east-1.elb.amazonaws.com) - 403 forbidden from web application IP validation

[https://tgonzalez.net](https://tgonzalez.net) Home page is publicly accessible

[https://tgonzalez.net/api](https://tgonzalez.net/api) CloudFlare Access Secured Path

### Issues

#### Cant Use www.tgonzalez.net Domain Name

- Can't proxy traffic for www.tgonzalez.net through CloudFlare. An SSL error will occur, because there is no certificate for www.tgonzalez.net
- Can't change the certificate to include www.tgonzalez.net at the moment, because I migrated to an application load balancer and I get the following error when making environment changes even though the load balancer exists as an application load balancer (**there is no UI for application load balancers in Elastic Beanstalk, but I saw a press release stating it was planned so I'm holding out**). I can still make code changes without an issue. Error: `Service:AmazonElasticLoadBalancing, Message:There is no ACTIVE Load Balancer named 'awseb-e-v-AWSEBLoa-1RA0LU9VF65ZA'`
- Without traffic being proxied through CloudFlare, a 403 forbidden will always be returned, so I removed the cname for www.tgonzalez.net until I can apply the certificate.

## Helpful Projects

### Normal Random Numbers for Throttling
http://www.csharpcity.com/2010/normal-distribution-random-generator-available/

### Diacritic Squashing
https://github.com/thomasgalliker/Diacritics.NET
