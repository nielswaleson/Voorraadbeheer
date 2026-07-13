$ErrorActionPreference = 'Stop'
$connString = 'Server=localhost\SQLEXPRESS;Database=magazijn;Trusted_Connection=True;TrustServerCertificate=True;'
$binCount = 150
$rng = New-Object System.Random(99)

$conn = New-Object System.Data.SqlClient.SqlConnection($connString)
$conn.Open()
$tx = $conn.BeginTransaction()

try {
    $cmdClear = $conn.CreateCommand()
    $cmdClear.Transaction = $tx
    $cmdClear.CommandText = 'DELETE FROM dbo.VoorraadInhoud; DELETE FROM dbo.Voorraad;'
    $cmdClear.ExecuteNonQuery() | Out-Null
    Write-Host 'Bestaande bakken gewist.'

    # Groepeer artikelen per soort (min. 2 varianten voor vervangbaarheid)
    $cmdSoorten = $conn.CreateCommand()
    $cmdSoorten.Transaction = $tx
    $cmdSoorten.CommandText = @"
SELECT Soort, MIN(ArtikelID) AS MinId, COUNT(*) AS Cnt
FROM dbo.Artikel
WHERE Actief = 1 AND Soort IS NOT NULL AND RTRIM(Soort) <> ''
GROUP BY Soort
HAVING COUNT(*) >= 2
ORDER BY Soort
"@
    $soorten = @()
    $r = $cmdSoorten.ExecuteReader()
    while ($r.Read()) {
        $soorten += [PSCustomObject]@{
            Soort = [string]$r['Soort']
            MinId = [int]$r['MinId']
            Cnt = [int]$r['Cnt']
        }
    }
    $r.Close()

    if ($soorten.Count -lt 10) { throw "Te weinig artikelsoorten met varianten ($($soorten.Count))." }

    Write-Host "$($soorten.Count) artikelsoorten beschikbaar."

    $cmdArtBySoort = $conn.CreateCommand()
    $cmdArtBySoort.Transaction = $tx
    $cmdArtBySoort.CommandText = 'SELECT ArtikelID FROM dbo.Artikel WHERE Soort = @Soort AND Actief = 1 ORDER BY ArtikelID'
    $pSoort = $cmdArtBySoort.Parameters.Add('@Soort', [System.Data.SqlDbType]::NVarChar, 100)

    $cmdBin = $conn.CreateCommand()
    $cmdBin.Transaction = $tx
    $cmdBin.CommandText = @"
INSERT INTO dbo.Voorraad (Locatie, Naam, Omschrijving, AlarmAantal)
OUTPUT INSERTED.VoorraadID
VALUES (@Locatie, @Naam, @Omschrijving, @Alarm)
"@
    $pLoc = $cmdBin.Parameters.Add('@Locatie', [System.Data.SqlDbType]::NVarChar, 50)
    $pNaam = $cmdBin.Parameters.Add('@Naam', [System.Data.SqlDbType]::Text)
    $pOms = $cmdBin.Parameters.Add('@Omschrijving', [System.Data.SqlDbType]::Text)
    $pAlarm = $cmdBin.Parameters.Add('@Alarm', [System.Data.SqlDbType]::Int)

    $cmdInhoud = $conn.CreateCommand()
    $cmdInhoud.Transaction = $tx
    $cmdInhoud.CommandText = @"
INSERT INTO dbo.VoorraadInhoud (VoorraadID, ArtikelID, Aantal)
VALUES (@V, @A, @Aantal)
"@

    $created = 0
    for ($i = 1; $i -le $binCount; $i++) {
        $pLoc.Value = ''
        $pNaam.Value = "Bak $i"
        $pOms.Value = "Magazijnbak $i"
        $pAlarm.Value = $rng.Next(3, 12)
        $voorraadId = [int]$cmdBin.ExecuteScalar()

        $barcode = 'L' + ('{0:D5}' -f $voorraadId)
        $cmdUpd = $conn.CreateCommand()
        $cmdUpd.Transaction = $tx
        $cmdUpd.CommandText = 'UPDATE dbo.Voorraad SET Locatie = @L WHERE VoorraadID = @Id'
        $cmdUpd.Parameters.AddWithValue('@L', $barcode) | Out-Null
        $cmdUpd.Parameters.AddWithValue('@Id', $voorraadId) | Out-Null
        $cmdUpd.ExecuteNonQuery() | Out-Null

        $soortPick = $soorten[$rng.Next(0, $soorten.Count)]
        $pSoort.Value = $soortPick.Soort
        $artIds = New-Object System.Collections.Generic.List[int]
        $r2 = $cmdArtBySoort.ExecuteReader()
        while ($r2.Read()) { [void]$artIds.Add([int]$r2['ArtikelID']) }
        $r2.Close()

        $variantCount = [Math]::Min($artIds.Count, $rng.Next(1, 4))
        $picked = $artIds | Get-Random -Count $variantCount

        foreach ($artId in $picked) {
            $aantal = $rng.Next(0, 25)
            $cmdInhoud.Parameters.Clear()
            $cmdInhoud.Parameters.AddWithValue('@V', $voorraadId) | Out-Null
            $cmdInhoud.Parameters.AddWithValue('@A', $artId) | Out-Null
            $cmdInhoud.Parameters.AddWithValue('@Aantal', $aantal) | Out-Null
            $cmdInhoud.ExecuteNonQuery() | Out-Null
        }

        $created++
        if ($i % 50 -eq 0) { Write-Host "  $i bakken..." }
    }

    $tx.Commit()
    Write-Host "Klaar: $created bakken aangemaakt."
}
catch {
    $tx.Rollback()
    throw
}
finally {
    $conn.Close()
}
