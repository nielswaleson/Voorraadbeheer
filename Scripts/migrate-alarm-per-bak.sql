-- Alarm per voorraadbak (niet per artikelregel)

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Voorraad') AND name = 'AlarmAantal')
    ALTER TABLE dbo.Voorraad ADD AlarmAantal int NOT NULL CONSTRAINT DF_Voorraad_AlarmAantal DEFAULT (5);
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VoorraadInhoud') AND name = 'AlarmAantal')
BEGIN
    UPDATE v
    SET AlarmAantal = ISNULL(x.MaxAlarm, 5)
    FROM dbo.Voorraad v
    INNER JOIN (
        SELECT VoorraadID, MAX(AlarmAantal) AS MaxAlarm
        FROM dbo.VoorraadInhoud
        GROUP BY VoorraadID
    ) x ON x.VoorraadID = v.VoorraadID
    WHERE v.AlarmAantal = 5;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VoorraadInhoud') AND name = 'AlarmAantal')
BEGIN
    DECLARE @df sysname;
    SELECT @df = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.VoorraadInhoud') AND c.name = 'AlarmAantal';
    IF @df IS NOT NULL
        EXEC('ALTER TABLE dbo.VoorraadInhoud DROP CONSTRAINT ' + @df);
    ALTER TABLE dbo.VoorraadInhoud DROP COLUMN AlarmAantal;
END
GO
