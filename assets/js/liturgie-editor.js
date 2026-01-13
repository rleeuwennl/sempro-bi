// Liturgie Editor Module - Edit YouTube links and upload PDFs for authorized users
(function($) {
    'use strict';

    var LiturgieEditor = {
        currentFile: null,

        init: function() {
            // Listen for content changes to detect liturgie pages
            var observer = new MutationObserver(function() {
                LiturgieEditor.checkForLiturgiePage();
            });
            
            observer.observe(document.getElementById('content'), {
                childList: true,
                subtree: true
            });

            // Check on initial load
            setTimeout(function() {
                LiturgieEditor.checkForLiturgiePage();
            }, 500);
        },

        checkForLiturgiePage: function() {
            // Get current page from URL
            var path = window.location.pathname;
            var match = path.match(/litturgie_[^\/]+\.html/);
            
            if (match) {
                this.currentFile = match[0].replace('html/', '');
                if (SimpleAuth && SimpleAuth.isAuthenticated) {
                    this.showEditor();
                }
            } else {
                this.hideEditor();
            }
        },

        showEditor: function() {
            if ($('#liturgie-editor').length) return; // Already shown

            var editorHtml = `
                <div id="liturgie-editor" class="liturgie-editor">
                    <div class="liturgie-editor-header">
                        <h4>⚙️ Liturgie Bewerken</h4>
                        <button class="liturgie-toggle" onclick="LiturgieEditor.toggleEditor()">−</button>
                    </div>
                    <div class="liturgie-editor-body">
                        <div class="liturgie-editor-section">
                            <label>YouTube Insluit Code (HTML iframe):</label>
                            <textarea id="youtube-insluit" placeholder="Plak hier de volledige iframe code..." style="width: 100%; height: 120px; padding: 8px; border: 2px solid #e0e0e0; border-radius: 4px; font-family: 'Courier New', monospace; font-size: 12px; box-sizing: border-box;"></textarea>
                            <button onclick="LiturgieEditor.updateInsluit()" class="liturgie-btn">Update YouTube Code</button>
                        </div>
                        <div class="liturgie-editor-section">
                            <label>Upload Liturgie PDF:</label>
                            <div style="display: flex; align-items: center; gap: 10px;">
                                <label for="pdf-upload" class="liturgie-file-btn">Choose PDF File</label>
                                <input type="file" id="pdf-upload" accept=".pdf" onchange="LiturgieEditor.onPdfFileSelected()" style="display: none;" />
                                <span id="pdf-label" style="color: #666; font-size: 13px;"></span>
                            </div>
                            <div id="pdf-button-container" style="display: flex; gap: 10px; display: none;">
                                <button onclick="LiturgieEditor.removePDF()" class="liturgie-btn" style="background-color: #dc3545;">Remove PDF</button>
                            </div>
                        </div>
                        <div id="liturgie-status" class="liturgie-status"></div>
                    </div>
                </div>
            `;
            
            $('#content').prepend(editorHtml);
            this.loadCurrentValues();
        },

        hideEditor: function() {
            $('#liturgie-editor').remove();
        },

        toggleEditor: function() {
            var $body = $('.liturgie-editor-body');
            var $toggle = $('.liturgie-toggle');
            
            if ($body.is(':visible')) {
                $body.slideUp(200);
                $toggle.text('+');
            } else {
                $body.slideDown(200);
                $toggle.text('−');
            }
        },

        loadCurrentValues: function() {
            // Extract filename from current URL or content
            var path = window.location.pathname;
            var match = path.match(/litturgie_[^\/]+\.html/);
            
            if (match) {
                var htmlFile = match[0];
                var jsonFile = htmlFile.replace('.html', '.json');
                
                // Fetch JSON data
                $.ajax({
                    url: '/liturgie/json/' + jsonFile,
                    dataType: 'json'
                })
                .done(function(data) {
                    if (data.youtubeInsluit) {
                        $('#youtube-insluit').val(data.youtubeInsluit);
                    }
                    // Display current PDF filename if it exists
                    if (data.pdfFile && data.pdfFile.trim()) {
                        var pdfName = data.pdfFile.split('/').pop(); // Get just the filename
                        $('#pdf-label').text('Huidge bestand: ' + pdfName);
                        // Show the Remove PDF button if PDF exists
                        $('#pdf-button-container').show();
                    } else {
                        // Clear the label and hide button if no PDF exists
                        $('#pdf-label').text('');
                        $('#pdf-button-container').hide();
                    }
                })
                .fail(function() {
                    console.log('Could not load JSON data');
                });
            }
        },

        updatePdfLabel: function() {
            var fileInput = document.getElementById('pdf-upload');
            var file = fileInput.files[0];
            var $label = $('#pdf-label');
            
            if (file) {
                $label.text('Nieuw bestand: ' + file.name);
            } else {
                // Reload the original PDF name
                this.loadCurrentValues();
            }
        },

        onPdfFileSelected: function() {
            var fileInput = document.getElementById('pdf-upload');
            var file = fileInput.files[0];
            
            if (file) {
                // Show the selected filename
                $('#pdf-label').text('Nieuw bestand: ' + file.name);
                // Automatically upload the PDF
                this.uploadPDF();
            }
        },

        updateInsluit: function() {
            var youtubeInsluit = $('#youtube-insluit').val().trim();
            
            // Allow empty value to delete the insluit code
            // Only validate if not empty
            if (youtubeInsluit && !youtubeInsluit.toLowerCase().includes('iframe')) {
                this.showStatus('Dit ziet er niet uit als een iframe code', 'error');
                return;
            }

            // Show appropriate message based on whether we're adding or deleting
            var statusMsg = youtubeInsluit ? 'Insluit code wordt bijgewerkt...' : 'Insluit code wordt verwijderd...';
            this.showStatus(statusMsg, 'info');

            $.ajax({
                url: '/api/liturgie/update-insluit',
                method: 'POST',
                headers: SimpleAuth.getAuthHeader(),
                contentType: 'application/json',
                data: JSON.stringify({
                    filename: this.currentFile,
                    youtubeInsluit: youtubeInsluit
                })
            })
            .done(function(data) {
                if (data.success) {
                    var successMsg = youtubeInsluit ? '✓ Insluit code bijgewerkt!' : '✓ Insluit code verwijderd!';
                    LiturgieEditor.showStatus(successMsg, 'success');
                    
                    // If deleting insluit code, refresh entire page
                    if (!youtubeInsluit) {
                        setTimeout(function() {
                            location.reload();
                        }, 1500);
                    } else {
                        // If updating, just reload the data
                        setTimeout(function() {
                            if (window.LiturgieDataLoader) {
                                LiturgieDataLoader.loadData(LiturgieEditor.currentFile);
                            }
                            LiturgieEditor.loadCurrentValues();
                        }, 1000);
                    }
                }
            })
            .fail(function() {
                LiturgieEditor.showStatus('Fout bij bijwerken insluit code', 'error');
            });
        },

        removeYoutubeInsluit: function() {
            if (!confirm('Weet je zeker dat je de YouTube insluit code wilt verwijderen?')) {
                return;
            }
            
            // Clear the textarea
            $('#youtube-insluit').val('');
            
            // Call updateInsluit to save the empty value
            this.updateInsluit();
        },

        uploadPDF: function() {
            var fileInput = document.getElementById('pdf-upload');
            var file = fileInput.files[0];

            if (!file) {
                this.showStatus('Selecteer een PDF bestand', 'error');
                return;
            }

            if (!file.name.endsWith('.pdf')) {
                this.showStatus('Alleen PDF bestanden zijn toegestaan', 'error');
                return;
            }

            this.showStatus('PDF wordt geüpload...', 'info');

            var formData = new FormData();
            formData.append('filename', this.currentFile);
            formData.append('pdfFile', file);

            $.ajax({
                url: '/api/liturgie/upload-pdf',
                method: 'POST',
                headers: SimpleAuth.getAuthHeader(),
                data: formData,
                processData: false,
                contentType: false
            })
            .done(function(data) {
                if (data.success) {
                    LiturgieEditor.showStatus('✓ PDF geüpload: ' + data.pdfFilename, 'success');
                    // Reload data from JSON and update iframe
                    setTimeout(function() {
                        if (window.LiturgieDataLoader) {
                            LiturgieDataLoader.loadData(LiturgieEditor.currentFile);
                        }
                        document.getElementById('pdf-upload').value = '';
                        LiturgieEditor.loadCurrentValues(); // Reload to show the new PDF filename
                    }, 1000);
                }
            })
            .fail(function() {
                LiturgieEditor.showStatus('Fout bij uploaden PDF', 'error');
            });
        },

        removePDF: function() {
            if (!confirm('Weet je zeker dat je het PDF wilt verwijderen?')) {
                return;
            }

            this.showStatus('PDF wordt verwijderd...', 'info');

            $.ajax({
                url: '/api/liturgie/remove-pdf',
                method: 'POST',
                headers: SimpleAuth.getAuthHeader(),
                contentType: 'application/json',
                data: JSON.stringify({
                    filename: this.currentFile
                })
            })
            .done(function(data) {
                if (data.success) {
                    LiturgieEditor.showStatus('✓ PDF verwijderd - pagina wordt ververst...', 'success');
                    
                    // Refresh the entire page after a short delay
                    setTimeout(function() {
                        location.reload();
                    }, 1500);
                }
            })
            .fail(function() {
                LiturgieEditor.showStatus('Fout bij verwijderen PDF', 'error');
            });
        },

        showStatus: function(message, type) {
            var $status = $('#liturgie-status');
            $status.removeClass('liturgie-status-success liturgie-status-error liturgie-status-info');
            $status.addClass('liturgie-status-' + type);
            $status.text(message).fadeIn(200);

            if (type === 'success') {
                setTimeout(function() {
                    $status.fadeOut(400);
                }, 3000);
            }
        }
    };

    $(document).ready(function() {
        // Initialize when SimpleAuth is ready
        var checkAuth = setInterval(function() {
            if (window.SimpleAuth) {
                clearInterval(checkAuth);
                LiturgieEditor.init();
            }
        }, 100);
    });

    window.LiturgieEditor = LiturgieEditor;

})(jQuery);
