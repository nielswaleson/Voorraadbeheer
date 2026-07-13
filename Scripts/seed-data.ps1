$ErrorActionPreference = 'Stop'
$connString = 'Server=localhost\SQLEXPRESS;Database=magazijn;Trusted_Connection=True;TrustServerCertificate=True;'

# Minimal valid 1x1 JPEG
$jpegBytes = [Convert]::FromBase64String(
    '/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDAREAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAb/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAgP/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwCfAA//2Q=='
)

$leverancierNamen = @(
    'TechnoSupply BV', 'MetaalGroothandel Jansen', 'Elektro Parts Nederland', 'BouwDirect XL',
    'Industrie Hub Noord', 'LogiTrade Europe', 'PakMaterialen Plus', 'Staal & Co Rotterdam',
    'Hydrauliek Centrum', 'Office Pro Groep', 'Chemie Depot BV', 'AutoParts Benelux',
    'Garden & Tools NL', 'FoodService Levering', 'Klimaat Techniek BV', 'VeiligheidShop',
    'Print & Label Solutions', 'Sanitair Groothandel', 'IT Hardware Direct', 'Textiel Import NL',
    'Farmaceutica Supply', 'Energie Componenten', 'Plastics United', 'TimmerGroothandel De Vries',
    'Maritiem Parts Center', 'Sport & Leisure BV', 'Meubel Import Europa', 'Papier & Karton NL',
    'Licht & Elektra Groep', 'Algemene Groothandel Meijer'
)

$artikelPrefixes = @('Schroef', 'Moer', 'Bout', 'Pakking', 'Filter', 'Kabel', 'Sensor', 'Klep', 'Motor', 'Pomp',
    'Lager', 'Seal', 'Plaat', 'Buis', 'Connector', 'Schakelaar', 'Relais', 'Display', 'Behuizing', 'Kit')
$artikelSuffixes = @('M6', 'M8', 'M10', '200mm', '500ml', '1m', 'XL', 'Pro', 'Standard', 'Heavy Duty', 'RVS', 'Brons', 'Koper')

$rng = New-Object System.Random(42)

$conn = New-Object System.Data.SqlClient.SqlConnection($connString)
$conn.Open()
$tx = $conn.BeginTransaction()

try {
    $leverancierIds = New-Object System.Collections.Generic.List[int]

    $cmdLev = $conn.CreateCommand()
    $cmdLev.Transaction = $tx
    $cmdLev.CommandText = 'INSERT INTO dbo.Leverancier (Naam, Actief) OUTPUT INSERTED.LeverancierID VALUES (@Naam, @Actief)'

    $pNaam = $cmdLev.Parameters.Add('@Naam', [System.Data.SqlDbType]::Text)
    $pActief = $cmdLev.Parameters.Add('@Actief', [System.Data.SqlDbType]::Bit)

    foreach ($naam in $leverancierNamen) {
        $pNaam.Value = $naam
        $pActief.Value = $true
        $id = [int]$cmdLev.ExecuteScalar()
        [void]$leverancierIds.Add($id)
    }

    Write-Host "Inserted $($leverancierIds.Count) leveranciers."

    $cmdArt = $conn.CreateCommand()
    $cmdArt.Transaction = $tx
    $cmdArt.CommandText = @'
INSERT INTO dbo.Artikel (LeverancierID, Naam, Omschrijving, BestelCode)
OUTPUT INSERTED.ArtikelID
VALUES (@LeverancierID, @Naam, @Omschrijving, @BestelCode)
'@

    $pLevId = $cmdArt.Parameters.Add('@LeverancierID', [System.Data.SqlDbType]::Int)
    $pArtNaam = $cmdArt.Parameters.Add('@Naam', [System.Data.SqlDbType]::Text)
    $pOms = $cmdArt.Parameters.Add('@Omschrijving', [System.Data.SqlDbType]::Text)
    $pCode = $cmdArt.Parameters.Add('@BestelCode', [System.Data.SqlDbType]::NChar, 50)

    $artikelIds = New-Object System.Collections.Generic.List[int]

    for ($i = 1; $i -le 1000; $i++) {
        $levIdx = $rng.Next(0, $leverancierIds.Count)
        $prefix = $artikelPrefixes[$rng.Next($artikelPrefixes.Count)]
        $suffix = $artikelSuffixes[$rng.Next($artikelSuffixes.Count)]
        $naam = "$prefix $suffix #$i"
        $oms = "Testartikel $i - $($leverancierNamen[$levIdx])"
        $code = ('ART{0:D5}' -f $i)

        $pLevId.Value = $leverancierIds[$levIdx]
        $pArtNaam.Value = $naam
        $pOms.Value = $oms
        $pCode.Value = $code

        $artId = [int]$cmdArt.ExecuteScalar()
        [void]$artikelIds.Add($artId)

        if ($i % 200 -eq 0) { Write-Host "  $i artikelen..." }
    }

    Write-Host "Inserted $($artikelIds.Count) artikelen."

    $cmdFoto = $conn.CreateCommand()
    $cmdFoto.Transaction = $tx
    $cmdFoto.CommandText = 'INSERT INTO dbo.Foto (Foto, ArtikelID, Tijd) VALUES (@Foto, @ArtikelID, @Tijd)'

    $pFoto = $cmdFoto.Parameters.Add('@Foto', [System.Data.SqlDbType]::Image)
    $pArtId = $cmdFoto.Parameters.Add('@ArtikelID', [System.Data.SqlDbType]::Int)
    $pTijd = $cmdFoto.Parameters.Add('@Tijd', [System.Data.SqlDbType]::DateTime2)

    $fotoCount = 0
    foreach ($artId in $artikelIds) {
        $n = $rng.Next(0, 6)
        for ($f = 0; $f -lt $n; $f++) {
            $pFoto.Value = $jpegBytes
            $pArtId.Value = $artId
            $pTijd.Value = (Get-Date).AddDays(-$rng.Next(0, 365)).AddMinutes(-$rng.Next(0, 1440))
            [void]$cmdFoto.ExecuteNonQuery()
            $fotoCount++
        }
    }

    Write-Host "Inserted $fotoCount fotos."

    $tx.Commit()
    Write-Host 'Klaar.'
}
catch {
    $tx.Rollback()
    throw
}
finally {
    $conn.Close()
}
