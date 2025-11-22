// Main Application ViewModel
function AppViewModel() {
    const self = this;

    // Authentication state
    self.isAuthenticated = ko.observable(authService.isAuthenticated());
    self.currentUser = ko.observable(authService.getCurrentUser());
    self.isAdmin = ko.computed(() => self.currentUser() && self.currentUser().role === 'Admin');
    self.isTeamLeader = ko.computed(() => self.currentUser() && self.currentUser().role === 'TeamLeader');
    self.isEvaluator = ko.computed(() => self.currentUser() && self.currentUser().role === 'Evaluator');

    // Current page
    self.currentPage = ko.observable('login');

    // Loading state
    self.isLoading = ko.observable(false);

    // Login form
    self.loginUsername = ko.observable('');
    self.loginPassword = ko.observable('');
    self.loginError = ko.observable('');

    // Page ViewModels
    self.dashboardViewModel = null;
    self.checklistsViewModel = null;
    self.evaluationsViewModel = null;
    self.assignmentsViewModel = null;

    // Login function
    self.login = async function() {
        self.loginError('');
        self.isLoading(true);

        try {
            const response = await authService.login(
                self.loginUsername(),
                self.loginPassword()
            );

            // Update authentication state
            self.isAuthenticated(true);
            self.currentUser(authService.getCurrentUser());

            // Clear login form
            self.loginUsername('');
            self.loginPassword('');

            // Navigate to dashboard
            self.navigateTo('dashboard');
        } catch (error) {
            console.error('Login failed:', error);
            self.loginError(error.message || 'Giriş başarısız. Lütfen kullanıcı adı ve şifrenizi kontrol edin.');
        } finally {
            self.isLoading(false);
        }
    };

    // Logout function
    self.logout = function() {
        authService.logout();
        self.isAuthenticated(false);
        self.currentUser(null);
        self.currentPage('login');

        // Clean up ViewModels
        self.dashboardViewModel = null;
        self.checklistsViewModel = null;
        self.evaluationsViewModel = null;
        self.assignmentsViewModel = null;
    };

    // Navigation function
    self.navigateTo = function(page) {
        // Check authentication
        if (page !== 'login' && !self.isAuthenticated()) {
            self.currentPage('login');
            return;
        }

        // Check admin access for certain pages
        if ((page === 'checklists' || page === 'projects') && !self.isAdmin()) {
            alert('Bu sayfaya erişim yetkiniz yok.');
            return;
        }

        self.currentPage(page);

        // Initialize ViewModels when navigating to specific pages
        if (page === 'dashboard' && !self.dashboardViewModel) {
            self.dashboardViewModel = new DashboardViewModel();
        }

        if (page === 'checklists' && !self.checklistsViewModel) {
            self.checklistsViewModel = new ChecklistsViewModel();
        }

        if (page === 'evaluations' && !self.evaluationsViewModel) {
            self.evaluationsViewModel = new EvaluationsViewModel();
        }

        if (page === 'assignments' && !self.assignmentsViewModel) {
            self.assignmentsViewModel = new AssignmentsViewModel();
        }

        // Reload data if ViewModel already exists
        if (page === 'dashboard' && self.dashboardViewModel) {
            self.dashboardViewModel.loadDashboardData();
        }

        if (page === 'checklists' && self.checklistsViewModel) {
            self.checklistsViewModel.loadChecklists();
        }

        if (page === 'evaluations' && self.evaluationsViewModel) {
            self.evaluationsViewModel.loadPendingAssignments();
        }

        if (page === 'assignments' && self.assignmentsViewModel) {
            self.assignmentsViewModel.loadAssignments();
        }
    };

    // Initialize app
    self.init = function() {
        // Check if user is already logged in
        if (self.isAuthenticated()) {
            self.navigateTo('dashboard');
        } else {
            self.currentPage('login');
        }
    };

    // Run initialization
    self.init();
}

// Apply Knockout bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    const appViewModel = new AppViewModel();
    ko.applyBindings(appViewModel);
});
