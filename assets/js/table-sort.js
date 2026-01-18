(function() {
    'use strict';

    // Track current sort state
    let currentSortColumn = null;
    let currentSortDirection = 'asc';

    // Function to initialize sorting on a table (called after table is loaded)
    window.initTableSorting = function() {
        const table = document.getElementById('worklogTable');
        if (!table) return;

        // Remove old event listeners by cloning and replacing headers
        const headers = table.querySelectorAll('th.sortable');
        headers.forEach(function(header) {
            const newHeader = header.cloneNode(true);
            header.parentNode.replaceChild(newHeader, header);
            
            newHeader.addEventListener('click', function() {
                const column = this.getAttribute('data-column');
                const dataType = this.getAttribute('data-type') || 'string';
                sortTable(column, dataType);
            });
        });
    };

    function sortTable(column, dataType) {
        const table = document.getElementById('worklogTable');
        if (!table) return;

        const tbody = table.querySelector('tbody');
        const rows = Array.from(tbody.querySelectorAll('tr'));
        if (rows.length === 0) return;

        // Separate the total row (last row) from data rows
        const totalRow = rows[rows.length - 1];
        const dataRows = rows.slice(0, -1);

        // Determine sort direction
        if (currentSortColumn === column) {
            currentSortDirection = currentSortDirection === 'asc' ? 'desc' : 'asc';
        } else {
            currentSortDirection = 'asc';
        }
        
        currentSortColumn = column;

        // Get column index based on data-column attribute
        const headers = Array.from(table.querySelectorAll('th.sortable'));
        const columnIndex = headers.findIndex(h => h.getAttribute('data-column') === column);
        if (columnIndex === -1) return;

        // Sort the data rows
        dataRows.sort(function(rowA, rowB) {
            const cellA = rowA.children[columnIndex];
            const cellB = rowB.children[columnIndex];
            if (!cellA || !cellB) return 0;

            let valueA = cellA.textContent.trim();
            let valueB = cellB.textContent.trim();
            let comparison = 0;

            if (dataType === 'number') {
                comparison = parseFloat(valueA) - parseFloat(valueB);
            } else if (column === 'date') {
                comparison = new Date(valueA) - new Date(valueB);
            } else {
                comparison = valueA.localeCompare(valueB, undefined, { sensitivity: 'base' });
            }

            return currentSortDirection === 'asc' ? comparison : -comparison;
        });

        // Clear tbody and re-append sorted rows
        tbody.innerHTML = '';
        dataRows.forEach(function(row) {
            tbody.appendChild(row);
        });

        // Re-append the total row at the end
        tbody.appendChild(totalRow);

        // Update sort indicators
        updateSortIndicators(column);
    }

    function updateSortIndicators(column) {
        const headers = document.querySelectorAll('th.sortable');
        
        headers.forEach(function(header) {
            const arrow = header.querySelector('.sort-arrow');
            const headerColumn = header.getAttribute('data-column');
            
            if (headerColumn === column) {
                header.classList.add('active');
                arrow.textContent = currentSortDirection === 'asc' ? ' ▲' : ' ▼';
                arrow.style.opacity = '1';
            } else {
                header.classList.remove('active');
                arrow.textContent = '';
                arrow.style.opacity = '0';
            }
        });
    }

    // Listen for table updates to reinitialize sorting
    $(document).on('worklogTableLoaded', function() {
        setTimeout(function() {
            initTableSorting();
        }, 100);
    });

})();
