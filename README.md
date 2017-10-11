# Slide Show Creator


Alright so here is my grand plan for scaling indexing. While running I thought about a way to distribute the load in what I consider a remarkable way. It will get that 15 minute time down to pull all the ad links way down and optimize from the top.

There are about 435 sections. AWS has the following 14 regions:

1. us-east-1 US East (N. Virginia)
2. us-east-2 US East (Ohio)
3. us-west-1 US West (N. California)
4. us-west-2 US West (Oregon)
5. ca-central-1 Canada (Central)
6. eu-west-1 EU (Ireland)
7. eu-central-1 EU (Frankfurt)
8. eu-west-2 EU (London)
9. ap-northeast-1 Asia Pacific (Tokyo)
10. ap-northeast-2 Asia Pacific (Seoul)
11. p-southeast-1 Asia Pacific (Singapore)
12. ap-southeast-2 Asia Pacific (Sydney)
13. ap-south-1 Asia Pacific (Mumbai)
14. sa-east-1 South America (São Paulo)

I don't think I can use china for my purposes and definetely not US Gov Cloud
15. China (Beijing) - cn-north-1
16. US GovCloud West (Oregon) - us-gov-west-1

The beauty of this process is that I don't care about latency. I mean there are only about 35,00 ads. That's 35,000 requests after the initial 435 requests to get the front page ads for each US geographical region. If you think about 35,000 requests isn't that much. The limiting factor is the CDN's rate limiter. Latency isn't an issue and I can scale this globally.

