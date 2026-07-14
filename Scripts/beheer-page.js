(function () {
    function $(id) { return document.getElementById(id); }

    function setStatus(msg, type) {
        var el = $('seedStatus');
        el.textContent = msg || '';
        el.className = 'sheet-status' + (type ? ' ' + type : '');
    }

    function init() {
        $('btnSeedTestData').addEventListener('click', function () {
            var clearFirst = $('chkClearFirst').checked;
            var msg = clearFirst
                ? 'Alle data wordt gewist en opnieuw gevuld met testdata. Doorgaan?'
                : 'Database vullen met testdata (alleen als leeg). Doorgaan?';
            if (!confirm(msg)) return;

            var btn = $('btnSeedTestData');
            btn.disabled = true;
            setStatus('Bezig met laden... dit kan even duren.', '');

            PageMethods.SeedTestData(clearFirst,
                function (result) {
                    btn.disabled = false;
                    setStatus(result.message, result.success ? 'ok' : 'err');
                },
                function (err) {
                    btn.disabled = false;
                    setStatus('Fout: ' + (err.get_message ? err.get_message() : err), 'err');
                }
            );
        });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
