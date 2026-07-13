(function () {
    function $(id) { return document.getElementById(id); }

    function escapeHtml(s) {
        return String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
    }

    function setStatus(msg, type) {
        var el = $('alarmCount');
        el.textContent = msg || '';
        el.className = 'sheet-status' + (type ? ' ' + type : '');
    }

    function soortLabel(bak) {
        if (!bak.artikelen || !bak.artikelen.length) return '';
        return bak.artikelen[0].Soort || '';
    }

    function render(bakken) {
        var root = $('alarmList');
        root.innerHTML = '';

        if (!bakken || !bakken.length) {
            root.innerHTML = '<p class="alarm-empty">Geen bakken in alarm - alles op peil.</p>';
            setStatus('0 bakken in alarm', 'ok');
            return;
        }

        bakken.forEach(function (bak) {
            var sect = document.createElement('div');
            sect.className = 'alarm-bak';
            sect.innerHTML =
                '<div class="alarm-bak-header">' +
                '<strong>' + escapeHtml(bak.Barcode) + '</strong>' +
                '<span>' + escapeHtml(bak.Naam) + '</span>' +
                '<span class="alarm-bak-soort">Soort: ' + escapeHtml(soortLabel(bak)) + '</span>' +
                '<span class="alarm-bak-totaal">Totaal: <strong>' + bak.TotaalAantal + '</strong> / alarm <= ' + bak.AlarmAantal +
                (bak.Tekort > 0 ? ' (tekort ' + bak.Tekort + ')' : '') + '</span>' +
                '</div>' +
                '<table class="alarm-artikel-table">' +
                '<thead><tr>' +
                '<th>Bestelcode</th><th>Artikel</th><th>Leverancier</th>' +
                '<th>Aantal in bak</th><th>Status</th><th>Actie</th>' +
                '</tr></thead><tbody></tbody></table>';

            var tbody = sect.querySelector('tbody');
            (bak.artikelen || []).forEach(function (art) {
                var tr = document.createElement('tr');
                if (!art.Actief) tr.className = 'inactief';

                tr.innerHTML =
                    '<td>' + escapeHtml(art.BestelCode) + '</td>' +
                    '<td>' + escapeHtml(art.Naam) + '</td>' +
                    '<td>' + escapeHtml(art.Leverancier) + '</td>' +
                    '<td class="col-aantal">' + art.Aantal + '</td>' +
                    '<td>' + (art.Actief ? 'Actief' : 'Inactief') + '</td>' +
                    '<td><button type="button" class="btn-inactief" data-id="' + art.ArtikelID + '" data-actief="' + (art.Actief ? '1' : '0') + '">' +
                    (art.Actief ? 'Inactief' : 'Actief maken') + '</button></td>';
                tbody.appendChild(tr);
            });

            root.appendChild(sect);
        });

        root.querySelectorAll('.btn-inactief').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(this.getAttribute('data-id'), 10);
                var wasActief = this.getAttribute('data-actief') === '1';
                PageMethods.SetArtikelActief(id, !wasActief,
                    function (result) {
                        setStatus(result.message, result.success ? 'ok' : 'err');
                        if (result.success) load();
                    },
                    function (err) {
                        setStatus('Fout: ' + (err.get_message ? err.get_message() : err), 'err');
                    }
                );
            });
        });

        setStatus(bakken.length + ' bak(ken) in alarm', 'warn');
    }

    function load() {
        PageMethods.GetAlarmList(
            function (rows) { render(rows); },
            function (err) { setStatus('Fout: ' + (err.get_message ? err.get_message() : err), 'err'); }
        );
    }

    function init() {
        $('btnReloadAlarm').addEventListener('click', load);
        load();
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
