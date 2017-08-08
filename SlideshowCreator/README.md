# Slide Show Creator

## Background
I received a 40" HD TV for my birthday and I'm not a huge fan of television. My apartment is bare. The art that I want to purchase is far too costly even with reproductions. I only have two true pictures of art. I want more! I want imagery of meaningful ideas that inspire me as I read and write; not *decorations* which merely fill up space.

## Purpose
The purpose of this project is to peruse works of art in the public domain. I want to make it easy to build a personal art gallery at a relatively low-cost. Art on canvas is expensive and costly. Even to visit a museum is expensive in terms of time. I want to create a personal art gallery with HD TV's where I can invest with less risk and transform it at the push of a button. To start I need to build a collection of works that I enjoy then I can look into something more than a slideshow on a single monitor. This process will allow me to select pieces that I love and purchase quality reproductions on canvas.

## Procedure
I have found a website with high quality images in the public domain. The site I have found the-athenaeum claims to have over 250,000 works of art. The pieces that I'm looking for in particular by Jean Leon Gerome are of high quality and there are over 250 images. Getting those images alone will be worth the efforts to display them on a large TV.

If all goes well I may make a donation to the site upon completion for their tremendous work in collecting these images and sharing them publicly. I will be certain to respect the website and throttle the traffic I direct to the site so that I don't appear as more than a few users browsing heavily during the night and possibly over the course of several days. I don't want to deny anyone's access even though I will be going in over a VPN just in case (which I always use anyway for basic network security).

Here are a few notes from the websites mission statement to ensure I'm not doing anything outright undesirable:
 - http://www.the-athenaeum.org/about/mission_statement.php
 - We are working to be a repository of primary source materials, whether they are images of artworks, texts, maps, or other related materials. But beyond that, we want to give you new ways to store and share your own notes, to make connections, and build an interconnected web of learning.
 - You can reuse the artwork (but not our logos or original text) in any way, as long as you credit us.
 - All people should have maximum access to art, and the good things that flow from creative works.

So far I have tested downloading an image off this site and displaying it on my TV with a USB stick. Furthermore the works I'm targeting are in the public domain and there are no visible terms of service on the website. In truth I'm not even going to know what I've downloaded until far later. All the data will have to be held privately for personal usage. I don't need to duplicate the site, only the data. I should be fine with the image rights as long as I don't try to actually open up a gallery. With this knowledge and testing I just need to grab all of the images and categorize them now and dump that to a USB drive.

1. Get all data for the images
2. Target artists of my choosing to download the full images
3. Copy to USB
4. Plug into TV

### Categorization

#### Data Structure

##### Dates
Dates shall be in lexographical format at any level of precision: [FULL_YEAR]-[LEADING-ZERO-MONTH]-[LEADING-ZERO-DAY-OF-MONTH]

For example:
- 1890
- 1890-01
- 1890-01-01

```javascript
[
    {
        artist: { name: 'Jean-Leon Gerome' },
        date: '1890',
        name: 'Pygmalion and Galatea (study)',
        location: 'http://www.the-athenaeum.org/art/display_image.php?id=32778'
    },
    {
        artist: { name: 'Claude Lorrain' },
        date: '1646',
        name: 'The Embarkation of Ulysses',
        location: 'http://www.the-athenaeum.org/art/display_image.php?id=303119'
    }
]
```

#### Image Archive
- Artist
    - Jean-Leon Gerome
        - pygmalion-and-galatea-study.jpg
    - Claude Lorrain
        - the-embarkation-of-ulysses.jpg

The image archive is organized physically due to the current limitation of needing to do a slideshown on usb stick to easily work on a simple HD TV. More thought should be put into this for long-term storage. No matter what I require a backup of the entire data set. References will not suffice. In our day and age web sites are so cheap they often dissapear and this collection is too precious to allow such a tragedy. I actually intend to backup the data up offline on physical media. For now the plan is a CD. The backup procedure is to load the CD onto a disk drive incapable of writing making the backup safe from the malicious software running rampant even during a crisis backup scenario. Was it thought that the Library of Alexandria would purposefully go up in flames before it had?