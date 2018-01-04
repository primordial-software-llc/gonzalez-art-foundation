function GalleryTemplate() {

    /**
     * https://stackoverflow.com/questions/901115/how-can-i-get-query-string-values-in-javascript
     * @param {any} name
     * @param {any} url
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

    function isAuthenticated() {
        return document.cookie &&
            document.cookie.indexOf('token=') > -1 &&
            document.cookie.indexOf('CF_Authorization=') > -1;
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
                if (document && document.cookie.indexOf('CF_Authorization=') < 0) {
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