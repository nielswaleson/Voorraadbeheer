(function () {
    var artikelen = [];
    var selectedVoorraadId = 0;
    var selectedLabel = '';
    var inhoudRows = [];
    var inhoudDeleted = [];
    var locatieGrid;

    function $(id) { return document.getElementById(id); }

    function setInhoudStatus(msg, type) {
        var el = $('inhoudStatus');
        el.textContent = msg || '';
        el.className = 'sheet-status' + (type ? ' ' + type : '');
    }

    function artikelOptions(selectedId) {
        var html = '<option value="">- kies artikel -</option>';
        artikelen.forEach(function (a) {
            html += '<option value="' + a.ArtikelID + '"' + (a.ArtikelID === selectedId ? ' selected' : '') + '>' +
                escapeHtml(a.Label) + '</option>';
        });
        return html;
    }

    function escapeHtml(s) {
        return String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
    }

    function renderInhoud() {
        var tbody = $('inhoudBody');
        tbody.innerHTML = '';
        inhoudRows.forEach(function (row, idx) {
            var tr = document.createElement('tr');
            tr.innerHTML =
                '<td><select class="inhoud-artikel" data-idx="' + idx + '">' + artikelOptions(row.ArtikelID) + '</select></td>' +
                '<td><input type="number" class="inhoud-aantal" data-idx="' + idx + '" min="0" value="' + (row.Aantal || 0) + '" /></td>' +
                '<td><button type="button" class="btn-del-inhoud" data-idx="' + idx + '">x</button></td>';
            tbody.appendChild(tr);
        });

        tbody.querySelectorAll('.inhoud-artikel').forEach(function (sel) {
            sel.addEventListener('change', function () {
                var i = parseInt(this.getAttribute('data-idx'), 10);
                inhoudRows[i].ArtikelID = parseInt(this.value, 10) || 0;
                inhoudRows[i]._dirty = true;
            });
        });
        tbody.querySelectorAll('.inhoud-aantal').forEach(function (inp) {
            inp.addEventListener('change', function () {
                var i = parseInt(this.getAttribute('data-idx'), 10);
                inhoudRows[i].Aantal = parseInt(this.value, 10) || 0;
                inhoudRows[i]._dirty = true;
            });
        });
        tbody.querySelectorAll('.btn-del-inhoud').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var i = parseInt(this.getAttribute('data-idx'), 10);
                var row = inhoudRows[i];
                if (row.VoorraadInhoudID > 0) inhoudDeleted.push(row.VoorraadInhoudID);
                inhoudRows.splice(i, 1);
                renderInhoud();
            });
        });
    }

    function loadInhoud(voorraadId, label) {
        selectedVoorraadId = voorraadId;
        selectedLabel = label || '';
        inhoudDeleted = [];
        $('inhoudSection').hidden = false;
        $('inhoudTitle').textContent = 'Inhoud - ' + (label || 'bak');

        PageMethods.GetInhoud(voorraadId,
            function (rows) {
                inhoudRows = (rows || []).map(function (r) {
                    return {
                        VoorraadInhoudID: r.VoorraadInhoudID,
                        ArtikelID: r.ArtikelID,
                        Aantal: r.Aantal
                    };
                });
                renderInhoud();
                setInhoudStatus('', '');
            },
            function (err) {
                setInhoudStatus('Fout: ' + (err.get_message ? err.get_message() : err), 'err');
            }
        );
    }

    function selectBakRow(rowIdx) {
        if (rowIdx < 0) return;
        var tbody = $('sheetBody');
        var rows = locatieGrid.getRows();
        var row = rows[rowIdx];
        if (!row || !row.VoorraadID) return;
        if (selectedVoorraadId === row.VoorraadID) {
            Array.prototype.forEach.call(tbody.rows, function (r) { r.classList.remove('row-selected'); });
            if (tbody.rows[rowIdx]) tbody.rows[rowIdx].classList.add('row-selected');
            return;
        }

        Array.prototype.forEach.call(tbody.rows, function (r) { r.classList.remove('row-selected'); });
        if (tbody.rows[rowIdx]) tbody.rows[rowIdx].classList.add('row-selected');

        var label = (row.Locatie || '') + (row.Naam ? ' - ' + row.Naam : '');
        loadInhoud(row.VoorraadID, label);
    }

    function rowIndexFromTarget(target) {
        var tbody = $('sheetBody');
        var tr = target.closest('tr');
        if (!tr || tr.parentElement !== tbody) return -1;

        var rowIdx = tr.sectionRowIndex;
        var dataCell = target.closest('[data-row]');
        if (dataCell) rowIdx = parseInt(dataCell.getAttribute('data-row'), 10);
        return isNaN(rowIdx) ? -1 : rowIdx;
    }

    function hookLocatieSelection() {
        var tbody = $('sheetBody');
        if (!tbody) return;

        function activateRow(e) {
            if (e.target.closest('.btn-del-row')) return;
            var rowIdx = rowIndexFromTarget(e.target);
            if (rowIdx >= 0) selectBakRow(rowIdx);
        }

        tbody.addEventListener('click', activateRow);
        tbody.addEventListener('focusin', activateRow);
    }

    function saveInhoud() {
        if (!selectedVoorraadId) return;
        var payload = {
            rows: inhoudRows.filter(function (r) { return r.ArtikelID > 0; }).map(function (r) {
                return {
                    VoorraadInhoudID: r.VoorraadInhoudID || 0,
                    ArtikelID: r.ArtikelID,
                    Aantal: r.Aantal || 0
                };
            }),
            deletedIds: inhoudDeleted
        };

        PageMethods.SaveInhoud(selectedVoorraadId, JSON.stringify(payload),
            function (result) {
                setInhoudStatus(result.message, result.success ? 'ok' : 'err');
                if (result.success) loadInhoud(selectedVoorraadId, selectedLabel);
            },
            function (err) {
                setInhoudStatus('Fout: ' + (err.get_message ? err.get_message() : err), 'err');
            }
        );
    }

    function init() {
        locatieGrid = initSheetGrid({
            idField: 'VoorraadID',
            columns: [
                { field: 'Locatie', col: 'barcode', type: 'label', header: 'Barcode', cssClass: 'col-barcode' },
                { field: 'Naam', col: 'naam', type: 'text', header: 'Naam bak', cssClass: 'col-naam' },
                { field: 'AlarmAantal', col: 'alarm', type: 'text', header: 'Alarm <=', cssClass: 'col-alarm' },
                { field: 'Omschrijving', col: 'omschrijving', type: 'text', header: 'Omschrijving', cssClass: 'col-omschrijving' }
            ],
            createRow: function () {
                return { VoorraadID: 0, Locatie: '(nieuw)', Naam: '', AlarmAantal: 5, Omschrijving: '', _dirty: true };
            },
            mapRow: function (r) {
                return {
                    VoorraadID: r.VoorraadID,
                    Locatie: r.Locatie || '',
                    Naam: r.Naam || '',
                    AlarmAantal: r.AlarmAantal != null ? r.AlarmAantal : 5,
                    Omschrijving: r.Omschrijving || ''
                };
            },
            mapSave: function (r) {
                return {
                    VoorraadID: r.VoorraadID,
                    Locatie: r.Locatie || '',
                    Naam: r.Naam || '',
                    AlarmAantal: parseInt(r.AlarmAantal, 10) || 5,
                    Omschrijving: r.Omschrijving || ''
                };
            },
            getData: function (ok, err) {
                PageMethods.GetPageData(function (data) {
                    artikelen = data.artikelen || [];
                    ok(data.locaties || []);
                }, err);
            },
            saveData: function (payload, ok, err) {
                PageMethods.SaveLocaties(JSON.stringify(payload), ok, err);
            }
        });

        setTimeout(hookLocatieSelection, 300);

        $('btnAddInhoud').addEventListener('click', function () {
            if (!selectedVoorraadId) {
                setInhoudStatus('Selecteer eerst een bak', 'warn');
                return;
            }
            inhoudRows.push({ VoorraadInhoudID: 0, ArtikelID: 0, Aantal: 0, _dirty: true });
            renderInhoud();
        });
        $('btnSaveInhoud').addEventListener('click', saveInhoud);
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
