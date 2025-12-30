// Auth Service - Handles authentication
var authService = (function() {
    'use strict';

    const TOKEN_KEY = 'jwt_token';
    const USER_KEY = 'user';

    return {
        login: function(username, password) {
            return apiService.post('/auth/login', { username, password }, false)
                .then(function(data) {
                    localStorage.setItem(TOKEN_KEY, data.token);
                    localStorage.setItem(USER_KEY, JSON.stringify(data.user));
                    return data.user;
                });
        },

        logout: function() {
            localStorage.removeItem(TOKEN_KEY);
            localStorage.removeItem(USER_KEY);
        },

        getToken: function() {
            return localStorage.getItem(TOKEN_KEY);
        },

        getUser: function() {
            const userStr = localStorage.getItem(USER_KEY);
            return userStr ? JSON.parse(userStr) : null;
        },

        isAuthenticated: function() {
            return !!this.getToken();
        },

        hasRole: function(role) {
            const user = this.getUser();
            return user && user.role === role;
        },

        isAdmin: function() {
            return this.hasRole('Admin');
        },

        isQualitySpecialist: function() {
            return this.hasRole('QualitySpecialist');
        },

        isFieldWorker: function() {
            return this.hasRole('FieldWorker');
        },

        getCurrentUser: function() {
            if (!this.isAuthenticated()) {
                return Promise.reject('Not authenticated');
            }
            return apiService.get('/auth/me');
        }
    };
})();
