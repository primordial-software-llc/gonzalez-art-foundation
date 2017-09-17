# Slide Show Creator

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

## Helpful Projects

### Normal Random Numbers for Throttling
http://www.csharpcity.com/2010/normal-distribution-random-generator-available/

### Diacritic Squashing
https://github.com/thomasgalliker/Diacritics.NET
