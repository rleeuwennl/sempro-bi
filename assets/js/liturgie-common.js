// Common liturgie data loading and application logic
var liturgieData = {youtubeInsluit: "", pdfFile: ""};

function applyLiturgieData() {
    console.log('Applying liturgie data:', liturgieData);
    
    var showLiveButton = !liturgieData.youtubeInsluit || liturgieData.youtubeInsluit.trim() === "";

    // Apply YouTube insluit code if present
    if (liturgieData.youtubeInsluit && liturgieData.youtubeInsluit.trim() !== "") {
        console.log('Found youtubeInsluit, extracting iframe src');
        var tempDiv = document.createElement('div');
        tempDiv.innerHTML = liturgieData.youtubeInsluit;
        var iframe = tempDiv.querySelector('iframe');
        if (iframe) {
            var src = iframe.getAttribute('src');
            console.log('Extracted src:', src);
            var $youtubeFrame = document.getElementById('youtube-frame');
            if ($youtubeFrame) {
                $youtubeFrame.src = src;
                $youtubeFrame.style.display = 'block';
                console.log('YouTube frame displayed');
            }
        }
    }

    // Apply PDF data
    if (liturgieData.pdfFile && liturgieData.pdfFile.trim() !== "") {
        console.log('Found pdfFile:', liturgieData.pdfFile);
        var $pdfFrame = document.getElementById('pdf-frame');
        if ($pdfFrame) {
            $pdfFrame.src = liturgieData.pdfFile;
            var $pdfSection = document.getElementById('pdf-section');
            if ($pdfSection) {
                $pdfSection.style.display = 'block';
                console.log('PDF section displayed');
            }
        }
    }

    // Apply live button (show if youtubeInsluit is empty)
    if (showLiveButton) {
        console.log('Showing live button');
        var $liveSection = document.getElementById('live-section');
        if ($liveSection) {
            $liveSection.style.display = 'block';
            var $liveButton = document.getElementById('live-button');
            if ($liveButton) {
                $liveButton.href = "https://www.youtube.com/@kerkangerlo-doesburg4481/live";
            }
        }
    }
}

// Load and apply data - expects LITURGIE_JSON_FILE to be defined in the HTML file
function loadAndApplyData() {
    if (typeof LITURGIE_JSON_FILE === 'undefined') {
        console.error('LITURGIE_JSON_FILE is not defined');
        return;
    }
    
    var jsonFile = LITURGIE_JSON_FILE + '?t=' + Date.now();
    console.log('Loading from:', jsonFile);
    
    var xhr = new XMLHttpRequest();
    xhr.open('GET', jsonFile, true);
    xhr.onreadystatechange = function() {
        if (xhr.readyState === 4) {
            if (xhr.status === 200) {
                try {
                    liturgieData = JSON.parse(xhr.responseText);
                    console.log('Data loaded successfully:', liturgieData);
                    applyLiturgieData();
                } catch(e) {
                    console.error('Failed to parse JSON:', e);
                }
            } else {
                console.error('Failed to load JSON, status:', xhr.status);
            }
        }
    };
    xhr.send();
}

// Load data immediately and also on DOMContentLoaded
loadAndApplyData();
document.addEventListener('DOMContentLoaded', loadAndApplyData);
