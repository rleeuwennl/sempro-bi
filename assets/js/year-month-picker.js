// Year Month Picker - Hierarchical Tree Structure
(function() {
    'use strict';
    
    const months = [
        'January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'
    ];
    
    // Store selection state
    const selectionState = {};
    
    // Initialize on document ready
    $(document).ready(function() {
        initializePicker();
    });
    
    function initializePicker() {
        const currentYear = new Date().getFullYear();
        const startYear = currentYear - 5;
        const endYear = currentYear + 5;
        
        // Initialize selection state
        for (let year = startYear; year <= endYear; year++) {
            selectionState[year] = {
                selected: false,
                months: {}
            };
            months.forEach((month, index) => {
                selectionState[year].months[index] = false;
            });
        }
        
        renderYears(startYear, endYear);
        setupMainToggle();
    }
    
    function setupMainToggle() {
        $('#picker-toggle').on('click', function() {
            const arrow = $(this).find('.picker-arrow');
            const content = $('#picker-content');
            
            arrow.toggleClass('collapsed');
            content.toggleClass('collapsed');
        });
    }
    
    function renderYears(startYear, endYear) {
        const content = $('#picker-content');
        content.empty();
        
        for (let year = startYear; year <= endYear; year++) {
            const yearItem = createYearItem(year);
            content.append(yearItem);
        }
    }
    
    function createYearItem(year) {
        const yearDiv = $('<div>').addClass('year-item').attr('data-year', year);
        
        // Year header
        const yearHeader = $('<div>').addClass('year-header');
        
        const toggle = $('<span>')
            .addClass('year-toggle')
            .html('▼')
            .on('click', function(e) {
                e.stopPropagation();
                toggleYear(year);
            });
        
        const checkbox = $('<input>')
            .attr('type', 'checkbox')
            .addClass('year-checkbox')
            .attr('data-year', year)
            .on('change', function(e) {
                e.stopPropagation();
                onYearCheckboxChange(year, $(this).is(':checked'));
            });
        
        const label = $('<span>')
            .addClass('year-label')
            .text(`${year} (Year)`)
            .on('click', function(e) {
                e.stopPropagation();
                checkbox.prop('checked', !checkbox.is(':checked')).trigger('change');
            });
        
        yearHeader.append(toggle, checkbox, label);
        
        // Month list (initially collapsed)
        const monthList = createMonthList(year);
        monthList.addClass('collapsed');
        
        yearDiv.append(yearHeader, monthList);
        return yearDiv;
    }
    
    function createMonthList(year) {
        const monthList = $('<div>')
            .addClass('month-list')
            .attr('data-year', year);
        
        months.forEach((month, index) => {
            const monthItem = $('<div>').addClass('month-item');
            
            const checkbox = $('<input>')
                .attr('type', 'checkbox')
                .addClass('month-checkbox')
                .attr('data-year', year)
                .attr('data-month', index)
                .on('change', function() {
                    onMonthCheckboxChange(year, index, $(this).is(':checked'));
                });
            
            const label = $('<span>')
                .addClass('month-label')
                .text(month)
                .on('click', function(e) {
                    e.stopPropagation();
                    checkbox.prop('checked', !checkbox.is(':checked')).trigger('change');
                });
            
            monthItem.append(checkbox, label);
            monthList.append(monthItem);
        });
        
        return monthList;
    }
    
    function toggleYear(year) {
        const yearItem = $(`.year-item[data-year="${year}"]`);
        const toggle = yearItem.find('.year-toggle');
        const monthList = yearItem.find('.month-list');
        
        toggle.toggleClass('collapsed');
        monthList.toggleClass('collapsed');
    }
    
    function onYearCheckboxChange(year, checked) {
        selectionState[year].selected = checked;
        
        // Update all months for this year
        months.forEach((month, index) => {
            selectionState[year].months[index] = checked;
        });
        
        // Update month checkboxes UI
        $(`.month-checkbox[data-year="${year}"]`).prop('checked', checked);
        
        logSelection();
    }
    
    function onMonthCheckboxChange(year, monthIndex, checked) {
        selectionState[year].months[monthIndex] = checked;
        
        // Update year checkbox state
        updateYearCheckboxState(year);
        
        logSelection();
    }
    
    function updateYearCheckboxState(year) {
        const yearCheckbox = $(`.year-checkbox[data-year="${year}"]`);
        const monthStates = Object.values(selectionState[year].months);
        const checkedCount = monthStates.filter(state => state).length;
        
        if (checkedCount === 0) {
            // No months selected
            yearCheckbox.prop('checked', false);
            yearCheckbox.prop('indeterminate', false);
            yearCheckbox.removeClass('indeterminate');
            selectionState[year].selected = false;
        } else if (checkedCount === months.length) {
            // All months selected
            yearCheckbox.prop('checked', true);
            yearCheckbox.prop('indeterminate', false);
            yearCheckbox.removeClass('indeterminate');
            selectionState[year].selected = true;
        } else {
            // Some months selected (indeterminate state)
            yearCheckbox.prop('checked', false);
            yearCheckbox.prop('indeterminate', true);
            yearCheckbox.addClass('indeterminate');
            selectionState[year].selected = false;
        }
    }
    
    function logSelection() {
        const selected = getSelectedItems();
        console.log('Selected items:', selected);
        updateFeedbackDisplay(selected);
    }
    
    function updateFeedbackDisplay(selected) {
        const feedbackContent = $('#feedback-content');
        
        if (selected.yearMonths.length === 0) {
            feedbackContent.html('<em>No selections yet</em>');
            return;
        }
        
        let html = '';
        
        selected.yearMonths.forEach(item => {
            const yearDiv = $('<div>').addClass('feedback-year');
            const titleDiv = $('<div>').addClass('feedback-year-title').text(item.year);
            
            if (item.months.length === months.length) {
                const fullYear = $('<div>')
                    .addClass('feedback-year-full')
                    .text('✓ Full year selected (all 12 months)');
                yearDiv.append(titleDiv, fullYear);
            } else {
                const monthNames = item.months
                    .sort((a, b) => a.index - b.index)
                    .map(m => m.name)
                    .join(', ');
                const monthsDiv = $('<div>')
                    .addClass('feedback-months')
                    .text(`Months: ${monthNames}`);
                yearDiv.append(titleDiv, monthsDiv);
            }
            
            feedbackContent.append(yearDiv);
        });
    }
    
    function getSelectedItems() {
        const result = {
            years: [],
            yearMonths: []
        };
        
        Object.keys(selectionState).forEach(year => {
            const yearData = selectionState[year];
            const selectedMonths = [];
            
            Object.keys(yearData.months).forEach(monthIndex => {
                if (yearData.months[monthIndex]) {
                    selectedMonths.push({
                        index: parseInt(monthIndex),
                        name: months[monthIndex]
                    });
                }
            });
            
            if (selectedMonths.length > 0) {
                result.yearMonths.push({
                    year: parseInt(year),
                    months: selectedMonths
                });
                
                if (selectedMonths.length === months.length) {
                    result.years.push(parseInt(year));
                }
            }
        });
        
        return result;
    }
    
    // Public API
    window.YearMonthPicker = {
        getSelection: function() {
            return getSelectedItems();
        },
        
        clearAll: function() {
            Object.keys(selectionState).forEach(year => {
                selectionState[year].selected = false;
                Object.keys(selectionState[year].months).forEach(monthIndex => {
                    selectionState[year].months[monthIndex] = false;
                });
            });
            
            $('.year-checkbox, .month-checkbox').prop('checked', false);
            $('.year-checkbox').prop('indeterminate', false).removeClass('indeterminate');
            $('#feedback-content').html('<em>No selections yet</em>');
        },
        
        selectYear: function(year) {
            if (selectionState[year]) {
                const checkbox = $(`.year-checkbox[data-year="${year}"]`);
                checkbox.prop('checked', true).trigger('change');
            }
        },
        
        selectMonth: function(year, monthIndex) {
            if (selectionState[year] && selectionState[year].months[monthIndex] !== undefined) {
                const checkbox = $(`.month-checkbox[data-year="${year}"][data-month="${monthIndex}"]`);
                checkbox.prop('checked', true).trigger('change');
            }
        },
        
        expandYear: function(year) {
            const yearItem = $(`.year-item[data-year="${year}"]`);
            const toggle = yearItem.find('.year-toggle');
            const monthList = yearItem.find('.month-list');
            
            if (monthList.hasClass('collapsed')) {
                toggle.removeClass('collapsed');
                monthList.removeClass('collapsed');
            }
        },
        
        collapseYear: function(year) {
            const yearItem = $(`.year-item[data-year="${year}"]`);
            const toggle = yearItem.find('.year-toggle');
            const monthList = yearItem.find('.month-list');
            
            if (!monthList.hasClass('collapsed')) {
                toggle.addClass('collapsed');
                monthList.addClass('collapsed');
            }
        }
    };
    
})();
