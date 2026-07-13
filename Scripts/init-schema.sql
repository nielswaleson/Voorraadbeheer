-- Basisschema Voorraadbeheer (idempotent)

IF OBJECT_ID('dbo.Leverancier', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Leverancier (
        LeverancierID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Naam text NOT NULL,
        Actief bit NOT NULL CONSTRAINT DF_Leverancier_Actief DEFAULT (1)
    );
END

IF OBJECT_ID('dbo.Persoon', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Persoon (
        PersoonID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Naam text NOT NULL,
        Actief bit NOT NULL CONSTRAINT DF_Persoon_Actief DEFAULT (1),
        Barcode nvarchar(50) NULL
    );
END

IF OBJECT_ID('dbo.Artikel', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Artikel (
        ArtikelID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        LeverancierID int NOT NULL,
        Naam text NOT NULL,
        Omschrijving text NULL,
        BestelCode nchar(50) NULL,
        Soort nvarchar(100) NULL,
        Actief bit NOT NULL CONSTRAINT DF_Artikel_Actief DEFAULT (1),
        CONSTRAINT FK_Artikel_Leverancier FOREIGN KEY (LeverancierID) REFERENCES dbo.Leverancier(LeverancierID)
    );
END

IF OBJECT_ID('dbo.Voorraad', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Voorraad (
        VoorraadID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Naam text NOT NULL,
        Omschrijving text NULL,
        Locatie nvarchar(50) NOT NULL CONSTRAINT DF_Voorraad_Locatie DEFAULT (''),
        AlarmAantal int NOT NULL CONSTRAINT DF_Voorraad_AlarmAantal DEFAULT (5)
    );
END

IF OBJECT_ID('dbo.VoorraadInhoud', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VoorraadInhoud (
        VoorraadInhoudID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        VoorraadID int NOT NULL,
        ArtikelID int NOT NULL,
        Aantal int NOT NULL CONSTRAINT DF_VoorraadInhoud_Aantal DEFAULT (0),
        CONSTRAINT FK_VoorraadInhoud_Voorraad FOREIGN KEY (VoorraadID) REFERENCES dbo.Voorraad(VoorraadID),
        CONSTRAINT FK_VoorraadInhoud_Artikel FOREIGN KEY (ArtikelID) REFERENCES dbo.Artikel(ArtikelID),
        CONSTRAINT UQ_VoorraadInhoud_LocArt UNIQUE (VoorraadID, ArtikelID)
    );
END

IF OBJECT_ID('dbo.Foto', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Foto (
        FotoID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Foto image NOT NULL,
        ArtikelID int NULL,
        Tijd datetime2 NOT NULL CONSTRAINT DF_Foto_Tijd DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_Foto_Artikel FOREIGN KEY (ArtikelID) REFERENCES dbo.Artikel(ArtikelID)
    );
END

IF OBJECT_ID('dbo.Log', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Log (
        LogID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Actie text NOT NULL,
        Tijd datetime2 NOT NULL CONSTRAINT DF_Log_Tijd DEFAULT (SYSDATETIME()),
        PersoonID int NULL,
        VoorraadID int NULL,
        ArtikelID int NULL,
        CONSTRAINT FK_Log_Persoon FOREIGN KEY (PersoonID) REFERENCES dbo.Persoon(PersoonID),
        CONSTRAINT FK_Log_Voorraad FOREIGN KEY (VoorraadID) REFERENCES dbo.Voorraad(VoorraadID),
        CONSTRAINT FK_Log_Artikel FOREIGN KEY (ArtikelID) REFERENCES dbo.Artikel(ArtikelID)
    );
END
