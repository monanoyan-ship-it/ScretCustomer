// API Service - Handles all HTTP requests
var apiService = (function() {
    'use strict';

    const BASE_URL = '/api';

    function getHeaders(includeAuth = true) {
        const headers = {
            'Content-Type': 'application/json'
        };

        if (includeAuth) {
            const token = localStorage.getItem('jwt_token');
            if (token) {
                headers['Authorization'] = 'Bearer ' + token;
            }
        }

        return headers;
    }

    function handleResponse(response) {
        if (!response.ok) {
            if (response.status === 401) {
                // Unauthorized - redirect to login
                if (typeof authService !== 'undefined') {
                    authService.logout();
                }
                window.location.href = '/Account/Login';
                return Promise.reject({ message: 'Oturum suresi doldu. Giris sayfasina yonlendiriliyorsunuz.' });
            }

            // Response'u text olarak al, JSON parse hatasi olabilir
            return response.text().then(function(text) {
                var errorObj = { message: 'Bir hata olustu.' };

                try {
                    // JSON parse etmeyi dene
                    var jsonError = JSON.parse(text);
                    errorObj = jsonError;
                } catch (parseError) {
                    // JSON degilse (HTML gibi), detayli hata mesaji olustur
                    var isDev = typeof APP_CONFIG !== 'undefined' && APP_CONFIG.isDevelopment();

                    if (isDev) {
                        // Development: Detayli hata
                        errorObj = {
                            message: 'API Hatasi [' + response.status + ' ' + response.statusText + '] - ' + response.url,
                            details: text.substring(0, 500),
                            status: response.status,
                            url: response.url
                        };
                        console.error('API Error Details:', {
                            status: response.status,
                            statusText: response.statusText,
                            url: response.url,
                            responseBody: text
                        });
                    } else {
                        // Production: Ozet hata
                        errorObj = {
                            message: 'Islem sirasinda bir hata olustu. Hata kodu: ' + response.status
                        };
                    }
                }

                return Promise.reject(errorObj);
            });
        }

        if (response.status === 204) {
            return Promise.resolve();
        }

        return response.json();
    }

    return {
        get: function(endpoint) {
            return fetch(BASE_URL + endpoint, {
                method: 'GET',
                headers: getHeaders(),
                credentials: 'include'
            }).then(handleResponse);
        },

        post: function(endpoint, data, includeAuth = true) {
            return fetch(BASE_URL + endpoint, {
                method: 'POST',
                headers: getHeaders(includeAuth),
                credentials: 'include',
                body: JSON.stringify(data)
            }).then(handleResponse);
        },

        put: function(endpoint, data) {
            return fetch(BASE_URL + endpoint, {
                method: 'PUT',
                headers: getHeaders(),
                credentials: 'include',
                body: JSON.stringify(data)
            }).then(handleResponse);
        },

        delete: function(endpoint) {
            return fetch(BASE_URL + endpoint, {
                method: 'DELETE',
                headers: getHeaders(),
                credentials: 'include'
            }).then(handleResponse);
        },

        // POST ile dosya indirme (Excel, PDF vb.)
        downloadPost: function(endpoint, data, filename) {
            return fetch(BASE_URL + endpoint, {
                method: 'POST',
                headers: getHeaders(),
                credentials: 'include',
                body: JSON.stringify(data)
            }).then(function(response) {
                if (!response.ok) {
                    return response.text().then(function(text) {
                        var errorMsg = 'Dosya indirme hatası';
                        try {
                            var jsonError = JSON.parse(text);
                            errorMsg = jsonError.message || errorMsg;
                        } catch (e) {
                            errorMsg = 'Hata kodu: ' + response.status;
                        }
                        return Promise.reject({ message: errorMsg });
                    });
                }
                return response.blob();
            }).then(function(blob) {
                // Blob'u dosya olarak indir
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = filename || 'download';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                document.body.removeChild(a);
            });
        },

        // GET ile dosya indirme
        downloadGet: function(endpoint, filename) {
            return fetch(BASE_URL + endpoint, {
                method: 'GET',
                headers: getHeaders(),
                credentials: 'include'
            }).then(function(response) {
                if (!response.ok) {
                    return response.text().then(function(text) {
                        var errorMsg = 'Dosya indirme hatası';
                        try {
                            var jsonError = JSON.parse(text);
                            errorMsg = jsonError.message || errorMsg;
                        } catch (e) {
                            errorMsg = 'Hata kodu: ' + response.status;
                        }
                        return Promise.reject({ message: errorMsg });
                    });
                }
                return response.blob();
            }).then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = filename || 'download';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                document.body.removeChild(a);
            });
        }
    };
})();

// Alias for backward compatibility
var ApiService = apiService;
