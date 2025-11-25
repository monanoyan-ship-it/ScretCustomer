// Main Application - Routing and initialization
(function() {
    'use strict';

    var app = Sammy('#app-container', function() {
        var self = this;

        // Helper to check authentication
        function requireAuth() {
            if (!authService.isAuthenticated()) {
                self.redirect('#/login');
                return false;
            }
            return true;
        }

        // Helper to apply bindings
        function applyViewModel(viewModel, templateUrl) {
            // Load template
            fetch(templateUrl)
                .then(res => res.text())
                .then(html => {
                    const container = document.getElementById('app-container');
                    ko.cleanNode(container);
                    container.innerHTML = html;
                    ko.applyBindings(viewModel, container);
                })
                .catch(err => {
                    console.error('Error loading template:', err);
                    alert('Sayfa yüklenirken bir hata olu_tu.');
                });
        }

        // Routes
        this.get('#/login', function() {
            if (authService.isAuthenticated()) {
                this.redirect('#/dashboard');
                return;
            }
            applyViewModel(new LoginViewModel(), '/templates/login.html');
        });

        this.get('#/dashboard', function() {
            if (!requireAuth()) return;
            applyViewModel(new DashboardViewModel(), '/templates/dashboard.html');
        });

        this.get('#/checklists', function() {
            if (!requireAuth()) return;
            if (!authService.isAdmin()) {
                alert('Bu sayfaya eri_im yetkiniz yok.');
                self.redirect('#/dashboard');
                return;
            }
            applyViewModel(new ChecklistViewModel(), '/templates/checklists.html');
        });

        this.get('#/checklists/:id', function() {
            if (!requireAuth()) return;
            const id = this.params.id;
            applyViewModel(new ChecklistDetailViewModel(id), '/templates/checklist-detail.html');
        });

        this.get('#/projects', function() {
            if (!requireAuth()) return;
            if (!authService.isAdmin()) {
                alert('Bu sayfaya eri_im yetkiniz yok.');
                self.redirect('#/dashboard');
                return;
            }
            applyViewModel(new ProjectViewModel(), '/templates/projects.html');
        });

        this.get('#/assignments', function() {
            if (!requireAuth()) return;
            applyViewModel(new AssignmentViewModel(), '/templates/assignments.html');
        });

        this.get('#/assignments/:id/evaluate', function() {
            if (!requireAuth()) return;
            const id = this.params.id;
            applyViewModel(new EvaluationViewModel(id), '/templates/evaluation.html');
        });

        this.get('#/evaluations', function() {
            if (!requireAuth()) return;
            applyViewModel(new EvaluationListViewModel(), '/templates/evaluation-list.html');
        });

        // Default route
        this.get('/', function() {
            if (authService.isAuthenticated()) {
                this.redirect('#/dashboard');
            } else {
                this.redirect('#/login');
            }
        });
    });

    // Start the application when DOM is ready
    $(document).ready(function() {
        app.run('#/');
    });
})();
