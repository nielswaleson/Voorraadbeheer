(function (window) {
    function createSheetGrid(config) {
        var rows = [];
        var deletedIds = [];
        var selectedCell = null;
        var selection = null;
        var anchor = null;
        var isDragging = false;
        var dragStarted = false;

        var COLS = config.columns.map(function (c) { return c.col; });

        function $(id) { return document.getElementById(id); }

        function colIndex(colName) {
            return COLS.indexOf(colName);
        }

        function colByName(colName) {
            for (var i = 0; i < config.columns.length; i++) {
                if (config.columns[i].col === colName) return config.columns[i];
            }
            return null;
        }

        function setStatus(msg, type) {
            var el = $('sheetStatus');
            el.textContent = msg || '';
            el.className = 'sheet-status' + (type ? ' ' + type : '');
        }

        function parseBool(val) {
            if (val === true || val === 1) return true;
            if (val === false || val === 0) return false;
            var s = String(val || '').trim().toLowerCase();
            if (!s) return true;
            if (s === '1' || s === 'ja' || s === 'j' || s === 'yes' || s === 'y' || s === 'true' || s === 'waar') return true;
            if (s === '0' || s === 'nee' || s === 'n' || s === 'no' || s === 'false' || s === 'onwaar') return false;
            return true;
        }

        function getRowId(row) {
            return row[config.idField] || 0;
        }

        function markDirty(rowIndex) {
            rows[rowIndex]._dirty = true;
            var tr = $('sheetBody').rows[rowIndex];
            if (!tr) return;
            var dirtyCell = tr.querySelector('.cell-text, .cell-select-wrap');
            if (dirtyCell) dirtyCell.classList.add('dirty');
        }

        function normalizeSelection(r1, c1, r2, c2) {
            return {
                r1: Math.min(r1, r2),
                c1: Math.min(c1, c2),
                r2: Math.max(r1, r2),
                c2: Math.max(c1, c2)
            };
        }

        function setSelection(r1, c1, r2, c2, keepAnchor) {
            selection = normalizeSelection(r1, c1, r2, c2);
            if (!keepAnchor) anchor = { row: r1, col: c1 };
            updateSelectionVisuals();
        }

        function clearSelection() {
            selection = null;
            updateSelectionVisuals();
        }

        function getCellElement(row, colName) {
            var tbody = $('sheetBody');
            if (row < 0 || row >= tbody.rows.length) return null;
            return tbody.rows[row].querySelector('.cell[data-col="' + colName + '"]');
        }

        function getLookupOptions(col) {
            return (config.lookups && config.lookups[col.lookup]) || [];
        }

        function resolveSelectValue(col, raw) {
            var opts = getLookupOptions(col);
            var s = String(raw || '').trim();
            if (!s) return 0;
            var n = parseInt(s, 10);
            if (!isNaN(n)) {
                for (var i = 0; i < opts.length; i++) {
                    if (opts[i].id === n) return n;
                }
            }
            var lower = s.toLowerCase();
            for (var j = 0; j < opts.length; j++) {
                if (String(opts[j].naam || '').toLowerCase() === lower) return opts[j].id;
            }
            return null;
        }

        function getSelectLabel(col, id) {
            var opts = getLookupOptions(col);
            for (var i = 0; i < opts.length; i++) {
                if (opts[i].id === id) return opts[i].naam || '';
            }
            return id ? String(id) : '';
        }

        function getCellValue(row, colName) {
            var r = rows[row];
            if (!r) return '';
            var col = colByName(colName);
            if (!col) return '';
            if (col.type === 'label') {
                if (col.format) return col.format(r);
                return String(r[col.field] != null ? r[col.field] : '');
            }
            if (col.type === 'bool') return r[col.field] ? '1' : '0';
            if (col.type === 'select') return getSelectLabel(col, r[col.field]);
            return r[col.field] || '';
        }

        function setCellValue(rowIndex, colName, raw) {
            var col = colByName(colName);
            if (!col) return;
            if (col.type === 'label' || col.type === 'readonly') return;
            if (col.type === 'bool') rows[rowIndex][col.field] = parseBool(raw);
            else if (col.type === 'select') {
                var resolved = resolveSelectValue(col, raw);
                if (resolved !== null) rows[rowIndex][col.field] = resolved;
            } else rows[rowIndex][col.field] = raw;
        }

        function updateSelectionVisuals() {
            var tbody = $('sheetBody');
            tbody.querySelectorAll('.cell').forEach(function (cell) {
                cell.classList.remove('selected', 'in-range');
            });

            if (!selection) {
                if (selectedCell) selectedCell.classList.add('selected');
                return;
            }

            for (var r = selection.r1; r <= selection.r2; r++) {
                for (var c = selection.c1; c <= selection.c2; c++) {
                    var cell = getCellElement(r, COLS[c]);
                    if (!cell) continue;
                    cell.classList.add('in-range');
                    if (r === selection.r2 && c === selection.c2) {
                        cell.classList.add('selected');
                        selectedCell = cell;
                    }
                }
            }
        }

        function selectionSize() {
            if (!selection) return 0;
            return (selection.r2 - selection.r1 + 1) * (selection.c2 - selection.c1 + 1);
        }

        function getSelectionText() {
            if (!selection) {
                if (!selectedCell) return '';
                return getCellValue(
                    parseInt(selectedCell.getAttribute('data-row'), 10),
                    selectedCell.getAttribute('data-col')
                );
            }

            var lines = [];
            for (var r = selection.r1; r <= selection.r2; r++) {
                var parts = [];
                for (var c = selection.c1; c <= selection.c2; c++) {
                    parts.push(getCellValue(r, COLS[c]));
                }
                lines.push(parts.join('\t'));
            }
            return lines.join('\n');
        }

        function copySelection() {
            if (!selectedCell && !selection) return;
            var text = getSelectionText();

            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(text).then(function () {
                    setStatus((selection ? selectionSize() : 1) + ' cel(len) gekopieerd', 'ok');
                }).catch(function () { fallbackCopy(text); });
            } else {
                fallbackCopy(text);
            }
        }

        function fallbackCopy(text) {
            var ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed';
            ta.style.left = '-9999px';
            document.body.appendChild(ta);
            ta.select();
            try {
                document.execCommand('copy');
                setStatus((selection ? selectionSize() : 1) + ' cel(len) gekopieerd', 'ok');
            } catch (err) {
                setStatus('Kopiëren mislukt', 'err');
            }
            document.body.removeChild(ta);
        }

        function escapeAttr(s) {
            return String(s)
                .replace(/&/g, '&amp;')
                .replace(/"/g, '&quot;')
                .replace(/</g, '&lt;');
        }

        function buildHeaderRow() {
            var html = '<th class="col-row">#</th><th class="col-id">ID</th>';
            config.columns.forEach(function (col) {
                html += '<th class="' + col.cssClass + '">' + escapeAttr(col.header) + '</th>';
            });
            html += '<th class="col-del"></th>';
            return html;
        }

        function buildDataCells(row, i) {
            var html = '';
            config.columns.forEach(function (col) {
                if (col.type === 'bool') {
                    html += '<td class="' + col.cssClass + ' cell-bool cell" data-col="' + col.col + '" data-row="' + i + '">' +
                        '<input type="checkbox"' + (row[col.field] ? ' checked' : '') + ' /></td>';
                } else if (col.type === 'select') {
                    var opts = getLookupOptions(col);
                    var sel = '<select class="cell-select">';
                    if (!row[col.field]) sel += '<option value="">- kies -</option>';
                    opts.forEach(function (o) {
                        sel += '<option value="' + o.id + '"' + (row[col.field] === o.id ? ' selected' : '') + '>' +
                            escapeAttr(o.naam || '') + '</option>';
                    });
                    sel += '</select>';
                    html += '<td class="' + col.cssClass + ' cell-select-wrap cell" data-col="' + col.col + '" data-row="' + i + '">' + sel + '</td>';
                } else if (col.type === 'label') {
                    var labelText = col.format ? col.format(row) : String(row[col.field] != null ? row[col.field] : '');
                    html += '<td class="' + col.cssClass + ' cell-label cell" data-col="' + col.col + '" data-row="' + i + '">' +
                        '<button type="button" class="cell-label-btn">' + escapeAttr(labelText) + '</button></td>';
                } else {
                    html += '<td class="' + col.cssClass + ' cell-text cell" data-col="' + col.col + '" data-row="' + i + '">' +
                        '<input type="text" class="cell-input" value="' + escapeAttr(row[col.field] || '') + '" /></td>';
                }
            });
            return html;
        }

        function applyVisibility() {
            if (!config.filterFn) return;
            var tbody = $('sheetBody');
            var visible = 0;
            Array.prototype.forEach.call(tbody.rows, function (tr, i) {
                var show = config.filterFn(rows[i]);
                tr.style.display = show ? '' : 'none';
                if (show) visible++;
            });
            return visible;
        }

        function render() {
            var tbody = $('sheetBody');
            var savedSelection = selection ? Object.assign({}, selection) : null;
            tbody.innerHTML = '';

            rows.forEach(function (row, i) {
                var tr = document.createElement('tr');
                if (!getRowId(row)) tr.className = 'row-new';

                tr.innerHTML =
                    '<td class="col-row">' + (i + 1) + '</td>' +
                    '<td class="col-id">' + (getRowId(row) || 'nieuw') + '</td>' +
                    buildDataCells(row, i) +
                    '<td class="col-del"><button type="button" class="btn-del-row" title="Rij verwijderen">&times;</button></td>';

                if (row._dirty) {
                    var dirtyCell = tr.querySelector('.cell-text, .cell-select-wrap');
                    if (dirtyCell) dirtyCell.classList.add('dirty');
                }

                tbody.appendChild(tr);
            });

            if (savedSelection) selection = savedSelection;
            bindEvents();
            updateSelectionVisuals();
            applyVisibility();
        }

        function cellCoords(cell) {
            return {
                row: parseInt(cell.getAttribute('data-row'), 10),
                col: colIndex(cell.getAttribute('data-col'))
            };
        }

        function onCellMouseDown(e) {
            if (e.button !== 0) return;
            var cell = e.target.closest('.cell');
            if (!cell) return;

            var coords = cellCoords(cell);

            if (e.shiftKey && anchor) {
                e.preventDefault();
                setSelection(anchor.row, anchor.col, coords.row, coords.col, true);
                return;
            }

            if (e.target.classList.contains('cell-input') || e.target.classList.contains('cell-select') ||
                e.target.classList.contains('cell-label-btn') || e.target.type === 'checkbox') {
                anchor = { row: coords.row, col: coords.col };
                return;
            }

            isDragging = true;
            dragStarted = false;
            anchor = { row: coords.row, col: coords.col };
            setSelection(coords.row, coords.col, coords.row, coords.col, true);
            selectedCell = cell;
            e.preventDefault();

            document.addEventListener('mousemove', onCellMouseMove);
            document.addEventListener('mouseup', onCellMouseUp);
        }

        function onCellMouseMove(e) {
            if (!isDragging) return;
            dragStarted = true;
            var el = document.elementFromPoint(e.clientX, e.clientY);
            var cell = el && el.closest ? el.closest('.cell') : null;
            if (!cell || !anchor) return;
            var coords = cellCoords(cell);
            setSelection(anchor.row, anchor.col, coords.row, coords.col, true);
        }

        function onCellMouseUp() {
            isDragging = false;
            document.removeEventListener('mousemove', onCellMouseMove);
            document.removeEventListener('mouseup', onCellMouseUp);
            if (dragStarted) {
                var active = document.activeElement;
                if (active && active.blur) active.blur();
            }
        }

        function bindEvents() {
            var tbody = $('sheetBody');

            tbody.querySelectorAll('.cell').forEach(function (cell) {
                cell.addEventListener('mousedown', onCellMouseDown);
            });

            tbody.querySelectorAll('.cell-input').forEach(function (input) {
                input.addEventListener('focus', function () {
                    if (isDragging) return;
                    var cell = input.closest('.cell');
                    var coords = cellCoords(cell);
                    if (!selection || selectionSize() === 1) {
                        anchor = { row: coords.row, col: coords.col };
                        setSelection(coords.row, coords.col, coords.row, coords.col, true);
                    }
                    selectedCell = cell;
                    updateSelectionVisuals();
                });
                input.addEventListener('input', function () {
                    var cell = input.closest('.cell');
                    var idx = parseInt(cell.getAttribute('data-row'), 10);
                    var col = colByName(cell.getAttribute('data-col'));
                    rows[idx][col.field] = input.value;
                    markDirty(idx);
                });
                input.addEventListener('keydown', onCellKeydown);
            });

            tbody.querySelectorAll('.cell-select').forEach(function (sel) {
                sel.addEventListener('change', function () {
                    var cell = sel.closest('.cell');
                    var idx = parseInt(cell.getAttribute('data-row'), 10);
                    var col = colByName(cell.getAttribute('data-col'));
                    rows[idx][col.field] = parseInt(sel.value, 10) || 0;
                    markDirty(idx);
                });
                sel.addEventListener('focus', function () {
                    if (isDragging) return;
                    var cell = sel.closest('.cell');
                    var coords = cellCoords(cell);
                    anchor = { row: coords.row, col: coords.col };
                    setSelection(coords.row, coords.col, coords.row, coords.col, true);
                    selectedCell = cell;
                    updateSelectionVisuals();
                });
                sel.addEventListener('keydown', onCellKeydown);
            });

            tbody.querySelectorAll('.cell-label-btn').forEach(function (btn) {
                btn.addEventListener('click', function (e) {
                    e.stopPropagation();
                    var cell = btn.closest('.cell');
                    var idx = parseInt(cell.getAttribute('data-row'), 10);
                    var col = colByName(cell.getAttribute('data-col'));
                    if (typeof config.onCellAction === 'function') {
                        config.onCellAction(col, idx, rows[idx], btn);
                    }
                });
            });

            tbody.querySelectorAll('.cell-bool input[type="checkbox"]').forEach(function (cb) {
                cb.addEventListener('change', function () {
                    var cell = cb.closest('.cell');
                    var idx = parseInt(cell.getAttribute('data-row'), 10);
                    var col = colByName(cell.getAttribute('data-col'));
                    rows[idx][col.field] = cb.checked;
                    markDirty(idx);
                });
                cb.addEventListener('focus', function () {
                    if (isDragging) return;
                    var cell = cb.closest('.cell');
                    var coords = cellCoords(cell);
                    anchor = { row: coords.row, col: coords.col };
                    setSelection(coords.row, coords.col, coords.row, coords.col, true);
                    selectedCell = cell;
                    updateSelectionVisuals();
                });
            });

            tbody.querySelectorAll('.btn-del-row').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var tr = btn.closest('tr');
                    deleteRow(Array.prototype.indexOf.call(tbody.rows, tr));
                });
            });
        }

        function focusCell(row, col, extendSelection) {
            var tbody = $('sheetBody');
            if (row < 0 || row >= tbody.rows.length) return;
            var colIdx = colIndex(col);
            if (colIdx < 0) return;

            if (extendSelection && anchor) {
                setSelection(anchor.row, anchor.col, row, colIdx, true);
            } else {
                anchor = { row: row, col: colIdx };
                setSelection(row, colIdx, row, colIdx, true);
            }

            var cell = getCellElement(row, col);
            if (!cell) return;
            var input = cell.querySelector('input, select');
            if (input) input.focus();
            selectedCell = cell;
            updateSelectionVisuals();
        }

        function nextCol(col, dir) {
            var idx = colIndex(col) + dir;
            if (idx < 0 || idx >= COLS.length) return null;
            return COLS[idx];
        }

        function onCellKeydown(e) {
            var cell = e.target.closest('.cell');
            var row = parseInt(cell.getAttribute('data-row'), 10);
            var col = cell.getAttribute('data-col');
            var extend = e.shiftKey;

            if (e.ctrlKey && (e.key === 'c' || e.key === 'C')) {
                if (e.target.selectionStart !== e.target.selectionEnd) return;
                e.preventDefault();
                copySelection();
                return;
            }

            if (e.key === 'Tab') {
                e.preventDefault();
                var n = nextCol(col, e.shiftKey ? -1 : 1);
                if (n) focusCell(row, n, extend);
                else if (!e.shiftKey && row < rows.length - 1) focusCell(row + 1, COLS[0], extend);
                else if (e.shiftKey && row > 0) focusCell(row - 1, COLS[COLS.length - 1], extend);
                else if (!e.shiftKey) addRow();
            } else if (e.key === 'Enter') {
                e.preventDefault();
                if (row < rows.length - 1) focusCell(row + 1, col, extend);
                else addRow();
            } else if (e.key === 'ArrowDown') {
                e.preventDefault();
                focusCell(row + 1, col, extend);
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                focusCell(row - 1, col, extend);
            } else if (e.key === 'ArrowRight') {
                e.preventDefault();
                var nr = nextCol(col, 1);
                if (nr) focusCell(row, nr, extend);
                else if (row < rows.length - 1) focusCell(row + 1, COLS[0], extend);
            } else if (e.key === 'ArrowLeft') {
                e.preventDefault();
                var nl = nextCol(col, -1);
                if (nl) focusCell(row, nl, extend);
                else if (row > 0) focusCell(row - 1, COLS[COLS.length - 1], extend);
            }
        }

        function handleCopy(e) {
            if (!selectedCell && !selection) return;
            var active = document.activeElement;
            if (active && active.classList && active.classList.contains('cell-input')) {
                if (active.selectionStart !== active.selectionEnd) return;
            }
            e.preventDefault();
            if (e.clipboardData) {
                e.clipboardData.setData('text/plain', getSelectionText());
                setStatus((selection ? selectionSize() : 1) + ' cel(len) gekopieerd', 'ok');
            }
        }

        function addRow() {
            rows.push(config.createRow());
            render();
            focusCell(rows.length - 1, COLS[0]);
            setStatus('Nieuwe rij toegevoegd', '');
        }

        function deleteRow(index) {
            var id = getRowId(rows[index]);
            if (id > 0) deletedIds.push(id);
            rows.splice(index, 1);
            clearSelection();
            render();
            setStatus('Rij verwijderd (nog niet opgeslagen)', '');
        }

        function loadData() {
            setStatus('Laden...', '');
            config.getData(
                function (data) {
                    rows = (data || []).map(config.mapRow);
                    deletedIds = [];
                    clearSelection();
                    render();
                    setStatus(rows.length + ' rijen geladen', 'ok');
                    if (typeof config.onDataLoaded === 'function') config.onDataLoaded(data);
                },
                function (err) {
                    setStatus('Fout bij laden: ' + (err.get_message ? err.get_message() : err), 'err');
                }
            );
        }

        function saveData() {
            setStatus('Opslaan...', '');
            config.saveData(
                {
                    rows: rows.map(config.mapSave),
                    deletedIds: deletedIds
                },
                function (result) {
                    if (result && result.success) {
                        deletedIds = [];
                        loadData();
                        setStatus(result.message || 'Opgeslagen', 'ok');
                    } else {
                        setStatus((result && result.message) || 'Opslaan mislukt', 'err');
                    }
                },
                function (err) {
                    setStatus('Fout bij opslaan: ' + (err.get_message ? err.get_message() : err), 'err');
                }
            );
        }

        function getPasteStart() {
            if (selection) return { row: selection.r1, col: COLS[selection.c1] };
            if (selectedCell) {
                return {
                    row: parseInt(selectedCell.getAttribute('data-row'), 10),
                    col: selectedCell.getAttribute('data-col') || COLS[0]
                };
            }
            return { row: 0, col: COLS[0] };
        }

        function handlePaste(e) {
            var text = (e.clipboardData || window.clipboardData).getData('text');
            if (!text || !text.trim()) return;
            e.preventDefault();

            var lines = text.split(/\r?\n/);
            while (lines.length && !lines[lines.length - 1].trim()) lines.pop();
            if (!lines.length) return;

            var start = getPasteStart();
            var startRow = start.row;
            var startColIdx = colIndex(start.col);

            lines.forEach(function (line, li) {
                var parts = line.split('\t');
                var rowIdx = startRow + li;
                while (rowIdx >= rows.length) rows.push(config.createRow());

                parts.forEach(function (part, pi) {
                    var colIdx = startColIdx + pi;
                    if (colIdx >= COLS.length) return;
                    setCellValue(rowIdx, COLS[colIdx], part.trim());
                });
                rows[rowIdx]._dirty = true;
            });

            render();
            focusCell(startRow, COLS[startColIdx]);
            setStatus(lines.length + ' rij(en) geplakt', '');
        }

        function init() {
            var thead = $('sheetHead');
            if (thead) thead.innerHTML = buildHeaderRow();

            $('btnAddRow').addEventListener('click', addRow);
            $('btnSave').addEventListener('click', saveData);
            $('btnReload').addEventListener('click', loadData);
            $('sheetWrap').addEventListener('paste', handlePaste);
            $('sheetWrap').addEventListener('copy', handleCopy);
            document.addEventListener('keydown', function (e) {
                if (!(e.ctrlKey || e.metaKey) || (e.key !== 'c' && e.key !== 'C')) return;
                if (!selectedCell && !selection) return;
                var active = document.activeElement;
                if (active && active.tagName === 'INPUT' && active.type === 'text') {
                    if (active.selectionStart !== active.selectionEnd) return;
                }
                if (active && active.tagName === 'TEXTAREA') return;
                e.preventDefault();
                copySelection();
            });
            loadData();
        }

        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', init);
        } else {
            init();
        }

        return {
            reload: loadData,
            render: render,
            getRows: function () { return rows; },
            setLookups: function (lookups) {
                config.lookups = lookups;
                render();
            },
            setFilter: function (fn) {
                config.filterFn = fn;
                var visible = applyVisibility();
                if (typeof config.onFilterChange === 'function') config.onFilterChange(visible, rows.length);
            }
        };
    }

    window.initSheetGrid = createSheetGrid;
})(window);
