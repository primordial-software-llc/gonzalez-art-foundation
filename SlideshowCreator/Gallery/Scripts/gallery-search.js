
function getImageUrl(item) {
    if (item.s3Path) {
        var url = '/api/Gallery/image/tgonzalez-image-archive/national-gallery-of-art-alt?s3Name=' +
            item.s3Path.split('/').pop();
        return url;
    } else {
        return 'https://www.the-athenaeum.org/art/display_image.php?id=' + item.imageId;
    }
}

function loadSearchResults(results) {
    var html = '';
    html += '<div id="search-results-summary">Works of art found: ' + results.length + '</div>';
    html += '<div id="slideshow-start"><img src="/Images/Glyphicons/glyphicons-9-film.png"> Start Slideshow </div>';
    for (var ct = 0; ct < results.length; ct += 1) {
        var result = results[ct];
        html += '<div><a target="_blank" href="' + getImageUrl(result) + '" title="' + result.source + ' - ' + result.pageId + '"' + '>' +
            results[ct].name +
            ' (' + results[ct].date + ') by ' +
            results[ct].originalArtist + '</a></div>';
    }

    $('#search-results').html(html);

    $('#slideshow-start').click(function () {
        localStorage.setItem("slideshowData", JSON.stringify(results));
        localStorage.setItem("slideshowIndex", 0);
        window.location = "/Home/ImageViewer";
    });
}

function assertSuccess(response, json) {
    if (!response || response.status < 200 || response.status > 299) {
        console.log(response);
        console.log(json);
        alert('Failed to get data: ' + JSON.stringify(json, 0, 4));
        return false;
    }
    return true;
}

function loadSearchResultsFromUrl(url) {
    fetch(url, { credentials: "same-origin" }).then(function (response) {
        if (response.status === 403) {
            alert('Credentials are invalid. Logout and login with valid credentials');
            return;
        }
        response
            .json()
            .then(function (json) {
                if (assertSuccess(response, json)) {
                    loadSearchResults(json);   
                }
            })
            .catch(function (error) {
                console.log('Failed to get data:');
                console.log(error);
            });
    });
}

var getUrlParameter = function getUrlParameter(sParam) {
    var sPageURL = decodeURIComponent(window.location.search.substring(1)),
        sURLVariables = sPageURL.split('&'),
        sParameterName,
        i;

    for (i = 0; i < sURLVariables.length; i++) {
        sParameterName = sURLVariables[i].split('=');

        if (sParameterName[0] === sParam) {
            return sParameterName[1] === undefined ? true : sParameterName[1];
        }
    }
};

$(document).ready(function () {

    var tags = getUrlParameter('tags');
    if (tags) {
        $('#tagSearchText').val(tags);
    }

    $('#likeSearch').click(function () {
        var url = `/api/Gallery/searchLikeArtist?artist=${encodeURIComponent($('#likeSearchText').val())}&source=${encodeURIComponent($('#siteSelection').val())}`;
        loadSearchResultsFromUrl(url);
    });

    $('#exactSearch').click(function () {
        var url = `/api/Gallery/searchExactArtist?artist=${encodeURIComponent($('#exactSearchText').val())}&source=${encodeURIComponent($('#siteSelection').val())}`;
        loadSearchResultsFromUrl(url);
    });

    $('#idSearch').click(function () {
        var url = `/api/Gallery/scan?lastPageId=${encodeURIComponent($('#idSearchText').val())}&source=${encodeURIComponent($('#siteSelection').val())}`;
        loadSearchResultsFromUrl(url);
    });

    $('#tagSearch').click(function () {
        var url = `/api/Gallery/searchLabel?label=${encodeURIComponent($('#tagSearchText').val())}&source=${encodeURIComponent($('#siteSelection').val())}`;
        loadSearchResultsFromUrl(url);
    });

});