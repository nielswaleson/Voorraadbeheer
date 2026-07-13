(function () {
    var currentPage = 1;
    var pageSize = 50;
    var totalCount = 0;

    function $(id) { return document.getElementById(id); }

    function setStatus(msg, type) {
        var el = $('logStatus');
        el.textContent = msg || '';
        el.className = 'sheet-status' + (type ? ' ' + type : '');
    }

    function escapeHtml(s) {
        return String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;');
    }

    function render(entries) {
        var tbody = $('logBody');
        tbody.innerHTML = '';
        (entries || []).forEach(function (e) {
            var tr = document.createElement('tr');
            var loc = e.Locatie + (e.LocatieNaam ? ' (' + e.LocatieNaam + ')' : '');
            tr.innerHTML =
                '<td>' + escapeHtml(e.Tijd) + '</td>' +
                '<td>' + escapeHtml(e.Persoon) + '</td>' +
                '<td>' + escapeHtml(loc) + '</td>' +
                '<td>' + escapeHtml(e.Artikel) + '</td>' +
                '<td>' + escapeHtml(e.Actie) + '</td>';
            tbody.appendChild(tr);
        });
    }

    function updatePagination() {
        var totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
        $('pageInfo').textContent = 'Pagina ' + currentPage + ' van ' + totalPages + ' (' + totalCount + ' regels)';
        $('btnPrevPage').disabled = currentPage <= 1;
        $('btnNextPage').disabled = currentPage >= totalPages;
    }

    function load() {
        PageMethods.GetLog(currentPage, pageSize,
            function (data) {
                totalCount = data.totalCount || 0;
                render(data.entries);
                updatePagination();
                setStatus('', '');
            },
            function (err) {
                setStatus('Fout: ' + (err.get_message ? err.get_message() : err), 'err');
            }
        );
    }

    function init() {
        $('btnReloadLog').addEventListener('click', function () { load(); });
        $('btnPrevPage').addEventListener('click', function () {
            if (currentPage > 1) { currentPage--; load(); }
        });
        $('btnNextPage').addEventListener('click', function () {
            var totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
            if (currentPage < totalPages) { currentPage++; load(); }
        });
        load();
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
