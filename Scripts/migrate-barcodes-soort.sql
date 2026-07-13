-- Barcodes, artikelsoort (vervangbaar), actief-vlag

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Persoon') AND name = 'Barcode')
    ALTER TABLE dbo.Persoon ADD Barcode nvarchar(50) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Artikel') AND name = 'Soort')
    ALTER TABLE dbo.Artikel ADD Soort nvarchar(100) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Artikel') AND name = 'Actief')
    ALTER TABLE dbo.Artikel ADD Actief bit NOT NULL CONSTRAINT DF_Artikel_Actief DEFAULT (1);
GO

UPDATE dbo.Persoon
SET Barcode = 'P' + CAST(PersoonID AS nvarchar(20))
WHERE Barcode IS NULL OR RTRIM(Barcode) = '';
GO

UPDATE dbo.Voorraad
SET Locatie = 'L' + RIGHT('00000' + CAST(VoorraadID AS nvarchar(10)), 5)
WHERE Locatie IS NULL OR RTRIM(Locatie) = '' OR Locatie IN (N'MAG-A', N'MAG-B', N'WERK');
GO

UPDATE dbo.Artikel
SET Soort = LTRIM(RTRIM(
    CASE
        WHEN CHARINDEX('#', CAST(Naam AS nvarchar(max))) > 0
        THEN LEFT(CAST(Naam AS nvarchar(max)), CHARINDEX('#', CAST(Naam AS nvarchar(max))) - 1)
        ELSE CAST(Naam AS nvarchar(max))
    END
))
WHERE Soort IS NULL OR RTRIM(Soort) = '';
GO
