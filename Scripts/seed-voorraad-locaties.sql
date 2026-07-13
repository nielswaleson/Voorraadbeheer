-- Voorbeeldlocaties en startvoorraad
IF NOT EXISTS (SELECT 1 FROM dbo.Voorraad WHERE Locatie = N'MAG-A')
    INSERT INTO dbo.Voorraad (Locatie, Naam, Omschrijving) VALUES (N'MAG-A', N'Magazijn A', N'Hoofdmagazijn');

IF NOT EXISTS (SELECT 1 FROM dbo.Voorraad WHERE Locatie = N'MAG-B')
    INSERT INTO dbo.Voorraad (Locatie, Naam, Omschrijving) VALUES (N'MAG-B', N'Magazijn B', N'Reservemagazijn');

IF NOT EXISTS (SELECT 1 FROM dbo.Voorraad WHERE Locatie = N'WERK')
    INSERT INTO dbo.Voorraad (Locatie, Naam, Omschrijving) VALUES (N'WERK', N'Werkplaats', N'Werkplaatsvoorraad');

-- Voorbeeldinhoud (eerste 5 artikelen op MAG-A, sommige onder alarm)
DECLARE @MagA int = (SELECT TOP 1 VoorraadID FROM dbo.Voorraad WHERE Locatie = N'MAG-A');

IF @MagA IS NOT NULL
BEGIN
    INSERT INTO dbo.VoorraadInhoud (VoorraadID, ArtikelID, Aantal, AlarmAantal)
    SELECT @MagA, a.ArtikelID,
           CASE WHEN a.ArtikelID % 3 = 0 THEN 2 ELSE 15 END,
           5
    FROM dbo.Artikel a
    WHERE a.ArtikelID <= 5
      AND NOT EXISTS (
          SELECT 1 FROM dbo.VoorraadInhoud vi
          WHERE vi.VoorraadID = @MagA AND vi.ArtikelID = a.ArtikelID
      );
END
