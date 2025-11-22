// API Service - Base HTTP request handler
const apiService = (function() {
    const BASE_URL = 'https://localhost:7001/api'; // Adjust this to your API URL

    function getAuthHeader() {
        const token = localStorage.getItem('token');
        return token ? { 'Authorization': `Bearer ${token}` } : {};
    }

    async function request(url, options = {}) {
        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
                ...getAuthHeader(),
                ...options.headers
            }
        };

        const config = { ...defaultOptions, ...options };

        try {
            const response = await fetch(`${BASE_URL}${url}`, config);

            // Handle 401 Unauthorized
            if (response.status === 401) {
                localStorage.removeItem('token');
                localStorage.removeItem('currentUser');
                window.location.reload();
                throw new Error('Oturum süresi doldu. Lütfen tekrar giriş yapın.');
            }

            // Handle other error status codes
            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || `HTTP Error: ${response.status}`);
            }

            // Return parsed JSON if there's content
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                return await response.json();
            }

            return null;
        } catch (error) {
            console.error('API Request Error:', error);
            throw error;
        }
    }

    return {
        get: (url) => request(url, { method: 'GET' }),

        post: (url, data) => request(url, {
            method: 'POST',
            body: JSON.stringify(data)
        }),

        put: (url, data) => request(url, {
            method: 'PUT',
            body: JSON.stringify(data)
        }),

        delete: (url) => request(url, { method: 'DELETE' })
    };
})();
