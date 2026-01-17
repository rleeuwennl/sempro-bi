// Year Picker - Simple Year Selection
(function() {
    'use strict';
    
    let selectedYear = null;
    
    // Initialize on document ready
    $(document).ready(function() {
        initializePicker();
    });
    
    function initializePicker() {
        const currentYear = new Date().getFullYear();
        const startYear = currentYear - 5;
        const endYear = currentYear + 5;
        
        selectedYear = 2025; // Default to 2025
        
        renderYearSelect(startYear, endYear);
        setupYearSelect();
    }
    
    function renderYearSelect(startYear, endYear) {
        const select = $('#year-select');
        select.empty();
        
        for (let year = endYear; year >= startYear; year--) {
            const option = $('<option>')
                .val(year)
                .text(year);
            
            if (year === selectedYear) {
                option.prop('selected', true);
            }
            
            select.append(option);
        }
    }
    
    function setupYearSelect() {
        $('#year-select').on('change', function() {
            selectedYear = parseInt($(this).val());
            triggerSelectionChange();
        });
        
        // Trigger initial load
        triggerSelectionChange();
    }
    
    function triggerSelectionChange() {
        console.log('Selected year:', selectedYear);
        
        // Trigger custom event for other components to listen to
        $(document).trigger('yearSelectionChanged', [selectedYear]);
        
        // If liturgie-loader exists, trigger a reload
        if (window.LiturgieLoader && typeof window.LiturgieLoader.loadData === 'function') {
            window.LiturgieLoader.loadData();
        }
    }
    
    // Public API
    window.YearMonthPicker = {
        getSelection: function() {
            return {
                year: selectedYear,
                years: [selectedYear]
            };
        },
        
        getSelectedYear: function() {
            return selectedYear;
        },
        
        setYear: function(year) {
            selectedYear = year;
            $('#year-select').val(year);
            triggerSelectionChange();
        }
    };
    
})();
