function getImageUrl(item) {
    if (item.s3Path) {
        var url = `/api/Gallery/image` +
            `?token=${encodeURIComponent(getCookie('token'))}` +
            `&s3Path=${encodeURIComponent(item.s3Path)}`;
        return url;
    } else {
        return 'http://www.the-athenaeum.org/art/display_image.php?id=' + item.imageId;
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

function loadSearchResultsFromUrl(url) {
    fetch(url).then(function (response) {
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
        var url = `/api/Gallery/searchLikeArtist?token=${encodeURIComponent(getCookie('token'))}&artist=${encodeURIComponent($('#likeSearchText').val())}&source=${encodeURIComponent($('#siteSelection').val())}`;
        loadSearchResultsFromUrl(url);
    });

    $('#exactSearch').click(function () {
        var url = `/api/Gallery/searchExactArtist?token=${encodeURIComponent(encodeURIComponent(getCookie('token')))}&artist=${encodeURIComponent($('#exactSearchText').val())}&source=${encodeURIComponent($('#siteSelection').val())}`;
        loadSearchResultsFromUrl(url);
    });

    $('#idSearch').click(function () {
        var url = `/api/Gallery/scan?token=${encodeURIComponent(getCookie('token'))}&lastPageId=${encodeURIComponent($('#idSearchText').val())}&source=${encodeURIComponent($('#siteSelection').val())}`;
        loadSearchResultsFromUrl(url);
    });

    $('#tagSearch').click(function () {
        var url = `/api/Gallery/searchLabel?token=${encodeURIComponent(getCookie('token'))}&label=${encodeURIComponent($('#tagSearchText').val())}&source=${encodeURIComponent($('#siteSelection').val())}`;
        loadSearchResultsFromUrl(url);
    });

});