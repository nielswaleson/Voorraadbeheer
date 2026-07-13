-- Voorraad = locatie; VoorraadInhoud = artikelen per locatie met aantal en alarm

IF OBJECT_ID('dbo.VoorraadInhoud', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VoorraadInhoud (
        VoorraadInhoudID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        VoorraadID int NOT NULL,
        ArtikelID int NOT NULL,
        Aantal int NOT NULL CONSTRAINT DF_VoorraadInhoud_Aantal DEFAULT (0),
        AlarmAantal int NOT NULL CONSTRAINT DF_VoorraadInhoud_Alarm DEFAULT (5),
        CONSTRAINT FK_VoorraadInhoud_Voorraad FOREIGN KEY (VoorraadID) REFERENCES dbo.Voorraad(VoorraadID),
        CONSTRAINT FK_VoorraadInhoud_Artikel FOREIGN KEY (ArtikelID) REFERENCES dbo.Artikel(ArtikelID),
        CONSTRAINT UQ_VoorraadInhoud_LocArt UNIQUE (VoorraadID, ArtikelID)
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Voorraad') AND name = 'Locatie')
    ALTER TABLE dbo.Voorraad ADD Locatie nvarchar(50) NOT NULL CONSTRAINT DF_Voorraad_Locatie DEFAULT ('');

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Voorraad') AND name = 'ArtikelID')
BEGIN
    INSERT INTO dbo.VoorraadInhoud (VoorraadID, ArtikelID, Aantal, AlarmAantal)
    SELECT v.VoorraadID, v.ArtikelID, ISNULL(v.Aantal, 0), 5
    FROM dbo.Voorraad v
    WHERE v.ArtikelID IS NOT NULL
      AND NOT EXISTS (
        SELECT 1 FROM dbo.VoorraadInhoud vi
        WHERE vi.VoorraadID = v.VoorraadID AND vi.ArtikelID = v.ArtikelID
    );

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Voorraad_Artikel')
        ALTER TABLE dbo.Voorraad DROP CONSTRAINT FK_Voorraad_Artikel;

    ALTER TABLE dbo.Voorraad DROP COLUMN ArtikelID;
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Voorraad') AND name = 'Aantal')
BEGIN
    DECLARE @dfAantal sysname;
    SELECT @dfAantal = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Voorraad') AND c.name = 'Aantal';
    IF @dfAantal IS NOT NULL
        EXEC('ALTER TABLE dbo.Voorraad DROP CONSTRAINT ' + @dfAantal);
    ALTER TABLE dbo.Voorraad DROP COLUMN Aantal;
END
