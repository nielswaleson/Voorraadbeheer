(function () {
    var persoon = null;
    var locatie = null;
    var artikel = null;
    var bakArtikelen = [];

    function $(id) { return document.getElementById(id); }

    function focusScan() {
        var input = $('scanInput');
        if (input) { input.focus(); input.select(); }
    }

    function setStatus(msg, type) {
        var el = $('scanStatus');
        el.textContent = msg || '';
        el.className = 'scan-status' + (type ? ' ' + type : '');
    }

    function addLog(msg, type) {
        var log = $('scanLog');
        var line = document.createElement('div');
        line.className = 'scan-log-line' + (type ? ' ' + type : '');
        var time = new Date().toLocaleTimeString('nl-NL', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
        line.textContent = time + ' - ' + msg;
        log.insertBefore(line, log.firstChild);
        while (log.children.length > 12) log.removeChild(log.lastChild);
    }

    function escapeHtml(s) {
        return String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
    }

    function showPersoon() {
        $('persoonBar').hidden = !persoon;
        if (persoon) $('persoonNaam').textContent = persoon.naam + ' (' + (persoon.barcode || 'P' + persoon.id) + ')';
    }

    function showLocatie() {
        $('locatieBar').hidden = !locatie;
        if (locatie) {
            $('locatieLabel').textContent = locatie.locatie + ' - ' + locatie.naam;
        }
    }

    function hideArtikelKeuze() {
        $('artikelKeuze').hidden = true;
        $('artikelKaarten').innerHTML = '';
        bakArtikelen = [];
    }

    function sortBakArtikelen(list) {
        return (list || []).slice().sort(function (a, b) {
            if (!!a.actief !== !!b.actief) return a.actief ? -1 : 1;
            var diff = (a.aantal || 0) - (b.aantal || 0);
            if (diff !== 0) return diff;
            return (a.bestelCode || '').localeCompare(b.bestelCode || '', 'nl');
        });
    }

    function firstActiveArtikel() {
        for (var i = 0; i < bakArtikelen.length; i++) {
            if (bakArtikelen[i].actief) return bakArtikelen[i];
        }
        return null;
    }

    function autoSelectFirstArtikel() {
        var first = firstActiveArtikel();
        if (first) selectArtikel(first.artikelId, false);
        else {
            artikel = null;
            $('artikelPanel').hidden = true;
            renderArtikelKaarten();
        }
    }

    function renderArtikelKaarten() {
        var root = $('artikelKaarten');
        root.innerHTML = '';
        var selectedId = artikel ? artikel.artikelId : 0;

        bakArtikelen.forEach(function (a) {
            var card = document.createElement('button');
            card.type = 'button';
            card.className = 'artikel-kaart';
            if (!a.actief) card.className += ' inactief';
            if (!a.fotoUrl) card.className += ' no-foto';
            if (a.isAlarm) card.className += ' alarm';
            if (a.artikelId === selectedId) card.className += ' selected';
            card.setAttribute('data-id', a.artikelId);
            card.disabled = !a.actief;

            var fotoHtml = a.fotoUrl
                ? '<img class="kaart-foto" src="' + escapeHtml(a.fotoUrl) + '" alt="" />'
                : '';

            card.innerHTML =
                fotoHtml +
                '<div class="kaart-body">' +
                '<strong class="kaart-naam">' + escapeHtml(a.naam) + '</strong>' +
                '<span class="kaart-meta">' + escapeHtml(a.leverancier || '') + '</span>' +
                '<span class="kaart-aantal' + (a.isAlarm ? ' laag' : '') + '">' + a.aantal + ' stuks</span>' +
                (a.isAlarm ? '<span class="kaart-alarm">Bak onder alarm</span>' : '') +
                (!a.actief ? '<span class="kaart-inactief">Inactief</span>' : '') +
                '</div>';

            card.addEventListener('click', function () {
                if (!a.actief) return;
                selectArtikel(a.artikelId);
            });

            root.appendChild(card);
        });
    }

    function showArtikelKeuze(data) {
        bakArtikelen = sortBakArtikelen(data.artikelen || []);
        $('artikelKeuze').hidden = false;
        var alarmTxt = data.isAlarm
            ? (' - onder alarm (totaal ' + data.bakTotaal + ' <= ' + data.alarmAantal + ')')
            : (' - totaal ' + data.bakTotaal + ' - alarm <= ' + data.alarmAantal);
        $('artikelKeuzeSoort').textContent = (data.soort ? 'Soort: ' + data.soort + ' - ' : '') +
            'laagste voorraad eerst - klik om te wisselen' + alarmTxt;
        renderArtikelKaarten();
    }

    function loadBakArtikelen(keepSelection) {
        if (!locatie) return;
        var prevId = keepSelection && artikel ? artikel.artikelId : 0;

        PageMethods.GetBakArtikelen(locatie.id,
            function (result) {
                if (!result || !result.success) {
                    setStatus((result && result.message) || 'Artikelen laden mislukt', 'err');
                    return;
                }
                showArtikelKeuze(result);
                if (prevId) {
                    var still = bakArtikelen.some(function (a) { return a.artikelId === prevId && a.actief; });
                    if (still) selectArtikel(prevId, true);
                    else autoSelectFirstArtikel();
                } else {
                    autoSelectFirstArtikel();
                }
            },
            function (err) {
                setStatus('Fout: ' + (err.get_message ? err.get_message() : err), 'err');
            }
        );
    }

    function showArtikel(a) {
        artikel = a;
        if (!a) { $('artikelPanel').hidden = true; renderArtikelKaarten(); return; }

        $('artikelPanel').hidden = false;
        $('artikelNaam').textContent = a.naam || '';
        $('artikelMeta').textContent =
            (a.bestelCode ? 'Bestelcode: ' + a.bestelCode + ' - ' : '') +
            'Leverancier: ' + (a.leverancier || '-') +
            (a.soort ? ' - Soort: ' + a.soort : '');
        $('artikelOmschrijving').textContent = a.omschrijving || '';

        var qtyEl = $('voorraadAantal');
        qtyEl.textContent = a.aantal != null ? a.aantal : 0;
        qtyEl.className = 'voorraad-aantal';

        var totEl = $('bakTotaal');
        totEl.textContent = a.bakTotaal != null ? a.bakTotaal : 0;
        totEl.className = 'voorraad-aantal bak-totaal' + (a.isAlarm ? ' alarm' : '');

        $('alarmInfo').textContent = a.isAlarm
            ? ('Bak onder alarmniveau (' + a.bakTotaal + ' <= ' + a.alarmAantal + ')')
            : ('Alarm bak bij <= ' + (a.alarmAantal != null ? a.alarmAantal : 5));
        $('alarmInfo').className = 'alarm-info' + (a.isAlarm ? ' active' : '');
        $('qtySet').value = a.aantal != null ? a.aantal : 0;

        var img = $('artikelFoto');
        var wrap = $('artikelPhotoWrap');
        if (a.fotoUrl) {
            wrap.hidden = false;
            img.src = a.fotoUrl;
        } else {
            wrap.hidden = true;
            img.removeAttribute('src');
        }
        renderArtikelKaarten();
    }

    function selectArtikel(artikelId, silent) {
        if (!locatie) return;

        PageMethods.GetArtikelAtLocatie(locatie.id, artikelId,
            function (result) {
                if (result && result.success && result.artikel) {
                    showArtikel(result.artikel);
                    if (!silent) {
                        setStatus('Geselecteerd: ' + result.artikel.naam + ' - voorraad: ' + result.artikel.aantal,
                            result.artikel.isAlarm ? 'warn' : 'ok');
                        addLog(result.message + ' (voorraad ' + result.artikel.aantal + ')', 'ok');
                    }
                } else {
                    setStatus((result && result.message) || 'Artikel laden mislukt', 'err');
                }
                focusScan();
            },
            function (err) {
                setStatus('Fout: ' + (err.get_message ? err.get_message() : err), 'err');
                focusScan();
            }
        );
    }

    function processScan(code) {
        code = (code || '').trim();
        if (!code) return;
        $('scanInput').value = '';

        PageMethods.ResolveBarcode(code,
            function (result) {
                if (!result || !result.success) {
                    setStatus((result && result.message) || 'Onbekende barcode', 'err');
                    addLog((result && result.message) || code + ': onbekend', 'err');
                    focusScan();
                    return;
                }

                if (result.scanType === 'actie_af') {
                    if (!persoon) setStatus('Scan eerst een persoon', 'err');
                    else if (!locatie) setStatus('Scan eerst een bak (L...)', 'err');
                    else if (!artikel) setStatus('Kies eerst een artikel', 'err');
                    else adjust('remove', 1);
                    focusScan();
                    return;
                }

                if (result.scanType === 'persoon') {
                    persoon = { id: result.persoonId, naam: result.persoonNaam, barcode: null };
                    showPersoon();
                    setStatus('Persoon OK - scan bak (L...)', 'ok');
                    addLog(result.message, 'ok');
                    focusScan();
                    return;
                }

                if (result.scanType === 'locatie') {
                    if (!persoon) {
                        setStatus('Scan eerst een persoon', 'warn');
                        addLog('Bak geweigerd: geen persoon', 'warn');
                        focusScan();
                        return;
                    }
                    locatie = { id: result.voorraadId, locatie: result.locatie, naam: result.locatieNaam };
                    artikel = null;
                    showLocatie();
                    showArtikel(null);
                    loadBakArtikelen(false);
                    setStatus(result.message + ' - kies een artikel', 'ok');
                    addLog(result.message, 'ok');
                    focusScan();
                }
            },
            function (err) {
                setStatus('Fout: ' + (err.get_message ? err.get_message() : err), 'err');
                focusScan();
            }
        );
    }

    function adjust(mode, amount) {
        if (!persoon || !locatie || !artikel) {
            setStatus('Scan persoon, bak en kies artikel', 'warn');
            return;
        }

        PageMethods.AdjustVoorraad(persoon.id, locatie.id, artikel.artikelId, mode, amount,
            function (result) {
                if (result && result.success) {
                    setStatus(result.message, result.artikel && result.artikel.isAlarm ? 'warn' : 'ok');
                    addLog(result.message, 'ok');
                    if (result.artikel) showArtikel(result.artikel);
                    loadBakArtikelen(true);
                } else {
                    setStatus((result && result.message) || 'Actie mislukt', 'err');
                    addLog((result && result.message) || 'Actie mislukt', 'err');
                }
                focusScan();
            },
            function (err) {
                setStatus('Fout: ' + (err.get_message ? err.get_message() : err), 'err');
                focusScan();
            }
        );
    }

    function resetSession() {
        persoon = null;
        locatie = null;
        artikel = null;
        showPersoon();
        showLocatie();
        hideArtikelKeuze();
        showArtikel(null);
        setStatus('Scan persoon (P...) -> bak (L...) -> kies artikel', '');
        focusScan();
    }

    function init() {
        var input = $('scanInput');
        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); processScan(input.value); }
        });
        document.addEventListener('click', function (e) {
            if (e.target.closest('button, a, input, select, label, .artikel-kaart')) return;
            focusScan();
        });
        $('btnClearSession').addEventListener('click', resetSession);
        $('btnRemove1').addEventListener('click', function () { adjust('remove', 1); });
        $('btnRemoveN').addEventListener('click', function () { adjust('remove', parseInt($('qtyRemove').value, 10) || 1); });
        $('btnAddN').addEventListener('click', function () { adjust('add', parseInt($('qtyAdd').value, 10) || 1); });
        $('btnSetQty').addEventListener('click', function () { adjust('set', parseInt($('qtySet').value, 10) || 0); });
        focusScan();
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
