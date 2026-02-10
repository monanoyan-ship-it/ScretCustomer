/**
 * Shared Confirmation Modal Helper
 * Usage: showConfirmModal(options)
 *
 * Options:
 *   title: Modal title (default: uses localization)
 *   message: Message to display (default: uses localization)
 *   type: 'warning' | 'danger' | 'info' | 'success' | 'primary' (default: 'warning')
 *   confirmText: Confirm button text (default: uses localization)
 *   confirmIcon: Confirm button icon class (default: 'bi-check')
 *   onConfirm: Callback function when confirmed
 */

var _confirmModalCallback = null;

function showConfirmModal(options) {
    options = options || {};

    var type = options.type || 'warning';
    var headerClass = 'bg-' + type;
    var btnClass = 'btn-' + type;
    var icon = options.icon || getDefaultIcon(type);

    // Set modal content with localization fallback
    document.getElementById('confirmModalHeader').className = 'modal-header ' + headerClass;
    document.getElementById('confirmModalIcon').className = 'bi ' + icon + ' me-2';
    document.getElementById('confirmModalTitle').textContent = options.title || T('Common.Confirmation', 'Onay');
    document.getElementById('confirmModalMessage').innerHTML = options.message || T('Confirm.Message', 'Bu işlemi onaylıyor musunuz?');
    document.getElementById('confirmModalBtn').className = 'btn ' + btnClass;
    document.getElementById('confirmModalBtnIcon').className = 'bi ' + (options.confirmIcon || 'bi-check') + ' me-1';
    document.getElementById('confirmModalBtnText').textContent = options.confirmText || T('Common.Confirm', 'Onayla');

    // Store callback
    _confirmModalCallback = options.onConfirm;

    // Show modal
    var modal = new bootstrap.Modal(document.getElementById('sharedConfirmModal'));
    modal.show();
}

function getDefaultIcon(type) {
    switch(type) {
        case 'danger': return 'bi-exclamation-triangle-fill';
        case 'warning': return 'bi-question-circle-fill';
        case 'success': return 'bi-check-circle-fill';
        case 'info': return 'bi-info-circle-fill';
        default: return 'bi-question-circle-fill';
    }
}

// Handle confirm button click
document.addEventListener('DOMContentLoaded', function() {
    var confirmBtn = document.getElementById('confirmModalBtn');
    if (confirmBtn) {
        confirmBtn.addEventListener('click', function() {
            var modal = bootstrap.Modal.getInstance(document.getElementById('sharedConfirmModal'));
            if (modal) modal.hide();

            if (_confirmModalCallback && typeof _confirmModalCallback === 'function') {
                _confirmModalCallback();
            }
            _confirmModalCallback = null;
        });
    }
});

// Shortcut functions for common cases
function showDeleteConfirm(itemName, onConfirm) {
    var message = itemName
        ? itemName + ' ' + T('Common.DeleteConfirmationMessage', 'silinecek. Bu işlemi onaylıyor musunuz?')
        : T('Common.DeleteConfirmationMessage', 'Bu kaydı silmek istediğinizden emin misiniz?');

    showConfirmModal({
        title: T('Common.DeleteConfirmation', 'Silme Onayı'),
        message: message,
        type: 'danger',
        confirmText: T('Common.YesDelete', 'Evet, Sil'),
        confirmIcon: 'bi-trash',
        onConfirm: onConfirm
    });
}

function showCancelConfirm(itemName, onConfirm) {
    var message = itemName
        ? itemName + ' ' + T('Confirm.CancelMessage', 'iptal edilecek. Bu işlemi onaylıyor musunuz?')
        : T('Confirm.CancelMessage', 'Bu işlemi iptal etmek istediğinizden emin misiniz?');

    showConfirmModal({
        title: T('Confirm.CancelTitle', 'İptal Onayı'),
        message: message,
        type: 'warning',
        confirmText: T('Confirm.YesCancel', 'Evet, İptal Et'),
        confirmIcon: 'bi-x-circle',
        onConfirm: onConfirm
    });
}

function showWarningConfirm(title, message, onConfirm) {
    showConfirmModal({
        title: title,
        message: message,
        type: 'warning',
        confirmText: T('Common.Confirm', 'Onayla'),
        confirmIcon: 'bi-check',
        onConfirm: onConfirm
    });
}

function showInfoConfirm(title, message, onConfirm) {
    showConfirmModal({
        title: title,
        message: message,
        type: 'info',
        confirmText: T('Common.OK', 'Tamam'),
        confirmIcon: 'bi-check',
        onConfirm: onConfirm
    });
}
