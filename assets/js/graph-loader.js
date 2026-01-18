// Graph Loader - Updates graph image with auth and parameters
(function() {
    'use strict';
    
    function updateGraph() {
        // Get current selections
        const year = window.YearMonthPicker ? window.YearMonthPicker.getSelectedYear() : new Date().getFullYear();
        const hourTypes = window.HourTypePicker ? window.HourTypePicker.getSelection() : [];
        const hourType = hourTypes.length > 0 ? hourTypes.join(', ') : 'None';
        
        // Get auth token
        const token = window.SimpleAuth ? window.SimpleAuth.token : null;
        
        if (!token) {
            console.log('User not authenticated, cannot load graph');
            return;
        }
        
        // Build URL with parameters
        const url = `/api/retrieve/graph?year=${encodeURIComponent(year)}&hourType=${encodeURIComponent(hourType)}&token=${encodeURIComponent(token)}`;
        
        // Update image src
        const img = document.querySelector('#content img[src*="retrieve/graph"]');
        if (img) {
            // Add error handler for 401/403 responses
            img.onerror = function() {
                console.log('Graph image failed to load - likely 401 Unauthorized');
                if (window.SimpleAuth) {
                    SimpleAuth.handleUnauthorized();
                }
            };
            
            img.src = url + '&_=' + new Date().getTime(); // Add timestamp to prevent caching
            console.log(`Graph updated for user - Year: ${year}, Hour Type: ${hourType}`);
        }
    }
    
    // Initialize on document ready
    $(document).ready(function() {
        // Listen for year or hour type changes
        $(document).on('yearSelectionChanged', function() {
            setTimeout(updateGraph, 100); // Small delay to ensure other components are updated
        });
        
        // Initial load after login
        setTimeout(function() {
            if (window.SimpleAuth && window.SimpleAuth.isAuthenticated) {
                updateGraph();
            }
        }, 500);
    });
    
    // Public API
    window.GraphLoader = {
        update: updateGraph
    };
    
})();
