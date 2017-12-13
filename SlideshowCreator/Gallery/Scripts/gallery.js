var slideshowTimer;
var hasMovedMouseOnImageViewerPage = false;

function getImageUrl(item) {
    return 'http://www.the-athenaeum.org/art/display_image.php?id=' + item.imageId;
}

function getCookie(cname) {
    var name = cname + "=";
    var decodedCookie = decodeURIComponent(document.cookie);
    var ca = decodedCookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
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

    var url = `/api/Gallery/${currentImage.pageId}/label?token=${encodeURIComponent(getCookie('token'))}`;
    fetch(url).then(function (response) {
        response
            .json()
            .then(function (json) {
                if (assertSuccess(response, json)) {
                    $('#slideshow-similar').prop('title', 'This work features ' + json.normalizedLabels);
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

$(document).ready(function () {
    showCurrentImage();

    $(this).mousemove(function () {
        hasMovedMouseOnImageViewerPage = true;
        $("#slideshow-player").slideDown("slow", function () {
            $("#slideshow-player").show();
            $('#slideshow-image-container').height('92%');
        });
    });
    $(this).keypress(function () {
        hasMovedMouseOnImageViewerPage = true;
        $("#slideshow-player").slideDown("slow", function () {
            $("#slideshow-player").show();
            $('#slideshow-image-container').height('92%');
        });
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