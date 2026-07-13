initSheetGrid({
    idField: 'LeverancierID',
    columns: [
        { field: 'Naam', col: 'naam', type: 'text', header: 'Naam', cssClass: 'col-naam' },
        { field: 'Actief', col: 'actief', type: 'bool', header: 'Actief', cssClass: 'col-actief' }
    ],
    createRow: function () {
        return { LeverancierID: 0, Naam: '', Actief: true, _dirty: true };
    },
    mapRow: function (r) {
        return { LeverancierID: r.LeverancierID, Naam: r.Naam || '', Actief: !!r.Actief };
    },
    mapSave: function (r) {
        return { LeverancierID: r.LeverancierID, Naam: r.Naam || '', Actief: !!r.Actief };
    },
    getData: function (ok, err) { PageMethods.GetLeveranciers(ok, err); },
    saveData: function (payload, ok, err) {
        PageMethods.SaveChanges(JSON.stringify(payload), ok, err);
    }
});
