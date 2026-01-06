// Permissions ViewModel
function PermissionsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.modalErrorMessage = ko.observable('');
    self.successMessage = ko.observable('');

    // Data
    self.permissions = ko.observableArray([]);
    self.categories = ko.observableArray([]);
    self.scopes = ko.observableArray([]);
    self.roles = ko.observableArray([]);
    self.users = ko.observableArray([]);
    self.rolePermissionsMap = ko.observable({});

    // Filters
    self.categoryFilter = ko.observable('');
    self.userSearchTerm = ko.observable('');

    // Selection
    self.selectedRole = ko.observable(null);
    self.selectedUser = ko.observable(null);
    self.selectedRolePermissionIds = ko.observableArray([]);
    self.userPermissionsSummary = ko.observable(null);

    // New user permission form
    self.newUserPermission = {
        permissionId: ko.observable(''),
        isGranted: ko.observable('true'),
        scope: ko.observable(1),
        validFrom: ko.observable(''),
        validUntil: ko.observable(''),
        notes: ko.observable('')
    };

    // Modal
    self.userPermissionModal = null;

    // Filtered permissions by category
    self.filteredPermissions = ko.computed(function() {
        var filter = self.categoryFilter();
        if (!filter) return self.permissions();
        return self.permissions().filter(function(p) {
            return p.category == filter;
        });
    });

    // Permissions grouped by category
    self.permissionsByCategory = ko.computed(function() {
        var perms = self.permissions();
        var grouped = {};
        perms.forEach(function(p) {
            var catName = p.categoryName;
            if (!grouped[catName]) {
                grouped[catName] = {
                    categoryName: catName,
                    permissions: []
                };
            }
            grouped[catName].permissions.push(p);
        });
        return Object.values(grouped);
    });

    // Filtered users by search term
    self.filteredUsers = ko.computed(function() {
        var term = self.userSearchTerm().toLowerCase();
        if (!term) return self.users();
        return self.users().filter(function(u) {
            return (u.firstName + ' ' + u.lastName).toLowerCase().indexOf(term) !== -1 ||
                   (u.email || '').toLowerCase().indexOf(term) !== -1;
        });
    });

    // Load all data
    self.loadData = function() {
        self.isLoading(true);

        Promise.all([
            fetch('/api/permissions', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/permissions/categories', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/permissions/scopes', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/permissions/roles-list', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/permissions/roles', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/users', { credentials: 'include' }).then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            self.permissions(results[0]);
            self.categories(results[1]);
            self.scopes(results[2]);
            self.roles(results[3]);

            // Build role permissions map
            var map = {};
            results[4].forEach(function(roleSummary) {
                map[roleSummary.role] = roleSummary.permissions.map(function(p) { return p.id; });
            });
            self.rolePermissionsMap(map);

            // Map users with role names
            var roleNames = {};
            self.roles().forEach(function(r) { roleNames[r.value] = r.name; });
            var mappedUsers = results[5].map(function(u) {
                u.roleName = roleNames[u.role] || u.role;
                return u;
            });
            self.users(mappedUsers);
        })
        .catch(function(error) {
            console.error('Error:', error);
            toastr.error('Veriler yüklenirken bir hata oluştu.');
        })
        .finally(function() {
            self.isLoading(false);
        });
    };

    // Get role permission count
    self.getRolePermissionCount = function(roleValue) {
        var map = self.rolePermissionsMap();
        return (map[roleValue] || []).length;
    };

    // Select role
    self.selectRole = function(role) {
        self.selectedRole(role);
        var map = self.rolePermissionsMap();
        self.selectedRolePermissionIds(map[role.value] || []);
    };

    // Check if permission is granted to selected role
    self.isPermissionGranted = function(permissionId) {
        return self.selectedRolePermissionIds().indexOf(permissionId) !== -1;
    };

    // Toggle role permission
    self.toggleRolePermission = function(permissionId) {
        var ids = self.selectedRolePermissionIds();
        var index = ids.indexOf(permissionId);
        if (index === -1) {
            ids.push(permissionId);
        } else {
            ids.splice(index, 1);
        }
        self.selectedRolePermissionIds(ids);
    };

    // Save role permissions
    self.saveRolePermissions = function() {
        if (!self.selectedRole()) return;

        self.isSaving(true);
        self.errorMessage('');

        var dto = {
            role: self.selectedRole().value,
            permissionIds: self.selectedRolePermissionIds(),
            scope: 1 // PermissionScope.All
        };

        fetch('/api/permissions/roles/bulk', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
        .then(function(response) {
            if (!response.ok) {
                return response.json().then(function(err) {
                    throw new Error(err.message || 'İşlem başarısız');
                });
            }
            return response.json();
        })
        .then(function(data) {
            toastr.success('Rol yetkileri başarıyla güncellendi.');
            // Update map
            var map = self.rolePermissionsMap();
            map[self.selectedRole().value] = self.selectedRolePermissionIds().slice();
            self.rolePermissionsMap(map);
        })
        .catch(function(error) {
            console.error('Error:', error);
            toastr.error(error.message || 'Yetkiler kaydedilirken bir hata oluştu.');
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    // Select user
    self.selectUser = function(user) {
        self.selectedUser(user);
        self.loadUserPermissions(user.id);
    };

    // Load user permissions
    self.loadUserPermissions = function(userId) {
        self.isLoading(true);

        fetch('/api/permissions/users/' + userId, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Kullanıcı yetkileri yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.userPermissionsSummary(data);
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(error.message);
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Show add user permission modal
    self.showAddUserPermissionModal = function() {
        // Reset form
        self.newUserPermission.permissionId('');
        self.newUserPermission.isGranted('true');
        self.newUserPermission.scope(1);
        self.newUserPermission.validFrom('');
        self.newUserPermission.validUntil('');
        self.newUserPermission.notes('');
        self.modalErrorMessage('');

        if (!self.userPermissionModal) {
            self.userPermissionModal = new bootstrap.Modal(document.getElementById('addUserPermissionModal'));
        }
        self.userPermissionModal.show();
    };

    // Add user permission
    self.addUserPermission = function() {
        if (!self.selectedUser() || !self.newUserPermission.permissionId()) {
            toastr.error('Lütfen bir yetki seçin.');
            return;
        }

        self.isSaving(true);
        self.modalErrorMessage('');

        var dto = {
            userId: self.selectedUser().id,
            permissionId: self.newUserPermission.permissionId(),
            isGranted: self.newUserPermission.isGranted() === 'true',
            scope: parseInt(self.newUserPermission.scope()),
            validFrom: self.newUserPermission.validFrom() || null,
            validUntil: self.newUserPermission.validUntil() || null,
            notes: self.newUserPermission.notes() || null
        };

        fetch('/api/permissions/users', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
        .then(function(response) {
            if (!response.ok) {
                return response.json().then(function(err) {
                    throw new Error(err.message || 'İşlem başarısız');
                });
            }
            return response.json();
        })
        .then(function(data) {
            toastr.success('Kullanıcı yetkisi başarıyla eklendi.');
            self.userPermissionModal.hide();
            self.loadUserPermissions(self.selectedUser().id);
        })
        .catch(function(error) {
            console.error('Error:', error);
            toastr.error(error.message || 'Yetki eklenirken bir hata oluştu.');
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    // Remove user permission
    self.removeUserPermission = function(permission) {
        showDeleteConfirm('Bu özel yetki', function() {
            fetch('/api/permissions/users/' + permission.userId + '/' + permission.permissionId, {
                method: 'DELETE',
                credentials: 'include'
            })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(err) {
                        throw new Error(err.message || 'İşlem başarısız');
                    });
                }
                return response.json();
            })
            .then(function(data) {
                toastr.success('Kullanıcı yetkisi başarıyla kaldırıldı.');
                self.loadUserPermissions(self.selectedUser().id);
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(error.message || 'Yetki kaldırılırken bir hata oluştu.');
            });
        });
    };

    // Initialize
    self.loadData();
}

// Apply bindings when document is ready
$(document).ready(function() {
    ko.applyBindings(new PermissionsViewModel(), document.getElementById('permissions-app'));
});
