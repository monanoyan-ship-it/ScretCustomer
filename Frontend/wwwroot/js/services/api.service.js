// API Service - Handles all HTTP requests
var apiService = (function() {
    'use strict';

    const BASE_URL = 'https://localhost:7001/api';

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
                authService.logout();
                window.location.hash = '#/login';
            }
            return response.json().then(err => Promise.reject(err));
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
                headers: getHeaders()
            }).then(handleResponse);
        },

        post: function(endpoint, data, includeAuth = true) {
            return fetch(BASE_URL + endpoint, {
                method: 'POST',
                headers: getHeaders(includeAuth),
                body: JSON.stringify(data)
            }).then(handleResponse);
        },

        put: function(endpoint, data) {
            return fetch(BASE_URL + endpoint, {
                method: 'PUT',
                headers: getHeaders(),
                body: JSON.stringify(data)
            }).then(handleResponse);
        },

        delete: function(endpoint) {
            return fetch(BASE_URL + endpoint, {
                method: 'DELETE',
                headers: getHeaders()
            }).then(handleResponse);
        }
    };
})();
