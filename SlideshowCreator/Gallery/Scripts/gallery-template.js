function GalleryTemplate() {

    /**
     * https://stackoverflow.com/questions/901115/how-can-i-get-query-string-values-in-javascript
     * @param {string} name Cookie name
     * @param {string} url Site url
     * @returns {string} url parameter value
     */
    function getParameterByName(name, url) {
        if (!url) url = window.location.href;
        name = name.replace(/[\[\]]/g, "\\$&");
        var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
            results = regex.exec(url);
        if (!results) return null;
        if (!results[2]) return '';
        return decodeURIComponent(results[2].replace(/\+/g, " "));
    }

    /**
     * CloudFlare Access has no affect outside of production without requests being proxied through CloudFlare.
     * I would need to have a a public test site on a subdomain.
     * @returns {boolean} true if in production and CloudFlare Authorization cookie exists.
     */
    function isAuthenticatedInCloudFlare() {
        if (window.location.hostname !== 'tgonzalez.net') {
            return true;
        }
        return document.cookie && document.cookie.indexOf('CF_Authorization=') > -1;
    }

    function isAuthenticated() {
        return document.cookie &&
            document.cookie.indexOf('token=') > -1 && isAuthenticatedInCloudFlare();
    }

    function updateAuthenticationForm() {
        if (isAuthenticated()) {
            $('#login-form').hide();
            $('#authenticated-user-form').show();
        } else {
            $('#login-form').show();
            $('#authenticated-user-form').hide();
        }
    }

    function authenticateUsernamePassword(username, password) {
        var url = '/api/Gallery/token' +
            '?username=' + encodeURIComponent(username) +
            '&password=' + encodeURIComponent(password);
        fetch(url, { credentials: "same-origin" }).then(function (response) {
            if (response.status === 403) {
                alert('Credentials are invalid. Logout and login with valid credentials');
                return;
            }
            
            response
                .json()
                .then(function (json) {
                    var d = new Date(json.expirationDate);
                    var expires = "expires=" + d.toUTCString();
                    document.cookie = 'token=' + encodeURIComponent(json.token) + ';' + expires + ";path=/";
                    updateAuthenticationForm();
                });
        });
    }

    this.init = function() {
        $(document).ready(function () {

            if (getParameterByName('username')) {
                $('#username').val(getParameterByName('username'));
            }

            if (getParameterByName('password')) {
                $('#password').val(getParameterByName('password'));
            }

            updateAuthenticationForm();

            $('#login').click(function () {
                if (!isAuthenticatedInCloudFlare()) { // It's better to login with a token first, then you don't need the awkwardly timed delay after the redirect.
                    window.location.href = 'https://tgonzalez.net/api/Gallery/twoFactorAuthenticationRedirect?galleryPath=' +
                        encodeURIComponent(
                            '/?username=' + $('#username').val() +
                            '&password=' + $('#password').val());
                } else {
                    authenticateUsernamePassword($('#username').val(), $('#password').val());
                }
            });

            $('#signout').click(function () {
                document.cookie = "token=;expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
                updateAuthenticationForm();
            });

            if ($('#username').val().length > 0 && $('#password').val().length > 0) {
                $('#login').click();
            }

        });
    };

}