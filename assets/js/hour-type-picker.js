// Hour Type Picker - Simple Checkbox Selection
(function() {
    'use strict';
    
    const hourTypes = [
        'Mechanical hours',
        'Software hours',
        'Visit hours'
    ];
    
    // Store selection state
    const selectionState = {};
    
    // Initialize on document ready
    $(document).ready(function() {
        initializeHourTypePicker();
    });
    
    function initializeHourTypePicker() {
        // Initialize selection state
        hourTypes.forEach(type => {
            selectionState[type] = false;
        });
        
        renderHourTypes();
        setupToggle();
    }
    
    function setupToggle() {
        $('#hour-type-toggle').on('click', function() {
            const arrow = $(this).find('.picker-arrow');
            const content = $('#hour-type-content');
            
            arrow.toggleClass('collapsed');
            content.toggleClass('collapsed');
        });
    }
    
    function renderHourTypes() {
        const content = $('#hour-type-content');
        content.empty();
        
        hourTypes.forEach(type => {
            const typeItem = createHourTypeItem(type);
            content.append(typeItem);
        });
    }
    
    function createHourTypeItem(type) {
        const itemDiv = $('<div>').addClass('hour-type-item');
        
        const checkbox = $('<input>')
            .attr('type', 'checkbox')
            .addClass('hour-type-checkbox')
            .attr('data-type', type)
            .on('change', function() {
                onHourTypeCheckboxChange(type, $(this).is(':checked'));
            });
        
        const label = $('<span>')
            .addClass('hour-type-label')
            .text(type)
            .on('click', function(e) {
                e.stopPropagation();
                checkbox.prop('checked', !checkbox.is(':checked')).trigger('change');
            });
        
        itemDiv.append(checkbox, label);
        return itemDiv;
    }
    
    function onHourTypeCheckboxChange(type, checked) {
        selectionState[type] = checked;
        logSelection();
    }
    
    function logSelection() {
        const selected = getSelectedHourTypes();
        console.log('Selected hour types:', selected);
        
        // Trigger graph update
        if (window.GraphLoader && typeof window.GraphLoader.update === 'function') {
            window.GraphLoader.update();
        }
    }
    
    function getSelectedHourTypes() {
        const result = [];
        
        Object.keys(selectionState).forEach(type => {
            if (selectionState[type]) {
                result.push(type);
            }
        });
        
        return result;
    }
    
    // Public API
    window.HourTypePicker = {
        getSelection: function() {
            return getSelectedHourTypes();
        },
        
        clearAll: function() {
            Object.keys(selectionState).forEach(type => {
                selectionState[type] = false;
            });
            
            $('.hour-type-checkbox').prop('checked', false);
        },
        
        selectType: function(type) {
            if (selectionState[type] !== undefined) {
                const checkbox = $(`.hour-type-checkbox[data-type="${type}"]`);
                checkbox.prop('checked', true).trigger('change');
            }
        },
        
        deselectType: function(type) {
            if (selectionState[type] !== undefined) {
                const checkbox = $(`.hour-type-checkbox[data-type="${type}"]`);
                checkbox.prop('checked', false).trigger('change');
            }
        },
        
        selectAll: function() {
            hourTypes.forEach(type => {
                selectionState[type] = true;
                $(`.hour-type-checkbox[data-type="${type}"]`).prop('checked', true);
            });
            logSelection();
        }
    };
    
})();
