// Liturgie Data Loader - Load liturgie data from JSON files
(function() {
    'use strict';

    window.LiturgieDataLoader = {
        loadData: function(htmlFilename) {
            var jsonFilename = htmlFilename.replace('.html', '.json');
            var jsonUrl = '/liturgie/json/' + jsonFilename;

            $.ajax({
                url: jsonUrl,
                dataType: 'json',
                async: false // Need synchronous for immediate data availability
            })
            .done(function(data) {
                // Override liturgieData with loaded JSON data
                window.liturgieData = data;
                LiturgieDataLoader.applyData();
            })
            .fail(function() {
                console.log('Could not load liturgie data from: ' + jsonUrl);
                // Use existing liturgieData if JSON load fails
                LiturgieDataLoader.applyData();
            });
        },

        applyData: function() {
            if (!window.liturgieData) return;

            // Derive showLiveButton: true if youtubeInsluit is empty
            var showLiveButton = !window.liturgieData.youtubeInsluit || window.liturgieData.youtubeInsluit.trim() === "";

            // Apply YouTube insluit code if present
            var $youtubeFrame = document.getElementById('youtube-frame');
            if ($youtubeFrame && window.liturgieData.youtubeInsluit && window.liturgieData.youtubeInsluit.trim() !== "") {
                // Create temporary container to extract iframe src from HTML
                var tempDiv = document.createElement('div');
                tempDiv.innerHTML = window.liturgieData.youtubeInsluit;
                var iframe = tempDiv.querySelector('iframe');
                if (iframe) {
                    $youtubeFrame.src = iframe.getAttribute('src');
                    $youtubeFrame.style.display = 'block';
                }
            }

            // Apply PDF data
            var $pdfFrame = document.getElementById('pdf-frame');
            if ($pdfFrame && window.liturgieData.pdfFile) {
                $pdfFrame.src = window.liturgieData.pdfFile;
                var $pdfSection = $pdfFrame.closest('#pdf-section');
                if ($pdfSection) {
                    $pdfSection.style.display = 'block';
                }
            }

            // Apply live button data (show if youtubeInsluit is empty)
            if (showLiveButton) {
                var $liveSection = document.getElementById('live-section');
                if ($liveSection) {
                    $liveSection.style.display = 'block';
                    var $liveLink = document.getElementById('live-link');
                    if ($liveLink) {
                        // For live button, use a standard live link
                        $liveLink.href = "https://www.youtube.com/@kerkangerlo-doesburg4481/live";
                    }
                }
            }
        }
    };

    // Auto-load on document ready if jQuery is available
    if (typeof jQuery !== 'undefined') {
        $(document).ready(function() {
            var path = window.location.pathname;
            var match = path.match(/litturgie_[^\/]+\.html/);
            if (match) {
                LiturgieDataLoader.loadData(match[0]);
            }
        });
    }
})();
