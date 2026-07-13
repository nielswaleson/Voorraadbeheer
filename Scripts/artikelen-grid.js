(function () {
    var leveranciers = [];
    var grid;
    var currentPage = 1;
    var pageSize = 100;
    var totalCount = 0;
    var searchTimer = null;

    function $(id) { return document.getElementById(id); }

    function getUrlLeverancierId() {
        var m = /[?&]leverancier=(\d+)/i.exec(window.location.search);
        return m ? parseInt(m[1], 10) : 0;
    }

    function getFilterParams() {
        return {
            leverancierId: parseInt($('filterLeverancier').value, 10) || 0,
            search: ($('filterSearch').value || '').trim()
        };
    }

    function populateFilterDropdown() {
        var sel = $('filterLeverancier');
        var current = sel.value;
        sel.innerHTML = '<option value="">Alle leveranciers</option>';
        leveranciers.forEach(function (l) {
            var opt = document.createElement('option');
            opt.value = l.id;
            opt.textContent = l.naam;
            sel.appendChild(opt);
        });
        if (current) sel.value = current;
    }

    function updateFilterStatus(shown) {
        var el = $('filterCount');
        if (!el) return;
        var totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
        el.textContent = shown + ' op pagina - ' + totalCount + ' totaal - pagina ' + currentPage + '/' + totalPages;
    }

    function updatePagination() {
        var totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
        $('pageInfo').textContent = 'Pagina ' + currentPage + ' van ' + totalPages;
        $('btnPrevPage').disabled = currentPage <= 1;
        $('btnNextPage').disabled = currentPage >= totalPages;
    }

    function reloadPage(resetPage) {
        if (resetPage) currentPage = 1;
        grid.reload();
    }

    grid = initSheetGrid({
        idField: 'ArtikelID',
        lookups: { leveranciers: [] },
        columns: [
            { field: 'LeverancierID', col: 'leverancier', type: 'select', lookup: 'leveranciers', header: 'Leverancier', cssClass: 'col-leverancier' },
            { field: 'Naam', col: 'naam', type: 'text', header: 'Naam', cssClass: 'col-naam' },
            { field: 'Omschrijving', col: 'omschrijving', type: 'text', header: 'Omschrijving', cssClass: 'col-omschrijving' },
            { field: 'BestelCode', col: 'bestelcode', type: 'text', header: 'Bestelcode', cssClass: 'col-bestelcode' },
            {
                field: 'FotoCount', col: 'fotos', type: 'label', header: "Foto's", cssClass: 'col-fotos',
                format: function (row) {
                    if (!row.ArtikelID) return '-';
                    var n = row.FotoCount || 0;
                    return n ? ('📷 ' + n) : '📷 +';
                }
            }
        ],
        createRow: function () {
            var def = 0;
            var fid = parseInt($('filterLeverancier').value, 10);
            if (fid) def = fid;
            else if (leveranciers.length) def = leveranciers[0].id;
            return { ArtikelID: 0, LeverancierID: def, Naam: '', Omschrijving: '', BestelCode: '', FotoCount: 0, _dirty: true };
        },
        mapRow: function (r) {
            return {
                ArtikelID: r.ArtikelID,
                LeverancierID: r.LeverancierID,
                Naam: r.Naam || '',
                Omschrijving: r.Omschrijving || '',
                BestelCode: (r.BestelCode || '').trim(),
                FotoCount: r.FotoCount || 0
            };
        },
        mapSave: function (r) {
            return {
                ArtikelID: r.ArtikelID,
                LeverancierID: r.LeverancierID,
                Naam: (r.Naam || '').trim(),
                Omschrijving: r.Omschrijving || '',
                BestelCode: (r.BestelCode || '').trim()
            };
        },
        getData: function (ok, err) {
            var f = getFilterParams();
            PageMethods.GetPageData(f.leverancierId, f.search, currentPage, pageSize,
                function (data) {
                    leveranciers = (data.leveranciers || []).map(function (l) {
                        return { id: l.LeverancierID, naam: l.Naam || '' };
                    });
                    grid.setLookups({ leveranciers: leveranciers });
                    populateFilterDropdown();
                    var warn = $('leverancierWarn');
                    if (warn) warn.style.display = leveranciers.length ? 'none' : 'block';

                    totalCount = data.totalCount || 0;
                    currentPage = data.page || currentPage;
                    pageSize = data.pageSize || pageSize;

                    var rows = data.artikelen || [];
                    updatePagination();
                    updateFilterStatus(rows.length);
                    ok(rows);
                },
                err
            );
        },
        saveData: function (payload, ok, err) {
            PageMethods.SaveChanges(JSON.stringify(payload), ok, err);
        },
        onDataLoaded: function () {
            var urlLev = getUrlLeverancierId();
            if (urlLev) {
                $('filterLeverancier').value = urlLev;
                reloadPage(true);
            }
        },
        onCellAction: function (col, rowIndex, row) {
            if (col.col === 'fotos' && window.artikelenFotos) {
                window.artikelenFotos.open(row);
            }
        }
    });

    window.artikelenGrid = grid;

    document.addEventListener('DOMContentLoaded', function () {
        var levSel = $('filterLeverancier');
        var search = $('filterSearch');

        if (levSel) {
            levSel.addEventListener('change', function () { reloadPage(true); });
        }
        if (search) {
            search.addEventListener('input', function () {
                clearTimeout(searchTimer);
                searchTimer = setTimeout(function () { reloadPage(true); }, 350);
            });
        }

        $('btnPrevPage').addEventListener('click', function () {
            if (currentPage > 1) {
                currentPage--;
                grid.reload();
            }
        });
        $('btnNextPage').addEventListener('click', function () {
            var totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
            if (currentPage < totalPages) {
                currentPage++;
                grid.reload();
            }
        });
    });
})();
