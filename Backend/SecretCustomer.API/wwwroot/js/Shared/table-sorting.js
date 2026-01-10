/**
 * Table Sorting Helper
 * Reusable sorting functionality for KnockoutJS ViewModels
 */
window.TableSorting = (function () {
    'use strict';

    /**
     * Create sorting state observables for a ViewModel
     * @param {string} defaultSortBy - Default column to sort by
     * @param {string} defaultDirection - Default direction ('asc' or 'desc')
     * @returns {Object} - Sorting state object with observables and methods
     */
    function createSortState(defaultSortBy, defaultDirection) {
        var state = {
            sortBy: ko.observable(defaultSortBy || ''),
            sortDirection: ko.observable(defaultDirection || 'asc')
        };

        /**
         * Get CSS class for a column header
         * @param {string} column - Column name
         * @returns {string} - CSS class ('sort-asc', 'sort-desc', or '')
         */
        state.getSortClass = function (column) {
            if (state.sortBy() !== column) return '';
            return 'sort-' + state.sortDirection();
        };

        /**
         * Get Bootstrap icon class for sort indicator
         * @param {string} column - Column name
         * @returns {string} - Icon class
         */
        state.getSortIcon = function (column) {
            if (state.sortBy() !== column) return 'bi-arrow-down-up';
            return state.sortDirection() === 'asc' ? 'bi-sort-up' : 'bi-sort-down';
        };

        /**
         * Toggle sort on header click
         * @param {string} column - Column name to sort by
         */
        state.toggleSort = function (column) {
            if (state.sortBy() === column) {
                // Same column: toggle direction
                state.sortDirection(state.sortDirection() === 'asc' ? 'desc' : 'asc');
            } else {
                // New column: set to asc
                state.sortBy(column);
                state.sortDirection('asc');
            }
        };

        /**
         * Reset sorting to default state
         */
        state.reset = function () {
            state.sortBy(defaultSortBy || '');
            state.sortDirection(defaultDirection || 'asc');
        };

        return state;
    }

    /**
     * Client-side array sorting
     * @param {Array} array - Array to sort
     * @param {string} sortBy - Property name to sort by
     * @param {string} sortDirection - 'asc' or 'desc'
     * @returns {Array} - Sorted array (new array, original unchanged)
     */
    function clientSort(array, sortBy, sortDirection) {
        if (!sortBy || !array || array.length === 0) return array;

        var sorted = array.slice(); // Clone array
        var direction = sortDirection === 'desc' ? -1 : 1;

        sorted.sort(function (a, b) {
            var valA = getNestedValue(a, sortBy);
            var valB = getNestedValue(b, sortBy);

            // Handle null/undefined
            if (valA == null && valB == null) return 0;
            if (valA == null) return 1;
            if (valB == null) return -1;

            // Date comparison
            if (valA instanceof Date && valB instanceof Date) {
                return (valA.getTime() - valB.getTime()) * direction;
            }

            // Try to parse as date strings
            if (typeof valA === 'string' && typeof valB === 'string') {
                var dateA = tryParseDate(valA);
                var dateB = tryParseDate(valB);
                if (dateA && dateB) {
                    return (dateA.getTime() - dateB.getTime()) * direction;
                }
            }

            // Number comparison
            if (typeof valA === 'number' && typeof valB === 'number') {
                return (valA - valB) * direction;
            }

            // Boolean comparison
            if (typeof valA === 'boolean' && typeof valB === 'boolean') {
                return ((valA === valB) ? 0 : valA ? -1 : 1) * direction;
            }

            // String comparison (case-insensitive, Turkish locale)
            return String(valA).localeCompare(String(valB), 'tr', { sensitivity: 'base' }) * direction;
        });

        return sorted;
    }

    /**
     * Get nested property value (e.g., 'user.name' or 'address.city')
     * @param {Object} obj - Source object
     * @param {string} path - Property path
     * @returns {*} - Property value or undefined
     */
    function getNestedValue(obj, path) {
        if (!path) return obj;
        return path.split('.').reduce(function (o, p) {
            return o ? o[p] : undefined;
        }, obj);
    }

    /**
     * Try to parse a string as a date
     * @param {string} str - String to parse
     * @returns {Date|null} - Date object or null
     */
    function tryParseDate(str) {
        if (!str || typeof str !== 'string') return null;
        // ISO date format check
        if (/^\d{4}-\d{2}-\d{2}/.test(str)) {
            var date = new Date(str);
            if (!isNaN(date.getTime())) return date;
        }
        return null;
    }

    /**
     * Build URL params for server-side sorting
     * @param {string} sortBy - Column to sort by
     * @param {string} sortDirection - 'asc' or 'desc'
     * @returns {Object} - Object with sortBy and sortDirection
     */
    function buildSortParams(sortBy, sortDirection) {
        var params = {};
        if (sortBy) {
            params.sortBy = sortBy;
            params.sortDirection = sortDirection || 'asc';
        }
        return params;
    }

    /**
     * Append sort params to URLSearchParams
     * @param {URLSearchParams} urlParams - Existing params
     * @param {string} sortBy - Column to sort by
     * @param {string} sortDirection - 'asc' or 'desc'
     */
    function appendSortParams(urlParams, sortBy, sortDirection) {
        if (sortBy) {
            urlParams.append('sortBy', sortBy);
            urlParams.append('sortDirection', sortDirection || 'asc');
        }
    }

    /**
     * Create a sorted computed array (for client-side sorting)
     * @param {ko.observableArray} sourceArray - Source data array
     * @param {ko.observable} sortByObservable - Observable for sort column
     * @param {ko.observable} sortDirectionObservable - Observable for sort direction
     * @returns {ko.computed} - Computed array that auto-sorts when sort changes
     */
    function createSortedComputed(sourceArray, sortByObservable, sortDirectionObservable) {
        return ko.computed(function () {
            var items = sourceArray();
            var by = sortByObservable();
            var dir = sortDirectionObservable();

            if (!by) return items;
            return clientSort(items, by, dir);
        });
    }

    // Public API
    return {
        createSortState: createSortState,
        clientSort: clientSort,
        buildSortParams: buildSortParams,
        appendSortParams: appendSortParams,
        createSortedComputed: createSortedComputed
    };
})();
