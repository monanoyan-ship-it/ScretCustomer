// Delete Confirmation Modal Component
var deleteConfirmation = (function() {
    'use strict';

    var currentCallback = null;
    var modalElement = null;
    var messageElement = null;
    var confirmButton = null;

    function initialize() {
        modalElement = document.getElementById('deleteConfirmationModal');
        messageElement = document.getElementById('deleteConfirmationMessage');
        confirmButton = document.getElementById('confirmDeleteBtn');

        if (!modalElement || !messageElement || !confirmButton) {
            console.error('Delete confirmation modal elements not found in DOM');
            return false;
        }

        // Attach confirm button event
        confirmButton.addEventListener('click', function() {
            if (currentCallback) {
                currentCallback();
            }
            var modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) modal.hide();
        });

        return true;
    }

    function show(message, onConfirm) {
        if (!modalElement && !initialize()) {
            console.error('Cannot show delete confirmation modal');
            return;
        }

        messageElement.textContent = message || 'Bu kaydı silmek istediğinizden emin misiniz?';
        currentCallback = onConfirm;

        var modal = new bootstrap.Modal(modalElement);
        modal.show();
    }

    return {
        show: show
    };
})();
