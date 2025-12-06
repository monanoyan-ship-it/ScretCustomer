// Delete Confirmation Modal Component
var deleteConfirmation = (function() {
    'use strict';

    var currentCallback = null;
    var modalElement = null;
    var messageElement = null;

    function initialize() {
        // Create modal HTML if not exists
        if (document.getElementById('deleteConfirmationModal')) {
            modalElement = document.getElementById('deleteConfirmationModal');
            messageElement = document.getElementById('deleteConfirmationMessage');
            return;
        }

        var modalHtml = `
            <div class="modal fade" id="deleteConfirmationModal" tabindex="-1">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header bg-danger text-white">
                            <h5 class="modal-title">
                                <i class="bi bi-exclamation-triangle"></i> Silme Onayı
                            </h5>
                            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <p id="deleteConfirmationMessage">Bu kaydı silmek istediğinizden emin misiniz?</p>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                                <i class="bi bi-x-circle"></i> İptal
                            </button>
                            <button type="button" class="btn btn-danger" id="confirmDeleteBtn">
                                <i class="bi bi-trash"></i> Evet, Sil
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHtml);
        modalElement = document.getElementById('deleteConfirmationModal');
        messageElement = document.getElementById('deleteConfirmationMessage');

        // Attach confirm button event
        document.getElementById('confirmDeleteBtn').addEventListener('click', function() {
            if (currentCallback) {
                currentCallback();
            }
            var modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) modal.hide();
        });
    }

    function show(message, onConfirm) {
        if (!modalElement) {
            initialize();
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
