(function () {
    var currentArtikel = null;
    var uploadUrl = 'ArtikelFoto.ashx';

    function $(id) { return document.getElementById(id); }

    function setPanelStatus(msg, type) {
        var el = $('fotoPanelStatus');
        if (!el) return;
        el.textContent = msg || '';
        el.className = 'foto-panel-status' + (type ? ' ' + type : '');
    }

    function isModalOpen() {
        var modal = $('fotoModal');
        return modal && !modal.hidden;
    }

    function openPanel(row) {
        if (!row || !row.ArtikelID) {
            alert('Sla het artikel eerst op voordat je foto\'s toevoegt.');
            return;
        }
        currentArtikel = row;
        $('fotoPanelTitle').textContent = 'Foto\'s - ' + (row.Naam || 'Artikel ' + row.ArtikelID);
        $('fotoModal').hidden = false;
        document.body.classList.add('foto-modal-open');
        loadGallery();
        setPanelStatus('');
        $('fotoDropZone').scrollIntoView({ block: 'nearest' });
    }

    function closePanel() {
        $('fotoModal').hidden = true;
        document.body.classList.remove('foto-modal-open');
        currentArtikel = null;
    }

    function loadGallery() {
        if (!currentArtikel) return;
        var gallery = $('fotoGallery');
        gallery.innerHTML = '<p class="foto-loading">Laden...</p>';

        PageMethods.GetFotoList(currentArtikel.ArtikelID,
            function (fotos) {
                gallery.innerHTML = '';
                if (!fotos || !fotos.length) {
                    gallery.innerHTML = '<p class="foto-empty">Nog geen foto\'s. Upload, plak of sleep hierheen.</p>';
                    return;
                }
                fotos.forEach(function (f) {
                    var item = document.createElement('div');
                    item.className = 'foto-item';
                    item.innerHTML =
                        '<img src="' + uploadUrl + '?id=' + f.FotoID + '" alt="Foto" />' +
                        '<div class="foto-item-meta">' +
                            '<span>' + (f.Tijd || '') + '</span>' +
                            '<button type="button" class="foto-del" data-id="' + f.FotoID + '" title="Verwijderen">&times;</button>' +
                        '</div>';
                    gallery.appendChild(item);
                });
                gallery.querySelectorAll('.foto-del').forEach(function (btn) {
                    btn.addEventListener('click', function () {
                        deleteFoto(parseInt(btn.getAttribute('data-id'), 10));
                    });
                });
            },
            function (err) {
                gallery.innerHTML = '';
                setPanelStatus('Fout bij laden: ' + (err.get_message ? err.get_message() : err), 'err');
            }
        );
    }

    function deleteFoto(fotoId) {
        if (!confirm('Foto verwijderen?')) return;
        PageMethods.DeleteFoto(fotoId,
            function (result) {
                if (result && result.success) {
                    setPanelStatus(result.message, 'ok');
                    loadGallery();
                    if (window.artikelenGrid) window.artikelenGrid.reload();
                } else {
                    setPanelStatus((result && result.message) || 'Verwijderen mislukt', 'err');
                }
            },
            function (err) {
                setPanelStatus('Fout: ' + (err.get_message ? err.get_message() : err), 'err');
            }
        );
    }

    function uploadFiles(fileList) {
        if (!currentArtikel || !fileList || !fileList.length) return;

        var files = [];
        for (var i = 0; i < fileList.length; i++) {
            if (fileList[i].type && fileList[i].type.indexOf('image/') === 0) files.push(fileList[i]);
        }
        if (!files.length) {
            setPanelStatus('Geen afbeeldingen gevonden.', 'err');
            return;
        }

        setPanelStatus('Uploaden...', '');

        var fd = new FormData();
        fd.append('artikelId', currentArtikel.ArtikelID);
        for (var j = 0; j < files.length; j++) fd.append('file', files[j]);

        var xhr = new XMLHttpRequest();
        xhr.open('POST', uploadUrl, true);
        xhr.onload = function () {
            try {
                var result = JSON.parse(xhr.responseText || '{}');
                if (result.success) {
                    setPanelStatus(result.message, 'ok');
                    loadGallery();
                    if (window.artikelenGrid) window.artikelenGrid.reload();
                } else {
                    setPanelStatus(result.message || 'Upload mislukt', 'err');
                }
            } catch (e) {
                setPanelStatus('Upload mislukt', 'err');
            }
        };
        xhr.onerror = function () { setPanelStatus('Upload mislukt', 'err'); };
        xhr.send(fd);
    }

    function handlePaste(e) {
        if (!isModalOpen() || !currentArtikel) return;

        var items = e.clipboardData && e.clipboardData.items;
        if (!items) return;

        var images = [];
        var fileBlobs = [];

        for (var i = 0; i < items.length; i++) {
            if (items[i].type.indexOf('image/') === 0) {
                var blob = items[i].getAsFile();
                if (blob) fileBlobs.push(blob);
            }
        }

        if (fileBlobs.length) {
            e.preventDefault();
            uploadFiles(fileBlobs);
            return;
        }
    }

    function initDropZone() {
        var zone = $('fotoDropZone');
        if (!zone) return;

        zone.addEventListener('dragover', function (e) {
            e.preventDefault();
            zone.classList.add('drag-over');
        });
        zone.addEventListener('dragleave', function () {
            zone.classList.remove('drag-over');
        });
        zone.addEventListener('drop', function (e) {
            e.preventDefault();
            zone.classList.remove('drag-over');
            if (e.dataTransfer && e.dataTransfer.files) uploadFiles(e.dataTransfer.files);
        });

        var input = $('fotoFileInput');
        if (input) {
            input.addEventListener('change', function () {
                uploadFiles(input.files);
                input.value = '';
            });
        }
    }

    window.artikelenFotos = {
        open: openPanel,
        init: function () {
            $('fotoPanelClose').addEventListener('click', closePanel);
            $('fotoModal').addEventListener('click', function (e) {
                if (e.target === $('fotoModal')) closePanel();
            });
            document.addEventListener('keydown', function (e) {
                if (e.key === 'Escape' && isModalOpen()) closePanel();
            });
            initDropZone();
            document.addEventListener('paste', handlePaste);
        }
    };

    document.addEventListener('DOMContentLoaded', function () {
        window.artikelenFotos.init();
    });
})();
