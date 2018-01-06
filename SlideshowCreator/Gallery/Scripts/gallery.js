﻿function Gallery() {
    var slideshowTimer;
    var hasMovedMouseOnImageViewerPage = false;

    function getImageUrl(item) {
        if (item.s3Path) {
            var url = '/api/Gallery/image/tgonzalez-image-archive/national-gallery-of-art/' +
                item.s3Path.split('/').pop() + '/';
            return url;
        } else {
            return 'https://www.the-athenaeum.org/art/display_image.php?id=' + item.imageId;
        }
    }

    function assertSuccess(response, json) {
        if (!response || response.status < 200 || response.status > 299) {
            if (json && json.ExceptionMessage === 'Not authenticated') {
                alert('Please Login');
            } else {
                console.log(response);
                console.log(json);
                alert('Failed to get data: ' + JSON.stringify(json, 0, 4));
            }
            return false;
        }
        return true;
    }

    function showCurrentImage() {
        var slideshowItems = JSON.parse(localStorage.getItem('slideshowData'));
        var slideshowIndex = parseInt(localStorage.getItem("slideshowIndex"));
        if (isNaN(slideshowIndex)) {
            return;
        }
        var currentImage = slideshowItems[slideshowIndex];
        $('#slideshow-index').html(slideshowIndex + 1);
        $('#slideshow-count').html(slideshowItems.length);
        $('#slideshow-image').prop('src', getImageUrl(currentImage));

        var link;
        var linkText;

        if (currentImage.source === 'http://images.nga.gov') {
            linkText = 'Courtesy National Gallery of Art, Washington';
            link = currentImage.sourceLink;
        } else {
            linkText = "Courtesy The Athenaeum";
            link = 'http://www.the-athenaeum.org/art/detail.php?ID=' + currentImage.pageId;
        }
        
        $('#slideshow-image-info').html(currentImage.name + ' (' + currentImage.date + ') by ' + currentImage.originalArtist +
            ' - <a target="_blank" href="' + link + '">' + linkText + '</a>');

        var url = `/api/Gallery/${currentImage.pageId}/label`;
        fetch(url, { credentials: "same-origin" }).then(function (response) {
            response
                .json()
                .then(function (json) {
                    if (assertSuccess(response, json)) {
                        $('#slideshow-similar').prop('title', 'This work features ' + json.normalizedLabels);
                        $('#slideshow-similar').prop('href', '/Home?tags=' + json.normalizedLabels.join(','));
                    }
                })
                .catch(function (error) {
                    console.log('Failed to get data:');
                    console.log(error);
                });
        });
    }

    function previouseImage() {
        var slideshowIndex = parseInt(localStorage.getItem("slideshowIndex", 0));
        localStorage.setItem("slideshowIndex", slideshowIndex - 1);
        showCurrentImage();
    }

    function nextImage() {
        var slideshowIndex = parseInt(localStorage.getItem("slideshowIndex", 0));
        localStorage.setItem("slideshowIndex", slideshowIndex + 1);
        showCurrentImage();
    }

    function pauseSlideshow() {
        clearInterval(slideshowTimer);
        $('#slideshow-pause').hide();
        $('#slideshow-play').show();
    }

    function showPlayer() {
        hasMovedMouseOnImageViewerPage = true;
        $("#slideshow-player").slideDown("slow", function () {
            $("#slideshow-player").show();
            $('#slideshow-image-container').height('92%');
        });
        $('body').css('cursor', '');
    }

    function hidePlayer() {
        $('body').css('cursor', 'none');
        $("#slideshow-player").slideUp("slow", function () {
            $("#slideshow-player").hide();
            $('#slideshow-image-container').height('100%');
        });
    }

    /**
     * Chrome requires full-screen mode to be user engaged.
     */
    function showFullscreen() {
        hidePlayer();
        var element = document.getElementsByTagName('html')[0];
        if (element.webkitRequestFullScreen) {
            element.webkitRequestFullScreen();
        } else if (element.requestFullscreen) {
            element.requestFullscreen();
        } else if (element.mozRequestFullScreen) {
            element.mozRequestFullScreen();
        } else if (element.msRequestFullscreen) {
            element.msRequestFullscreen();
        }
    }

    function tryHidePlayer() {
        if (!hasMovedMouseOnImageViewerPage) {
            hidePlayer();
        }
        hasMovedMouseOnImageViewerPage = false;
    }
    
    function isFullScreen() {
        return window.fullScreen ||
            (window.innerWidth === screen.width && window.innerHeight === screen.height);
    }

    this.init = function() {
        $(document).ready(function () {
            showCurrentImage();

            $('#slideshow-fullscreen').click(function () {
                showFullscreen();
            });

            $(this).mousemove(function () {
                if (!isFullScreen()) {
                    showPlayer();
                }
            });

            $(this).keypress(function () {
                if (!isFullScreen()) {
                    showPlayer();
                }
            });

            setInterval(function () {
                tryHidePlayer();
            }, 15000);

            $('#slideshow-previous').click(function () {
                previouseImage();
                pauseSlideshow();
            });

            $('#slideshow-next').click(function () {
                nextImage();
                pauseSlideshow();
            });

            var defaultInterval = 6;
            $('#slideshow-interval').val(defaultInterval);
            $('#slideshow-pause').hide();
            $('#slideshow-play').click(function () {
                function slideshowTimerAction() {
                    nextImage();
                }

                var intervalInMs = parseFloat($('#slideshow-interval').val()) * 1000;
                console.log(intervalInMs);
                slideshowTimer = setInterval(slideshowTimerAction, intervalInMs);
                $('#slideshow-pause').show();
                $('#slideshow-play').hide();
            });

            $('#slideshow-pause').click(function () {
                pauseSlideshow();
            });
        });
    };
    
}

var gallery = new Gallery();
gallery.init();