The great thing about scaling globally is that I'm pretty much guaranteed to get a new IP. IP's represent physical locations so I'm guaranteed to have at least 14 IP addresses. Potentially even more, but I can only operate on absolute certainties. Each geographical region I expect to have at least one set of distinct IP's. So I can distribute the 35,000 ads over 14 physical machines (ahh the cloud doesn't escape physicalities)

I will have to create a special deployment process from scratch to upload one piece of code to 14 symmetrical lambda functions with the only variance being region. This is one of the great reasons to automate these processes. After speed there is `perfection`. I hate writing for the sake of speed. I love writing for the sake of perfection. It's just not feasible to deploy to 14 geographic regions and expect to have the same application. I would be nuts to think that. It's hard enough to manage one version.

I am calling this the `Lambda Symphony`. Now `Lambda Symphony` will take some .net core c# interface and publish the implementations all around the globe. Now the underlying code will all access data in one region US however, I will have a global network piping the data into one central location! Pretty sick huh? Worst case scenario, I need to develop a web service to pipe the data, but AWS should itself be operating on web services so I should have that taken care of for me. So here's what it will look like broken out.


## Get the link dictionary

For example:

- http://longbeach.backpage.com/
- http://losangeles.backpage.com/
- http://mendocino.backpage.com/

## Split the link dictionary by region

For example:

1. us-east-1 US East (N. Virginia)
- http://mendocino.backpage.com/
- http://merced.backpage.com/
- http://modesto.backpage.com/

2. us-east-2 US East (Ohio)
- http://monterey.backpage.com/
- http://northbay.backpage.com/
- http://eastbay.backpage.com/

3. us-west-1 US West (N. California)
- http://orangecounty.backpage.com/
- http://palmsprings.backpage.com/
- http://palmdale.backpage.com/

4. us-west-2 US West (Oregon)
- http://redding.backpage.com/
- http://sacramento.backpage.com/
- http://sandiego.backpage.com/

5. ca-central-1 Canada (Central)
- http://sanfernandovalley.backpage.com/
- http://sf.backpage.com/
- http://sangabrielvalley.backpage.com/

6. eu-west-1 EU (Ireland)
- http://sanjose.backpage.com/
- http://sanluisobispo.backpage.com/
- http://sanmateo.backpage.com/

7. eu-central-1 EU (Frankfurt)
- http://santabarbara.backpage.com/
- http://santacruz.backpage.com/
- http://santamaria.backpage.com/

8. eu-west-2 EU (London)
- http://siskiyou.backpage.com/
- http://stockton.backpage.com/
- http://susanville.backpage.com/

9. ap-northeast-1 Asia Pacific (Tokyo)
- http://ventura.backpage.com/
- http://visalia.backpage.com/
- http://boulder.backpage.com/

10. ap-northeast-2 Asia Pacific (Seoul)
- http://coloradosprings.backpage.com/
- http://denver.backpage.com/
- http://fortcollins.backpage.com/

11. p-southeast-1 Asia Pacific (Singapore)
- http://pueblo.backpage.com/
- http://rockies.backpage.com/
- http://westslope.backpage.com/

12. ap-southeast-2 Asia Pacific (Sydney)
- http://bridgeport.backpage.com/
- http://newlondon.backpage.com/
- http://hartford.backpage.com/

13. ap-south-1 Asia Pacific (Mumbai)
- http://newhaven.backpage.com/
- http://nwct.backpage.com/
- http://delaware.backpage.com/

14. sa-east-1 South America (São Paulo)
- http://nova.backpage.com/
- http://southernmaryland.backpage.com/
- http://dc.backpage.com/

## Drill into the links for a link dictionary

1. Get all front-page links
    1. For each link, get the content
2. Dump the link, date, title and content into DynamoDb

## Process the data
Now I have all the data. See the amount of data I'm working with isn't anything insane. It's big, but it's not insane. It's entirely manageable. It's really not that bad at all. So you see from here, for now, I can just pull back all of this data into a windows machine with Java 7 every night and anayze the content as shown in [memex](https://github.com/timg456789/CMU_memex).

I would like to use GateCloud then I could distribute the work and let each region run its own analysis and finish up its unit of work. Now I also could just feed the second level (top level is us.backpage.com) links into a SQS queue then let the regions fight to pull back all of the ads. Crap, so the max timeout is 300 seconds or 5 minutes in Lambda. I don't think I will be able to run through potentially 50 downloads in 3 minutes. I mean potentially I could, but the question raises issues. This needs another layer of distribution. I need two sets of lambda functions. Check this out.

Refer to indexing flow in /Visuals.

Now my mind is starting to break down on my ability to explain and describe this process. I need to retreat into sweet c# to describe this fine process. The one question is exactly how to distribute. I have all the mechanics, but the exact points at which I make certain moves will determine a great deal. The main question is does the process use a queue or not. Then is the process broken down into 3 or four distinct pieces.

Look to my pseudo code with this commit to describe the skeleton for this process. My concern is a timeout or some kind of failure. I need to make everything so distinct that failure is rare and not an issue when it does finally occur. It's the way we fall.

This repository is dedicated to documenting my journey to systematically discover and acquire fine art paintings.

https://tgonzalez.net

## Image Recognition

Check this out: https://aws.amazon.com/rekognition/pricing/

This is where I want to take this project, but I need more time.

## Content Delivery Network (CDN)

https://www.cloudflare.com/a/overview/tgonzalez.net

## SlideShowCreator

The SlideShowCreator project is used to crawl [the-athenaeum](www.the-athenaeum.org), in a respectable manner over the course of several days, to classify their 280,000+ image archive into dynamodb.

    {
      "artist": "dante gabriel rossetti",
      "date": "1869",
      "imageId": 736170,
      "name": "The Mandolin Player",
      "originalArtist": "Dante Gabriel Rossetti",
      "pageId": 33,
      "source": "http://www.the-athenaeum.org"
    }


## Gallery (MVCApp .NET Project)

The Gallery is the front-end for the images. The images aren't hosted. The gallery is simply an index for the images. All images displayed have references to the-athenaeum. The entire purose of the gallery is to increase the number of images a person can view over the course of a day.

The main feature of the gallery is a slide show for an image search.

## Gallery

Refer to `SlideshowCreator.DataAccessTests.ApiTests` for complete examples.

### Token

Call the `token` web service to get an authentication token which is safe to store in a cookie, because it is a cryptographic hash which will not leak identity information even if the cookie were to be exposed. The authentication token is good for one UTC day or upon a website publish. After that time, the source information to create the cookie has new random input, from a cryptographically secure random number generator, and produces an entirely new hash.

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

### Image Hosting
Image hosting will be a requirement for AWS rekognition. The images need to be hosted in an S3 bucket for analysis. This could be a long ways away, depending on how long it takes to build a crawler for the National Gallery of Art (NGA). However I can't easily access the images from images.nga.gov, so here are the next steps:
1. Re-crawl the-athenaeum fully transiently (working on this now)
2. Make a backup of the data
3. Create a crawler for images.nga.gov
4. Crawl images.nga.gov
    1. Get a temporary access token
    2. Download the super high resolution image
    3. Unzip the image
    4. Upload an s3 bucket
5. Upload the images for the-athenaeum to s3
    1. Most of the images exist locally from the first crawl.
    2. If the image doesn't exist locally on laptop, fetch a new image.
    3. Make the entire process transient
        1. clssify in dynamodb
        2. Upload to s3
6. Find a new image archive or analyze the images with rekognition

I really want to get another archive, then I can make the process generic. There will be more complex data, a variety of crawlers and a variety of source data. All of this should be placed into the same dynamodb table though. It can be effeciently partitioned with the "source" field.

## National Gallery of Art

I did see a browser check that said DDos protection from CloudFlare (CEO the Matthew prince). I'm not sure how that would affect a crawler. Perhaps it requires JavaScript execution. This would be a good challenge.

http://images.nga.gov
https://www.nga.gov/content/ngaweb/notices/terms-of-use.html

> Unless otherwise expressly provided elsewhere on the National Gallery of Art’s website (“Site”), and in such instance only as specifically provided, the contents of this Site, including all images and text, are for personal, educational, non-commercial use only, and may not be reproduced in any form without the permission of the National Gallery of Art.

http://images.nga.gov/en/page/openaccess.html

*the Gallery believes these images to be in the public domain*

>While the Gallery believes these images to be in the public domain, the Gallery can only give permissions with respect to rights it has and makes no representations or warranties that use of these images will not violate rights that persons or entities other than the Gallery may have under the laws of various countries.

>No copyright or other proprietary right in the image itself or in the underlying work of art is conveyed by making the image accessible. Furthermore, in making the image accessible, the Gallery does not grant the user an exclusive right to use or reproduce such image or work of art.

>As a courtesy to the Gallery and to enable others to identify and locate information about its collections, the Gallery encourages users to include the following credit with any use of one of its open access images: Courtesy National Gallery of Art, Washington

http://images.nga.gov/robots.txt

    User-agent: *
    Crawl-delay: 40

At a mere 40 seconds each with result sets of 75 that means it would take:

    45000 seconds / 75 results per page = 6000 paging requests
    6000*40seconds = 24,000 seconds / 60 seconds = 400 minutes / 60 minutes = 6.6666 hours

That's how long it would take to build an index of images. I want the super high res images, so I can create a good experience for a gallery. That means I'm looking at

    40 seconds * 45,000 images = 1,800,000 million seconds to download 45,000 images

    That breaks down to 21 days.

This actually isn't that bad. I need to index the site first, that will take more dev time than computing time. Then I can worry about scraping the high-res images.
I can also view the images transiently and avoid scraping entirely. I can buffer up some images and perhaps solve the problem entirely. It will be semi-complex, because the images are zipped, but that just means I can't do it in pure html like I'm planning with the-atheneum. Everything would need to be routed through a server which can download, unzip, then serve the image file.

## Development Requirements

1. Webstorm
2. Visual Studio
3. It's highly recommended to have a Solid State Drive (SSD)

## Helpful Projects

### Normal Random Numbers for Throttling
http://www.csharpcity.com/2010/normal-distribution-random-generator-available/

### Diacritic Squashing
https://github.com/thomasgalliker/Diacritics.NET
