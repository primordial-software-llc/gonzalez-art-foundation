# Slide Show Creator

This repository is dedicated to documenting my journey to systematically discover and acquire fine art paintings.

## DynamoDB Settings

### Table
Read Capacity: 5
Write Capacity: 25
Estimated Monthly Cost: $12.58

### Global Secondary Index
Read Capacity: 25
Write Capacity: 5
Estimated Monthly Cost: $4.84

25 write should be just more than what I need for transient classification. Then 25 read is an absolute guess at what is fast on lookup up by artist name.

`I'll probably need another Global Secondary Index for Name once I support that use case`

- Do I need to search by name alone? How likely is it that you know the artist, but not the name? With the Artist global secondary index, name is the sort key, so the work under an artist is already index and will be fast. The problem here is having to duplicate the fields for searchability. If I need to add another index on a name field, that means storing the original name, then squashing diacritics and case. More importantly it means two duplicate fields. It begs the question of at which point do I use something like ElasticSearch?

This should be adequate for transient classification and application usage.

## Acquisitions

[Expectations (1885) by Sir Lawrence Alma-Tadema]( http://www.the-athenaeum.org/art/detail.php?ID=329)

## SlideShowCreator

The SlideShowCreator project is used to crawl [the-athenaeum](www.the-athenaeum.org), in a respectable manner over the course of several days, to download their 250,000+ image archive and classify the images. I chose the-athenaeum, because it has a very open mission statement with no Robots.txt and no terms of service. The process has two steps:

1. Enumerate page id's and download the following for each page to the local hard disk
    1. Html File
    2. Full image JPEG referenced on the page
2. Enumerate local html files and classify the content
    1. Name
    2. Artist
    3. Date
    4. ImageId

*I should have captured the page id, but that's stored in the physical local file. I will have to add the field when I push the data to a more usable repository*

The process yields the following folder structure:

1. ImageArchive (100Gb)
2. HtmlArchive (2Gb)
3. Classification (20Mb)

*Caution: Downloading over 100Gb into a single folder can cause undesired affects to a computer. The recommended course of action is to use an SSD and research windows folder optimizations. Don't attempt to version the content with Git.*

`Need to think about downloading images when classifying transiently. This is all I want to store besides the index.`

## Gallery

### Authentication
At the very minimum, the gallery will store a salted hash of a password. The password will be sent in plain-text over SSL. This is entirely adequate for read-only access for a resource which simply needs to remain `persoal` until I can figure out what I have and at least identify what is and is not in the public domain. Potentially I could re-write non-commercial pieces of software in use and do whatever I'd like with a portion of the site with public domain imagery.

### Image Hosting
As long as I access the images transiently the problem of image copyright ceases to exist. Simply keep a personal backup for disaster scenarios. The index is king, like one of the most valuable books in [The Library of Babel](https://en.wikipedia.org/wiki/The_Library_of_Babel).

### Viewing
Images will be viewed from the source using the index I've built to navigate until there are either access, quality or performance issues, which have yet to be the case.

Use Cases

1. I load the application and it has saved where I last left off. I override the default rate from 15 seconds to 1 minute.

2. While viewing the automatic slideshow, I press pause and temporarily disable the automatic slideshow. Once I'm done writing notes about the image I press play and continue the slideshow.

3. While viewing the automatic slideshow, I press forward to go to the next image prior to the configured rate.

4. When the automatic slideshow switches to the next image, I hit back then press pause to return to the previous image and stay there.

5. I load the application and enter an artists name. I view all images for the artist with an automatic slideshow. Once I'm done, I go back to enumeration mode and continue where I left off while enumerating.

*To start, there is no need to save the position. I will keep my last position in my physical notebook, but this would be the most desirable non-essential feature. No matter what though I need to be able to set the position in case if I fall asleep while viewing.*

### Application Structure

  Github $7 per month (HTML/CSS/JavasScript hosting)

  Dynamo DB $1 estimated per month (Data mapping)

  AWS EC2 Windows $15 estimated per month(Rest API to dynamo db and to delegate authentication)

## Equipment

1. Desktop with SSD for development
2. 1080p projector with 100 inch screen
3. Laptop dedicated for viewing

## Future Data Targets

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

    #
    # "$Id: robots.txt 3494 2003-03-19 15:37:44Z mike $"
    #
    #   This file tells search engines not to index your CUPS server.
    #
    #   Copyright 1993-2003 by Easy Software Products.
    #
    #   These coded instructions, statements, and computer programs are the
    #   property of Easy Software Products and are protected by Federal
    #   copyright law.  Distribution and use rights are outlined in the file
    #   "LICENSE.txt" which should have been included with this file.  If this
    #   file is missing or damaged please contact Easy Software Products
    #   at:
    #
    #       Attn: CUPS Licensing Information
    #       Easy Software Products
    #       44141 Airport View Drive, Suite 204
    #       Hollywood, Maryland 20636-3111 USA
    #
    #       Voice: (301) 373-9600
    #       EMail: cups-info@cups.org
    #         WWW: http://www.cups.org
    #

    #User-agent: *
    #Disallow: /

    User-agent: *
    Crawl-delay: 40
    #
    # End of "$Id: robots.txt 3494 2003-03-19 15:37:44Z mike $".
    #

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
