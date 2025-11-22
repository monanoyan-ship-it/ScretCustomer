// Authentication Service
const authService = (function() {

    async function login(username, password) {
        try {
            const response = await apiService.post('/auth/login', {
                username: username,
                password: password
            });

            // Save token and user info to localStorage
            localStorage.setItem('token', response.token);
            localStorage.setItem('currentUser', JSON.stringify({
                userId: response.userId,
                username: response.username,
                fullName: response.fullName,
                role: response.role,
                branchId: response.branchId
            }));

            return response;
        } catch (error) {
            console.error('Login error:', error);
            throw error;
        }
    }

    async function register(userData) {
        try {
            const response = await apiService.post('/auth/register', userData);

            // Save token and user info to localStorage
            localStorage.setItem('token', response.token);
            localStorage.setItem('currentUser', JSON.stringify({
                userId: response.userId,
                username: response.username,
                fullName: response.fullName,
                role: response.role,
                branchId: response.branchId
            }));

            return response;
        } catch (error) {
            console.error('Register error:', error);
            throw error;
        }
    }

    function logout() {
        localStorage.removeItem('token');
        localStorage.removeItem('currentUser');
    }

    function getCurrentUser() {
        const userJson = localStorage.getItem('currentUser');
        return userJson ? JSON.parse(userJson) : null;
    }

    function isAuthenticated() {
        return !!localStorage.getItem('token');
    }

    function isAdmin() {
        const user = getCurrentUser();
        return user && user.role === 'Admin';
    }

    function isTeamLeader() {
        const user = getCurrentUser();
        return user && user.role === 'TeamLeader';
    }

    function isEvaluator() {
        const user = getCurrentUser();
        return user && user.role === 'Evaluator';
    }

    return {
        login,
        register,
        logout,
        getCurrentUser,
        isAuthenticated,
        isAdmin,
        isTeamLeader,
        isEvaluator
    };
})();
