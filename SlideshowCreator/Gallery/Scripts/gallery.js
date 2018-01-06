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
    var currentImage = slideshowItems[slideshowIndex];
    $('#slideshow-index').html(slideshowIndex + 1);
    $('#slideshow-count').html(slideshowItems.length);
    $('#slideshow-image').prop('src', getImageUrl(currentImage));
    $('#slideshow-image-info').html(currentImage.name + ' (' + currentImage.date + ') by ' + currentImage.originalArtist + ' ' +
        ' Source ' + '<a target="_blank" href="http://www.the-athenaeum.org/art/detail.php?ID=' + currentImage.pageId + '">' + currentImage.source + '</a>');

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

function showFullsize() {
    hasMovedMouseOnImageViewerPage = true;
    $("#slideshow-player").slideDown("slow", function () {
        $("#slideshow-player").show();
        $('#slideshow-image-container').height('92%');
    });
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

$(document).ready(function () {
    showCurrentImage();

    $(this).mousemove(function () {
        showFullsize();
    });
    $(this).keypress(function () {
        showFullsize();
    });

    setInterval(function () {
        if (!hasMovedMouseOnImageViewerPage) {
            $("#slideshow-player").slideUp("slow", function () {
                $("#slideshow-player").hide();
                $('#slideshow-image-container').height('100%');
            });
        }
        hasMovedMouseOnImageViewerPage = false;
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