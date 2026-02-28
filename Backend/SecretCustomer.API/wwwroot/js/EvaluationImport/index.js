"use strict";

function EvaluationImportViewModel() {
    var self = this;

    // Upload
    self.selectedFile = ko.observable(null);
    self.isUploading = ko.observable(false);
    self.showCustomerSelect = ko.observable(false);
    self.customerList = ko.observableArray([]);
    self.selectedCustomerId = ko.observable(null);
    self.pendingUploadData = null; // stores FormData when customer selection is needed

    // Sessions
    self.sessions = ko.observableArray([]);
    self.isLoading = ko.observable(false);

    // Session Detail
    self.selectedSession = ko.observable(null);

    // Unmatched Items
    self.unmatchedItems = ko.observableArray([]);
    self.unmatchedTypeFilter = ko.observable(null);
    self.isLoadingUnmatched = ko.observable(false);
    self.unmatchedPage = ko.observable(1);
    self.unmatchedTotalCount = ko.observable(0);
    self.unmatchedPageSize = ko.observable(50);
    self.unmatchedTotalPages = ko.computed(function () {
        return Math.ceil(self.unmatchedTotalCount() / self.unmatchedPageSize()) || 1;
    });
    self.unmatchedVisiblePages = ko.computed(function () {
        var current = self.unmatchedPage();
        var total = self.unmatchedTotalPages();
        var pages = [];
        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);
        for (var i = start; i <= end; i++) pages.push(i);
        return pages;
    });

    // Pending Rows
    self.pendingRows = ko.observableArray([]);
    self.rowStatusFilter = ko.observable(null);
    self.isLoadingRows = ko.observable(false);
    self.isImporting = ko.observable(false);
    self.rowsPage = ko.observable(1);
    self.rowsTotalCount = ko.observable(0);
    self.rowsPageSize = ko.observable(50);
    self.rowsTotalPages = ko.computed(function () {
        return Math.ceil(self.rowsTotalCount() / self.rowsPageSize()) || 1;
    });
    self.rowsVisiblePages = ko.computed(function () {
        var current = self.rowsPage();
        var total = self.rowsTotalPages();
        var pages = [];
        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);
        for (var i = start; i <= end; i++) pages.push(i);
        return pages;
    });

    // Resolve Modal
    self.resolveItem = ko.observable(null);
    self.resolveSearchQuery = ko.observable("");
    self.resolveSearchResults = ko.observableArray([]);
    self.resolveSelectedEntityId = ko.observable(null);
    self.resolveNewFirstName = ko.observable("");
    self.resolveNewLastName = ko.observable("");
    // Project creation
    self.resolveNewProjectName = ko.observable("");
    self.resolveChecklistList = ko.observableArray([]);
    self.resolveSelectedChecklistId = ko.observable(null);

    var searchDebounceTimer = null;

    // ===== File Upload =====
    self.handleFileSelect = function (vm, event) {
        var files = event.target.files;
        if (files && files.length > 0) {
            self.selectedFile(files[0]);
        }
    };

    self.uploadFile = function (customerId) {
        if (!self.selectedFile() || self.isUploading()) return;

        var formData = new FormData();
        formData.append("file", self.selectedFile());
        if (customerId) formData.append("customerId", customerId);

        self.isUploading(true);
        fetch("/api/evaluation-import/upload", {
            method: "POST",
            body: formData
        })
            .then(function (response) {
                if (!response.ok) return response.json().then(function (err) { throw err; });
                return response.json();
            })
            .then(function (data) {
                var msg = "Excel dosyası başarıyla işlendi. " + data.importedRows + " kayıt direkt içe aktarıldı, " + data.pendingRows + " kayıt beklemede.";
                if (data.skippedRows > 0) msg += " " + data.skippedRows + " kayıt tekrar (CallId) olduğu için atlandı.";
                toastr.success(msg);
                self.selectedFile(null);
                self.showCustomerSelect(false);
                self.selectedCustomerId(null);
                var fileInput = document.querySelector('#evaluation-import-app input[type="file"]');
                if (fileInput) fileInput.value = "";
                self.loadSessions();
            })
            .catch(function (err) {
                if (err.code === "CUSTOMER_REQUIRED") {
                    // Customer not found in Excel, show customer picker
                    self.showCustomerSelect(true);
                    self.loadCustomers();
                    toastr.warning("Excel'deki firma adı sistemde bulunamadı. Lütfen müşteri seçin.");
                } else {
                    toastr.error(err.message || "Dosya yüklenirken hata oluştu.");
                }
            })
            .finally(function () {
                self.isUploading(false);
            });
    };

    self.uploadWithCustomer = function () {
        if (!self.selectedCustomerId()) {
            toastr.warning("Lütfen bir müşteri seçin.");
            return;
        }
        self.uploadFile(self.selectedCustomerId());
    };

    self.loadCustomers = function () {
        fetch("/api/evaluation-import/customers")
            .then(function (response) { return response.json(); })
            .then(function (data) {
                self.customerList(data);
            });
    };

    // ===== Sessions =====
    self.loadSessions = function () {
        self.isLoading(true);
        fetch("/api/evaluation-import/sessions")
            .then(function (response) { return response.json(); })
            .then(function (data) {
                var mapped = data.map(function (s) {
                    s.createdAtFormatted = formatDate(s.createdAt);
                    return s;
                });
                self.sessions(mapped);
            })
            .catch(function () {
                toastr.error("Oturumlar yüklenirken hata oluştu.");
            })
            .finally(function () {
                self.isLoading(false);
            });
    };

    self.openSessionDetail = function (session) {
        self.isLoadingUnmatched(false);
        self.isLoadingRows(false);
        self.unmatchedItems([]);
        self.pendingRows([]);
        self.unmatchedPage(1);
        self.rowsPage(1);

        fetch("/api/evaluation-import/sessions/" + session.id)
            .then(function (response) { return response.json(); })
            .then(function (detail) {
                enrichSessionDetail(detail);
                self.selectedSession(detail);
                self.loadUnmatchedItems(null);
                self.loadPendingRows(null);

                var modal = new bootstrap.Modal(document.getElementById("sessionDetailModal"));
                modal.show();
            })
            .catch(function () {
                toastr.error("Oturum detayı yüklenirken hata oluştu.");
            });
    };

    // ===== Unmatched Items =====
    self.loadUnmatchedItems = function (typeId) {
        var session = self.selectedSession();
        if (!session) return;

        if (typeId !== undefined) {
            self.unmatchedTypeFilter(typeId);
            self.unmatchedPage(1);
        }

        self.isLoadingUnmatched(true);

        var params = new URLSearchParams();
        if (self.unmatchedTypeFilter() !== null && self.unmatchedTypeFilter() !== undefined)
            params.append("itemType", self.unmatchedTypeFilter());
        params.append("page", self.unmatchedPage());
        params.append("pageSize", self.unmatchedPageSize());

        fetch("/api/evaluation-import/sessions/" + session.id + "/unmatched?" + params.toString())
            .then(function (response) { return response.json(); })
            .then(function (data) {
                self.unmatchedItems(data.items);
                self.unmatchedTotalCount(data.totalCount);
            })
            .catch(function () {
                toastr.error("Eşleşmeyen öğeler yüklenirken hata oluştu.");
            })
            .finally(function () {
                self.isLoadingUnmatched(false);
            });
    };

    self.unmatchedPrevPage = function () {
        if (self.unmatchedPage() > 1) {
            self.unmatchedPage(self.unmatchedPage() - 1);
            self.loadUnmatchedItems();
        }
    };
    self.unmatchedNextPage = function () {
        if (self.unmatchedPage() < self.unmatchedTotalPages()) {
            self.unmatchedPage(self.unmatchedPage() + 1);
            self.loadUnmatchedItems();
        }
    };
    self.unmatchedGoToPage = function (page) {
        if (page >= 1 && page <= self.unmatchedTotalPages()) {
            self.unmatchedPage(page);
            self.loadUnmatchedItems();
        }
    };

    // ===== Pending Rows =====
    self.loadPendingRows = function (statusId) {
        var session = self.selectedSession();
        if (!session) return;

        if (statusId !== undefined) {
            self.rowStatusFilter(statusId);
            self.rowsPage(1);
        }

        self.isLoadingRows(true);

        var params = new URLSearchParams();
        if (self.rowStatusFilter() !== null && self.rowStatusFilter() !== undefined)
            params.append("status", self.rowStatusFilter());
        params.append("page", self.rowsPage());
        params.append("pageSize", self.rowsPageSize());

        fetch("/api/evaluation-import/sessions/" + session.id + "/pending-rows?" + params.toString())
            .then(function (response) { return response.json(); })
            .then(function (data) {
                var mapped = data.items.map(function (r) {
                    r.parsedCallDateFormatted = r.parsedCallDate ? formatDate(r.parsedCallDate) : "-";
                    return r;
                });
                self.pendingRows(mapped);
                self.rowsTotalCount(data.totalCount);
            })
            .catch(function () {
                toastr.error("Bekleyen satırlar yüklenirken hata oluştu.");
            })
            .finally(function () {
                self.isLoadingRows(false);
            });
    };

    self.rowsPrevPage = function () {
        if (self.rowsPage() > 1) {
            self.rowsPage(self.rowsPage() - 1);
            self.loadPendingRows();
        }
    };
    self.rowsNextPage = function () {
        if (self.rowsPage() < self.rowsTotalPages()) {
            self.rowsPage(self.rowsPage() + 1);
            self.loadPendingRows();
        }
    };
    self.rowsGoToPage = function (page) {
        if (page >= 1 && page <= self.rowsTotalPages()) {
            self.rowsPage(page);
            self.loadPendingRows();
        }
    };

    // ===== Resolve Modal =====
    self.openResolveModal = function (item) {
        self.resolveItem(item);
        self.resolveSearchQuery("");
        self.resolveSearchResults([]);
        self.resolveSelectedEntityId(null);

        // Kişi veya Evaluator tipiyse originalValue'dan ad/soyad parse et
        if ((item.itemTypeId === 1 || item.itemTypeId === 2) && item.originalValue) {
            var parts = item.originalValue.trim().split(/\s+/);
            self.resolveNewFirstName(parts[0] || "");
            self.resolveNewLastName(parts.slice(1).join(" ") || "");
        } else {
            self.resolveNewFirstName("");
            self.resolveNewLastName("");
        }

        // Proje tipiyse proje adını ve checklist listesini hazırla
        if (item.itemTypeId === 3) {
            self.resolveNewProjectName(item.originalValue || "");
            self.resolveSelectedChecklistId(null);
            if (self.resolveChecklistList().length === 0) {
                fetch("/api/evaluation-import/checklists")
                    .then(function (r) { return r.json(); })
                    .then(function (data) { self.resolveChecklistList(data); });
            }
        }

        // Tüm tipler için mevcut listeyi otomatik yükle
        loadEntityList(item);

        var modal = new bootstrap.Modal(document.getElementById("resolveModal"));
        modal.show();
    };

    function loadEntityList(item) {
        var session = self.selectedSession();
        var customerId = session ? session.customerId : null;
        var url;

        if (item.itemTypeId === 3) {
            url = "/api/evaluation-import/search/projects?q=";
            if (customerId) url += "&customerId=" + customerId;
        } else if (item.itemTypeId === 1) {
            url = "/api/evaluation-import/search/personnel?customerId=" + (customerId || 0) + "&q=";
        } else if (item.itemTypeId === 2) {
            url = "/api/evaluation-import/search/users?q=";
        } else {
            return;
        }

        fetch(url)
            .then(function (response) { return response.json(); })
            .then(function (data) {
                self.resolveSearchResults(data);
            });
    }

    self.resolveSearchQuery.subscribe(function (query) {
        clearTimeout(searchDebounceTimer);
        var item = self.resolveItem();

        if (!query || query.length < 2) {
            // Boş query'de tam listeyi göster
            if (item) {
                loadEntityList(item);
            } else {
                self.resolveSearchResults([]);
            }
            return;
        }
        searchDebounceTimer = setTimeout(function () {
            performResolveSearch(query);
        }, 300);
    });

    function performResolveSearch(query) {
        var item = self.resolveItem();
        if (!item) return;

        var url;
        var session = self.selectedSession();
        if (item.itemTypeId === 1) {
            var customerId = session ? session.customerId : 0;
            url = "/api/evaluation-import/search/personnel?customerId=" + (customerId || 0) + "&q=" + encodeURIComponent(query);
        } else if (item.itemTypeId === 2) {
            url = "/api/evaluation-import/search/users?q=" + encodeURIComponent(query);
        } else {
            var cId = session ? session.customerId : null;
            url = "/api/evaluation-import/search/projects?q=" + encodeURIComponent(query);
            if (cId) url += "&customerId=" + cId;
        }

        fetch(url)
            .then(function (response) { return response.json(); })
            .then(function (data) {
                self.resolveSearchResults(data);
            })
            .catch(function () {
                self.resolveSearchResults([]);
            });
    }

    self.selectResolveEntity = function (entity) {
        self.resolveSelectedEntityId(entity.id);
    };

    self.resolveLink = function () {
        var item = self.resolveItem();
        if (!item || !self.resolveSelectedEntityId()) return;

        resolveUnmatchedItem(item.id, {
            entityId: self.resolveSelectedEntityId(),
            actionId: 1
        });
    };

    self.resolveCreateNew = function () {
        var item = self.resolveItem();
        if (!item) return;

        var dto = { actionId: 2 };

        if (item.itemTypeId === 3) {
            // Project
            if (!self.resolveSelectedChecklistId()) {
                toastr.warning("Lütfen bir kontrol listesi seçin.");
                return;
            }
            dto.newProjectName = self.resolveNewProjectName();
            dto.newProjectChecklistId = self.resolveSelectedChecklistId();
        } else {
            // Person
            dto.newFirstName = self.resolveNewFirstName();
            dto.newLastName = self.resolveNewLastName();
        }

        resolveUnmatchedItem(item.id, dto);
    };

    self.resolveSkip = function () {
        var item = self.resolveItem();
        if (!item) return;

        resolveUnmatchedItem(item.id, {
            actionId: 3
        });
    };

    function resolveUnmatchedItem(itemId, dto) {
        fetch("/api/evaluation-import/unmatched/" + itemId + "/resolve", {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(dto)
        })
            .then(function (response) {
                if (!response.ok) return response.json().then(function (err) { throw err; });
                return response.json();
            })
            .then(function () {
                toastr.success("Öğe başarıyla çözümlendi.");
                bootstrap.Modal.getInstance(document.getElementById("resolveModal"))?.hide();

                self.loadUnmatchedItems();
                self.loadPendingRows();
                refreshSessionDetail();
            })
            .catch(function (err) {
                toastr.error(err.message || "Çözümleme sırasında hata oluştu.");
            });
    }

    // ===== Import Resolved =====
    self.importResolved = function () {
        var session = self.selectedSession();
        if (!session || self.isImporting()) return;

        self.isImporting(true);
        fetch("/api/evaluation-import/sessions/" + session.id + "/import-resolved", {
            method: "POST"
        })
            .then(function (response) {
                if (!response.ok) return response.json().then(function (err) { throw err; });
                return response.json();
            })
            .then(function (result) {
                toastr.success(result.importedCount + " kayıt başarıyla içe aktarıldı." +
                    (result.failedCount > 0 ? " " + result.failedCount + " kayıt başarısız." : ""));

                self.loadPendingRows();
                refreshSessionDetail();
                self.loadSessions();
            })
            .catch(function (err) {
                toastr.error(err.message || "İçe aktarma sırasında hata oluştu.");
            })
            .finally(function () {
                self.isImporting(false);
            });
    };

    // ===== Helpers =====
    function enrichSessionDetail(detail) {
        detail.importedPercent = ko.computed(function () {
            return detail.totalRows > 0 ? Math.round((detail.importedRows / detail.totalRows) * 100) : 0;
        });
        detail.pendingPercent = ko.computed(function () {
            return detail.totalRows > 0 ? Math.round((detail.pendingRows / detail.totalRows) * 100) : 0;
        });
        detail.skippedPercent = ko.computed(function () {
            return detail.totalRows > 0 ? Math.round((detail.skippedRows / detail.totalRows) * 100) : 0;
        });
        detail.unmatchedTotal = ko.computed(function () {
            return (detail.unmatchedPersonCount || 0) + (detail.unmatchedEvaluatorCount || 0) + (detail.unmatchedProjectCount || 0);
        });
    }

    function refreshSessionDetail() {
        var session = self.selectedSession();
        if (!session) return;

        fetch("/api/evaluation-import/sessions/" + session.id)
            .then(function (response) { return response.json(); })
            .then(function (detail) {
                enrichSessionDetail(detail);
                self.selectedSession(detail);
            });
    }

    function formatDate(dateStr) {
        if (!dateStr) return "-";
        var d = new Date(dateStr);
        if (isNaN(d.getTime())) return dateStr;
        var day = String(d.getDate()).padStart(2, "0");
        var month = String(d.getMonth() + 1).padStart(2, "0");
        var year = d.getFullYear();
        var hours = String(d.getHours()).padStart(2, "0");
        var minutes = String(d.getMinutes()).padStart(2, "0");
        return day + "." + month + "." + year + " " + hours + ":" + minutes;
    }

    // Init
    self.loadSessions();
}

$(document).ready(function () {
    ko.applyBindings(new EvaluationImportViewModel(), document.getElementById("evaluation-import-app"));
});
