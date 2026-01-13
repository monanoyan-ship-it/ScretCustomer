/**
 * TooltipService - AJAX destekli tooltip/popover servisi
 *
 * Kullanım:
 * <button class="app-tooltip" data-tooltip-key="project-types" data-tooltip-title="Proje Tipleri">
 *     <i class="bi bi-info-circle"></i>
 * </button>
 *
 * Opsiyonel data attribute'lar:
 * - data-tooltip-key: API'den çekilecek tooltip anahtarı (zorunlu)
 * - data-tooltip-title: Popover başlığı (opsiyonel)
 * - data-tooltip-id: Dinamik içerik için ID parametresi (opsiyonel)
 * - data-tooltip-placement: Popover pozisyonu (default: right)
 */

// Popover CSS (dinamik eklenir)
(function() {
    var style = document.createElement('style');
    style.textContent = '.app-tooltip-popover { max-width: 450px !important; } .app-tooltip-popover .popover-body { padding: 0.5rem; }';
    document.head.appendChild(style);
})();
var TooltipService = (function() {
    var cache = {}; // İçerik cache'i
    var activePopover = null; // Aktif popover instance

    /**
     * Tooltip içeriğini API'den çeker
     */
    function fetchContent(key, id) {
        var cacheKey = id ? key + '_' + id : key;

        // Cache'de varsa direkt dön
        if (cache[cacheKey]) {
            return Promise.resolve(cache[cacheKey]);
        }

        var url = '/api/tooltips/' + key;
        if (id) {
            url += '?id=' + id;
        }

        return fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) {
                    throw new Error('Tooltip yüklenemedi');
                }
                return response.text();
            })
            .then(function(html) {
                cache[cacheKey] = html; // Cache'e kaydet
                return html;
            });
    }

    /**
     * Popover'ı gösterir
     */
    function showPopover(element, title, content, placement) {
        // Önceki popover'ı kapat
        hideActivePopover();

        var popover = new bootstrap.Popover(element, {
            title: title || '',
            content: content,
            html: true,
            placement: placement || 'right',
            trigger: 'manual',
            container: 'body',
            sanitize: false,
            customClass: 'app-tooltip-popover'
        });

        popover.show();
        activePopover = { instance: popover, element: element };
    }

    /**
     * Loading popover gösterir
     */
    function showLoading(element, title, placement) {
        var loadingHtml = '<div class="text-center py-2"><span class="spinner-border spinner-border-sm"></span> Yükleniyor...</div>';
        showPopover(element, title, loadingHtml, placement);
    }

    /**
     * Aktif popover'ı kapatır
     */
    function hideActivePopover() {
        if (activePopover) {
            try {
                activePopover.instance.dispose();
            } catch (e) {}
            activePopover = null;
        }
    }

    /**
     * Tooltip butonuna tıklama handler'ı
     */
    function handleClick(e) {
        e.preventDefault();
        e.stopPropagation();

        var btn = e.currentTarget;
        var key = btn.getAttribute('data-tooltip-key');
        var title = btn.getAttribute('data-tooltip-title') || '';
        var id = btn.getAttribute('data-tooltip-id');
        var placement = btn.getAttribute('data-tooltip-placement') || 'right';

        if (!key) {
            console.error('TooltipService: data-tooltip-key attribute gerekli');
            return;
        }

        // Aynı butona tekrar tıklandıysa kapat
        if (activePopover && activePopover.element === btn) {
            hideActivePopover();
            return;
        }

        // Loading göster
        showLoading(btn, title, placement);

        // İçeriği çek ve göster
        fetchContent(key, id)
            .then(function(html) {
                showPopover(btn, title, html, placement);
            })
            .catch(function(error) {
                console.error('TooltipService error:', error);
                showPopover(btn, title, '<div class="text-danger">İçerik yüklenemedi</div>', placement);
            });
    }

    /**
     * Dışarı tıklandığında popover'ı kapat
     */
    function handleOutsideClick(e) {
        if (!activePopover) return;

        var popoverEl = document.querySelector('.app-tooltip-popover');
        var btn = activePopover.element;

        if (popoverEl && !popoverEl.contains(e.target) && !btn.contains(e.target)) {
            hideActivePopover();
        }
    }

    /**
     * Event listener'ları başlat
     */
    function init() {
        // Tooltip butonlarına click handler
        $(document).on('click', '.app-tooltip', handleClick);

        // Dışarı tıklayınca kapat
        $(document).on('click', handleOutsideClick);

        // ESC tuşuyla kapat
        $(document).on('keydown', function(e) {
            if (e.key === 'Escape') {
                hideActivePopover();
            }
        });
    }

    /**
     * Cache'i temizle (gerekirse)
     */
    function clearCache(key) {
        if (key) {
            // Belirli key'i temizle
            Object.keys(cache).forEach(function(k) {
                if (k === key || k.startsWith(key + '_')) {
                    delete cache[k];
                }
            });
        } else {
            // Tüm cache'i temizle
            cache = {};
        }
    }

    // Public API
    return {
        init: init,
        clearCache: clearCache,
        hide: hideActivePopover
    };
})();

// Sayfa yüklendiğinde başlat
$(document).ready(function() {
    TooltipService.init();
});
