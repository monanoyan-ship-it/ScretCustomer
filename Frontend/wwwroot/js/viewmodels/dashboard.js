// Dashboard ViewModel
function DashboardViewModel() {
    const self = this;

    self.stats = ko.observable({
        totalEvaluations: 0,
        averageScore: 0,
        percentageChange: 0,
        topBranches: [],
        bottomBranches: []
    });

    self.isLoading = ko.observable(false);

    self.loadDashboardData = async function() {
        self.isLoading(true);
        try {
            const currentUser = authService.getCurrentUser();
            let endpoint = '/dashboard/admin';

            // Determine which dashboard to load based on role
            if (currentUser.role === 'TeamLeader') {
                endpoint = `/dashboard/teamleader/${currentUser.userId}`;
            } else if (currentUser.role === 'CustomerRepresentative') {
                endpoint = `/dashboard/representative/${currentUser.branchId}`;
            }

            const data = await apiService.get(endpoint);
            self.stats(data);
        } catch (error) {
            console.error('Error loading dashboard:', error);
            alert('Dashboard verileri yüklenirken hata oluştu: ' + error.message);
        } finally {
            self.isLoading(false);
        }
    };

    // Load data when initialized
    self.loadDashboardData();
}
