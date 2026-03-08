/**
 * Client-side Localization System
 * Provides translation functions for JavaScript files
 *
 * Usage:
 * 1. Define TRANSLATION_KEYS array with all keys used in your module
 * 2. Call Localization.loadKeys(TRANSLATION_KEYS) before initializing ViewModel
 * 3. Use T('Key.Name', 'Fallback Value') in your code
 */
(function(window) {
    'use strict';

    // Current translations (loaded from API)
    var translations = {};

    /**
     * Get translation for a key
     * @param {string} key - Translation key (e.g., 'Common.Save')
     * @param {string} [defaultValue] - Default value if key not found
     * @param {...*} [args] - Arguments for string formatting
     * @returns {string} - Translated string
     */
    function T(key, defaultValue) {
        var value = translations[key] || defaultValue || key;

        // Handle string formatting with arguments
        if (arguments.length > 2) {
            var args = Array.prototype.slice.call(arguments, 2);
            for (var i = 0; i < args.length; i++) {
                value = value.replace('{' + i + '}', args[i]);
            }
        }

        return value;
    }

    /**
     * Set translations (merge with existing)
     * @param {Object} newTranslations - Object with translation key-value pairs
     */
    function setTranslations(newTranslations) {
        if (newTranslations && typeof newTranslations === 'object') {
            translations = Object.assign({}, translations, newTranslations);
        }
    }

    /**
     * Load translations from API
     * @param {string} [languageCode] - Language code (e.g., 'tr', 'en')
     * @returns {Promise}
     */
    function loadTranslations(languageCode) {
        var url = '/api/localization/resources/current';
        if (languageCode) {
            url = '/api/localization/resources/code/' + languageCode;
        }

        return fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) {
                    console.warn('Failed to load translations');
                    return {};
                }
                return response.json();
            })
            .then(function(data) {
                if (data && typeof data === 'object') {
                    setTranslations(data);
                }
                return translations;
            })
            .catch(function(error) {
                console.warn('Error loading translations:', error);
                return translations;
            });
    }

    /**
     * Get current language code
     * @returns {string}
     */
    function getCurrentLanguage() {
        // Try to get from cookie
        var match = document.cookie.match(/Language=([^;]+)/);
        return match ? match[1] : 'tr';
    }

    /**
     * Load specific translation keys from API (batch request)
     * @param {string[]} keys - Array of translation keys to load
     * @returns {Promise}
     */
    function loadKeys(keys) {
        if (!keys || keys.length === 0) {
            return Promise.resolve(translations);
        }

        return fetch('/api/localization/resources/batch', {
            method: 'POST',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ keys: keys })
        })
            .then(function(response) {
                if (!response.ok) {
                    console.warn('Failed to load translation keys');
                    return {};
                }
                return response.json();
            })
            .then(function(data) {
                if (data && typeof data === 'object') {
                    setTranslations(data);
                }
                return translations;
            })
            .catch(function(error) {
                console.warn('Error loading translation keys:', error);
                return translations;
            });
    }

    /**
     * Format a Date object as YYYY-MM-DD using local timezone (not UTC).
     * Replaces .toISOString().split('T')[0] which causes off-by-one errors in UTC+ timezones.
     */
    function formatLocalDate(date) {
        if (!date || !(date instanceof Date)) return '';
        return date.getFullYear() + '-' + String(date.getMonth() + 1).padStart(2, '0') + '-' + String(date.getDate()).padStart(2, '0');
    }

    // Expose to global scope
    window.T = T;
    window.formatLocalDate = formatLocalDate;
    window.Localization = {
        T: T,
        setTranslations: setTranslations,
        loadTranslations: loadTranslations,
        loadKeys: loadKeys,
        getCurrentLanguage: getCurrentLanguage,
        translations: translations
    };

})(window);
