initSheetGrid({
    idField: 'PersoonID',
    columns: [
        { field: 'Barcode', col: 'barcode', type: 'label', header: 'Barcode', cssClass: 'col-barcode' },
        { field: 'Naam', col: 'naam', type: 'text', header: 'Naam', cssClass: 'col-naam' },
        { field: 'Actief', col: 'actief', type: 'bool', header: 'Actief', cssClass: 'col-actief' }
    ],
    createRow: function () {
        return { PersoonID: 0, Barcode: '(nieuw)', Naam: '', Actief: true, _dirty: true };
    },
    mapRow: function (r) {
        return {
            PersoonID: r.PersoonID,
            Barcode: r.Barcode || (r.PersoonID ? 'P' + r.PersoonID : ''),
            Naam: r.Naam || '',
            Actief: !!r.Actief
        };
    },
    mapSave: function (r) {
        return { PersoonID: r.PersoonID, Naam: r.Naam || '', Actief: !!r.Actief };
    },
    getData: function (ok, err) { PageMethods.GetPersonen(ok, err); },
    saveData: function (payload, ok, err) {
        PageMethods.SaveChanges(JSON.stringify(payload), ok, err);
    }
});
