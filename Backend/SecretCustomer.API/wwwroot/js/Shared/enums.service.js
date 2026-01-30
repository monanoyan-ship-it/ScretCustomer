// ===== Enums Service =====
// Enum verilerini API'den yukler ve cache'ler
// Tum sayfalarda kullanilabilir

var EnumsService = (function() {
    var _cache = null;
    var _loadPromise = null;

    // Enum verilerini yukle
    function load() {
        if (_cache) {
            return Promise.resolve(_cache);
        }

        if (_loadPromise) {
            return _loadPromise;
        }

        _loadPromise = fetch('/api/enums', { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error('Enum verileri yuklenemedi');
                return res.json();
            })
            .then(function(data) {
                _cache = data;
                return data;
            })
            .catch(function(err) {
                console.error('EnumsService.load error:', err);
                _loadPromise = null;
                throw err;
            });

        return _loadPromise;
    }

    // Cache'i temizle (nadiren gerekir)
    function clearCache() {
        _cache = null;
        _loadPromise = null;
    }

    // ID'ye gore enum item bul
    function getById(enumArray, id) {
        if (!enumArray) return null;
        return enumArray.find(function(item) { return item.id === id; });
    }

    // SystemName'e gore enum item bul
    function getBySystemName(enumArray, systemName) {
        if (!enumArray) return null;
        return enumArray.find(function(item) { return item.systemName === systemName; });
    }

    // Display name dondur (lokalize edilmis - API'den displayName olarak gelir)
    function getDisplayName(enumArray, idOrSystemName) {
        var item = typeof idOrSystemName === 'number'
            ? getById(enumArray, idOrSystemName)
            : getBySystemName(enumArray, idOrSystemName);

        if (!item) return 'Bilinmiyor';

        // API'den gelen displayName'i kullan
        if (item.displayName) {
            return item.displayName;
        }

        // Fallback: T fonksiyonu varsa lokalize et, yoksa nameKey'in son kismini kullan
        if (typeof T === 'function') {
            return T(item.nameKey, item.nameKey.split('.').pop());
        }
        return item.nameKey.split('.').pop();
    }

    // CSS class dondur
    function getCssClass(enumArray, idOrSystemName) {
        var item = typeof idOrSystemName === 'number'
            ? getById(enumArray, idOrSystemName)
            : getBySystemName(enumArray, idOrSystemName);

        return item ? item.cssClass : 'bg-secondary';
    }

    // Icon dondur
    function getIcon(enumArray, idOrSystemName) {
        var item = typeof idOrSystemName === 'number'
            ? getById(enumArray, idOrSystemName)
            : getBySystemName(enumArray, idOrSystemName);

        return item ? item.icon : 'bi-question';
    }

    // SelectList icin options olustur (KnockoutJS icin)
    // systemName: String value (backend icin), id: numeric (fallback)
    function toSelectOptions(enumArray) {
        if (!enumArray) return [];
        return enumArray.map(function(item) {
            // API'den gelen displayName'i kullan
            var name = item.displayName || (typeof T === 'function' ? T(item.nameKey, item.nameKey.split('.').pop()) : item.nameKey.split('.').pop());
            return {
                id: item.id,
                systemName: item.systemName,
                name: name
            };
        });
    }

    // Tip adina gore enum array dondur (cache'den)
    // Ornek: getByType('evaluationNotificationFrequency') => [{id: 0, ...}, {id: 1, ...}]
    function getByType(typeName) {
        if (!_cache) return [];
        return _cache[typeName] || [];
    }

    // ===== Kisayol metodlari =====

    // User Roles
    function getUserRoleDisplay(role) {
        if (!_cache) return role;
        return getDisplayName(_cache.userRoles, role);
    }

    function getUserRoleCss(role) {
        if (!_cache) return 'bg-secondary';
        return getCssClass(_cache.userRoles, role);
    }

    // Customer Personnel Roles
    function getCustomerRoleDisplay(role) {
        if (!_cache) return role;
        return getDisplayName(_cache.customerPersonnelRoles, role);
    }

    function getCustomerRoleCss(role) {
        if (!_cache) return 'bg-secondary';
        return getCssClass(_cache.customerPersonnelRoles, role);
    }

    // Assignment Statuses
    function getAssignmentStatusDisplay(status) {
        if (!_cache) return status;
        return getDisplayName(_cache.assignmentStatuses, status);
    }

    function getAssignmentStatusCss(status) {
        if (!_cache) return 'bg-secondary';
        return getCssClass(_cache.assignmentStatuses, status);
    }

    // Evaluation Statuses
    function getEvaluationStatusDisplay(status) {
        if (!_cache) return status;
        return getDisplayName(_cache.evaluationStatuses, status);
    }

    function getEvaluationStatusCss(status) {
        if (!_cache) return 'bg-secondary';
        return getCssClass(_cache.evaluationStatuses, status);
    }

    // Project Statuses
    function getProjectStatusDisplay(status) {
        if (!_cache) return status;
        return getDisplayName(_cache.projectStatuses, status);
    }

    function getProjectStatusCss(status) {
        if (!_cache) return 'bg-secondary';
        return getCssClass(_cache.projectStatuses, status);
    }

    // Penalty Types
    function getPenaltyTypeDisplay(type) {
        if (!_cache) return type;
        return getDisplayName(_cache.penaltyTypes, type);
    }

    function getPenaltyTypeCss(type) {
        if (!_cache) return 'bg-secondary';
        return getCssClass(_cache.penaltyTypes, type);
    }

    // Period Statuses
    function getPeriodStatusDisplay(status) {
        if (!_cache) return status;
        return getDisplayName(_cache.periodStatuses, status);
    }

    function getPeriodStatusCss(status) {
        if (!_cache) return 'bg-secondary';
        return getCssClass(_cache.periodStatuses, status);
    }

    // Approval Statuses
    function getApprovalStatusDisplay(status) {
        if (!_cache) return status;
        return getDisplayName(_cache.approvalStatuses, status);
    }

    function getApprovalStatusCss(status) {
        if (!_cache) return 'bg-secondary';
        return getCssClass(_cache.approvalStatuses, status);
    }

    // Months
    function getMonthDisplay(monthId) {
        if (!_cache || !_cache.months) return monthId;
        return getDisplayName(_cache.months, monthId);
    }

    // Ay numarasından lokalize edilmiş ay adı + yıl döndürür
    // Örnek: formatMonthYear(new Date()) => "Ocak 2026"
    function formatMonthYear(date) {
        if (!date) return '-';
        var d = new Date(date);
        var monthId = d.getMonth() + 1; // JavaScript months are 0-based
        var monthName = getMonthDisplay(monthId);
        return monthName + ' ' + d.getFullYear();
    }

    // Days of Week
    function getDayOfWeekDisplay(dayId) {
        if (!_cache || !_cache.daysOfWeek) return dayId;
        return getDisplayName(_cache.daysOfWeek, dayId);
    }

    // JavaScript Date'den lokalize edilmiş gün adı döndürür
    // JavaScript: Sunday=0, Monday=1, ..., Saturday=6
    function formatDayOfWeek(date) {
        if (!date) return '-';
        var d = new Date(date);
        var dayId = d.getDay(); // 0=Sunday, 1=Monday, ..., 6=Saturday
        return getDayOfWeekDisplay(dayId);
    }

    // Public API
    return {
        load: load,
        clearCache: clearCache,
        getById: getById,
        getBySystemName: getBySystemName,
        getDisplayName: getDisplayName,
        getCssClass: getCssClass,
        getIcon: getIcon,
        toSelectOptions: toSelectOptions,
        getByType: getByType,

        // Kisayollar
        getUserRoleDisplay: getUserRoleDisplay,
        getUserRoleCss: getUserRoleCss,
        getCustomerRoleDisplay: getCustomerRoleDisplay,
        getCustomerRoleCss: getCustomerRoleCss,
        getAssignmentStatusDisplay: getAssignmentStatusDisplay,
        getAssignmentStatusCss: getAssignmentStatusCss,
        getEvaluationStatusDisplay: getEvaluationStatusDisplay,
        getEvaluationStatusCss: getEvaluationStatusCss,
        getProjectStatusDisplay: getProjectStatusDisplay,
        getProjectStatusCss: getProjectStatusCss,
        getPenaltyTypeDisplay: getPenaltyTypeDisplay,
        getPenaltyTypeCss: getPenaltyTypeCss,
        getPeriodStatusDisplay: getPeriodStatusDisplay,
        getPeriodStatusCss: getPeriodStatusCss,
        getApprovalStatusDisplay: getApprovalStatusDisplay,
        getApprovalStatusCss: getApprovalStatusCss,
        getMonthDisplay: getMonthDisplay,
        formatMonthYear: formatMonthYear,
        getDayOfWeekDisplay: getDayOfWeekDisplay,
        formatDayOfWeek: formatDayOfWeek,

        // Cache'e dogrudan erisim (yuklendikten sonra)
        get cache() { return _cache; }
    };
})();
