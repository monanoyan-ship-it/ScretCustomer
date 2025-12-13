// Customer API Service
var customerApiService = (function() {
    'use strict';

    return {
        // Customer endpoints
        getAllCustomers: function(includeInactive = false) {
            return apiService.get('/customers?includeInactive=' + includeInactive);
        },

        getActiveCustomers: function() {
            return apiService.get('/customers/active');
        },

        getCustomerById: function(id) {
            return apiService.get('/customers/' + id);
        },

        createCustomer: function(customerData) {
            return apiService.post('/customers', customerData);
        },

        updateCustomer: function(id, customerData) {
            return apiService.put('/customers/' + id, customerData);
        },

        deleteCustomer: function(id) {
            return apiService.delete('/customers/' + id);
        },

        // Customer Personnel endpoints
        getAllPersonnel: function(includeInactive = false) {
            return apiService.get('/customer-personnel?includeInactive=' + includeInactive);
        },

        getPersonnelByCustomerId: function(customerId, includeInactive = false) {
            return apiService.get('/customer-personnel/by-customer/' + customerId + '?includeInactive=' + includeInactive);
        },

        getPersonnelById: function(id) {
            return apiService.get('/customer-personnel/' + id);
        },

        createPersonnel: function(personnelData) {
            return apiService.post('/customer-personnel', personnelData);
        },

        updatePersonnel: function(id, personnelData) {
            return apiService.put('/customer-personnel/' + id, personnelData);
        },

        deletePersonnel: function(id) {
            return apiService.delete('/customer-personnel/' + id);
        },

        changePersonnelPassword: function(id, passwordData) {
            return apiService.post('/customer-personnel/' + id + '/change-password', passwordData);
        },

        // Admin tarafından şifre sıfırlama - eski şifre gerektirmez
        resetPersonnelPassword: function(id, newPassword) {
            return apiService.post('/customer-personnel/' + id + '/reset-password', { newPassword: newPassword });
        }
    };
})();
