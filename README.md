# Slide Show Creator

This repository is dedicated to documenting my journey to systematically discover fine art paintings.

## Next Steps By Priority

### Backup The Athenaeum Images to s3
Last night I went through a terrible scenario. The-athenaeum became inaccessible and the Gallery was shut down! I can't allow such an incident to occur. I have local backups of the images. These images need to go into s3 with a reference in DynamoDb, just like the National Gallery of Art.

### Index Data for National Galley of Art
This is actually really simple. CloudFlare's I'm under attack mode has been disabled (last I checked) so crawling should be slightly easier. I can also pull the data back in bulk of 75 items at a time from the search results so this should be a small one night task. I've already done that crawl to get the image links I just never got the name/artist/date data.

### Make National Gallery of Art Available from the API's and the Gallery Website
This is self-explanatory. I may need to adjust the HTML/JavaScript to account for the new source e.g. I link to the original source page. That link is probably hard-coded. I store the source website, but not the exact link. This would be an extra field and should probably just go on each record and then all that is abstracted from HTML/JavaScript entirely and in the API.

## Gallery
The Gallery is where I browse a lifetime supply of visual art in a never-ending slideshow from anywhere in the world that has a web browser with internet. As of this moment only the-athenaeum data can be browsed in the Gallery. While seemingly simple I have found it extraordinarily difficult to browse and explore fine art at scale. The task is far too difficult to do manually. The goal is an experiece that is as rich as visiting a museum. Sacrificing some quality of imagery for breadth of what is explored. My goal with the Gallery website is to truly challenge a visit to a museum. 20MB images look stunning on a 100" HD projector in total darkness with a quality screen.

https://tgonzalez.net

### Data Sources

*The Athenaeum doesn't have the image files and the National Gallery of Art doesn't have the image data.*

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

### API's

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

## Content Delivery Network (CDN)

I think I can perform lossless compression of images with CloudFlare. It's a new feature released on their anniversary. The only question is does it help with a single user? This would be huge if I opened up the Gallery to the public.

https://www.cloudflare.com/a/overview/tgonzalez.net

## Helpful Projects

### Normal Random Numbers for Throttling
http://www.csharpcity.com/2010/normal-distribution-random-generator-available/

### Diacritic Squashing
https://github.com/thomasgalliker/Diacritics.NET
