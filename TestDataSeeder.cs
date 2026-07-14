using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace YourProject
{
    public static class TestDataSeeder
    {
        private static readonly byte[] JpegPlaceholder = Convert.FromBase64String(
            "/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDAREAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAb/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAgP/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwCfAA//2Q==");

        private static readonly string[] LeverancierNamen =
        {
            "TechnoSupply BV", "MetaalGroothandel Jansen", "Elektro Parts Nederland", "BouwDirect XL",
            "Industrie Hub Noord", "LogiTrade Europe", "PakMaterialen Plus", "Staal & Co Rotterdam",
            "Hydrauliek Centrum", "Office Pro Groep", "Chemie Depot BV", "AutoParts Benelux",
            "Garden & Tools NL", "FoodService Levering", "Klimaat Techniek BV", "VeiligheidShop",
            "Print & Label Solutions", "Sanitair Groothandel", "IT Hardware Direct", "Textiel Import NL",
            "Farmaceutica Supply", "Energie Componenten", "Plastics United", "TimmerGroothandel De Vries",
            "Maritiem Parts Center", "Sport & Leisure BV", "Meubel Import Europa", "Papier & Karton NL",
            "Licht & Elektra Groep", "Algemene Groothandel Meijer"
        };

        private static readonly string[] ArtikelPrefixes =
        {
            "Schroef", "Moer", "Bout", "Pakking", "Filter", "Kabel", "Sensor", "Klep", "Motor", "Pomp",
            "Lager", "Seal", "Plaat", "Buis", "Connector", "Schakelaar", "Relais", "Display", "Behuizing", "Kit"
        };

        private static readonly string[] ArtikelSuffixes =
        {
            "M6", "M8", "M10", "200mm", "500ml", "1m", "XL", "Pro", "Standard", "Heavy Duty", "RVS", "Brons", "Koper"
        };

        private static string ConnString =>
            ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public static SeedResult Seed(bool clearFirst)
        {
            DatabaseBootstrap.EnsureDatabase();

            try
            {
                using (var conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    if (!clearFirst && HasData(conn))
                    {
                        return new SeedResult
                        {
                            success = false,
                            message = "Database bevat al data. Vink 'Eerst alle data wissen' aan om opnieuw te vullen."
                        };
                    }

                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            if (clearFirst)
                                ClearAll(conn, tx);

                            var stats = new SeedStats();
                            SeedLeveranciers(conn, tx, stats);
                            SeedArtikelen(conn, tx, stats);
                            SeedFotos(conn, tx, stats);
                            SeedPersonen(conn, tx, stats);
                            SeedBakken(conn, tx, stats);

                            tx.Commit();
                            return new SeedResult { success = true, message = stats.ToString() };
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new SeedResult { success = false, message = ex.Message };
            }
        }

        private static bool HasData(SqlConnection conn)
        {
            using (var cmd = new SqlCommand(@"
                SELECT CASE WHEN
                    (SELECT COUNT(*) FROM dbo.Leverancier) > 0 OR
                    (SELECT COUNT(*) FROM dbo.Artikel) > 0 OR
                    (SELECT COUNT(*) FROM dbo.Voorraad) > 0
                THEN 1 ELSE 0 END", conn))
                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
        }

        private static void ClearAll(SqlConnection conn, SqlTransaction tx)
        {
            Exec(conn, tx, @"
                DELETE FROM dbo.Log;
                DELETE FROM dbo.Foto;
                DELETE FROM dbo.VoorraadInhoud;
                DELETE FROM dbo.Voorraad;
                DELETE FROM dbo.Artikel;
                DELETE FROM dbo.Leverancier;
                DELETE FROM dbo.Persoon;");
        }

        private static void SeedLeveranciers(SqlConnection conn, SqlTransaction tx, SeedStats stats)
        {
            foreach (var naam in LeverancierNamen)
            {
                using (var cmd = new SqlCommand(
                    "INSERT INTO dbo.Leverancier (Naam, Actief) VALUES (@Naam, 1)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Naam", naam);
                    cmd.ExecuteNonQuery();
                    stats.Leveranciers++;
                }
            }
        }

        private static void SeedArtikelen(SqlConnection conn, SqlTransaction tx, SeedStats stats)
        {
            var leverancierIds = new List<int>();
            using (var cmd = new SqlCommand("SELECT LeverancierID FROM dbo.Leverancier ORDER BY LeverancierID", conn, tx))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read()) leverancierIds.Add(r.GetInt32(0));
            }

            if (leverancierIds.Count == 0)
                throw new InvalidOperationException("Geen leveranciers om artikelen aan te koppelen.");

            var rng = new Random(42);
            for (int i = 1; i <= 1000; i++)
            {
                int levId = leverancierIds[rng.Next(leverancierIds.Count)];
                string prefix = ArtikelPrefixes[rng.Next(ArtikelPrefixes.Length)];
                string suffix = ArtikelSuffixes[rng.Next(ArtikelSuffixes.Length)];
                string soort = prefix + " " + suffix;
                string naam = soort + " #" + i;
                string oms = "Testartikel " + i;
                string code = "ART" + i.ToString("D5");

                using (var cmd = new SqlCommand(@"
                    INSERT INTO dbo.Artikel (LeverancierID, Naam, Omschrijving, BestelCode, Soort, Actief)
                    VALUES (@Lev, @Naam, @Oms, @Code, @Soort, 1)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Lev", levId);
                    cmd.Parameters.AddWithValue("@Naam", naam);
                    cmd.Parameters.AddWithValue("@Oms", oms);
                    cmd.Parameters.AddWithValue("@Code", code);
                    cmd.Parameters.AddWithValue("@Soort", soort);
                    cmd.ExecuteNonQuery();
                    stats.Artikelen++;
                }
            }
        }

        private static void SeedFotos(SqlConnection conn, SqlTransaction tx, SeedStats stats)
        {
            var artikelIds = new List<int>();
            using (var cmd = new SqlCommand("SELECT ArtikelID FROM dbo.Artikel ORDER BY ArtikelID", conn, tx))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read()) artikelIds.Add(r.GetInt32(0));
            }

            var rng = new Random(42);
            foreach (int artikelId in artikelIds)
            {
                int n = rng.Next(0, 6);
                for (int f = 0; f < n; f++)
                {
                    using (var cmd = new SqlCommand(
                        "INSERT INTO dbo.Foto (Foto, ArtikelID, Tijd) VALUES (@Foto, @ArtikelID, @Tijd)", conn, tx))
                    {
                        cmd.Parameters.Add("@Foto", System.Data.SqlDbType.Image).Value = JpegPlaceholder;
                        cmd.Parameters.AddWithValue("@ArtikelID", artikelId);
                        cmd.Parameters.AddWithValue("@Tijd", DateTime.Now.AddDays(-rng.Next(0, 365)).AddMinutes(-rng.Next(0, 1440)));
                        cmd.ExecuteNonQuery();
                        stats.Fotos++;
                    }
                }
            }
        }

        private static void SeedPersonen(SqlConnection conn, SqlTransaction tx, SeedStats stats)
        {
            var namen = new[] { "Niels Waleson", "William Waleson", "Jan de Vries", "Maria Jansen", "Pieter Bakker" };
            foreach (var naam in namen)
            {
                using (var cmd = new SqlCommand(
                    "INSERT INTO dbo.Persoon (Naam, Actief) OUTPUT INSERTED.PersoonID VALUES (@Naam, 1)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Naam", naam);
                    int id = Convert.ToInt32(cmd.ExecuteScalar());
                    using (var cmdBc = new SqlCommand(
                        "UPDATE dbo.Persoon SET Barcode = @Bc WHERE PersoonID = @Id", conn, tx))
                    {
                        cmdBc.Parameters.AddWithValue("@Bc", "P" + id);
                        cmdBc.Parameters.AddWithValue("@Id", id);
                        cmdBc.ExecuteNonQuery();
                    }
                    stats.Personen++;
                }
            }
        }

        private static void SeedBakken(SqlConnection conn, SqlTransaction tx, SeedStats stats)
        {
            var soorten = new List<string>();
            using (var cmd = new SqlCommand(@"
                SELECT Soort FROM dbo.Artikel
                WHERE Actief = 1 AND Soort IS NOT NULL AND RTRIM(Soort) <> ''
                GROUP BY Soort HAVING COUNT(*) >= 2
                ORDER BY Soort", conn, tx))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read()) soorten.Add(r.GetString(0));
            }

            if (soorten.Count < 10)
                throw new InvalidOperationException("Te weinig artikelsoorten voor bakken.");

            var rng = new Random(99);
            const int binCount = 150;

            for (int i = 1; i <= binCount; i++)
            {
                int alarm = rng.Next(3, 12);
                int voorraadId;
                using (var cmd = new SqlCommand(@"
                    INSERT INTO dbo.Voorraad (Locatie, Naam, Omschrijving, AlarmAantal)
                    OUTPUT INSERTED.VoorraadID
                    VALUES ('', @Naam, @Oms, @Alarm)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Naam", "Bak " + i);
                    cmd.Parameters.AddWithValue("@Oms", "Magazijnbak " + i);
                    cmd.Parameters.AddWithValue("@Alarm", alarm);
                    voorraadId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string barcode = "L" + voorraadId.ToString("D5");
                using (var cmd = new SqlCommand(
                    "UPDATE dbo.Voorraad SET Locatie = @L WHERE VoorraadID = @Id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@L", barcode);
                    cmd.Parameters.AddWithValue("@Id", voorraadId);
                    cmd.ExecuteNonQuery();
                }
                stats.Bakken++;

                string soort = soorten[rng.Next(soorten.Count)];
                var artikelIds = new List<int>();
                using (var cmd = new SqlCommand(
                    "SELECT ArtikelID FROM dbo.Artikel WHERE Soort = @Soort AND Actief = 1 ORDER BY ArtikelID", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Soort", soort);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read()) artikelIds.Add(r.GetInt32(0));
                    }
                }

                int variantCount = Math.Min(artikelIds.Count, rng.Next(1, 4));
                var picked = artikelIds.OrderBy(_ => rng.Next()).Take(variantCount).ToList();

                foreach (int artikelId in picked)
                {
                    using (var cmd = new SqlCommand(@"
                        INSERT INTO dbo.VoorraadInhoud (VoorraadID, ArtikelID, Aantal)
                        VALUES (@V, @A, @Aantal)", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@V", voorraadId);
                        cmd.Parameters.AddWithValue("@A", artikelId);
                        cmd.Parameters.AddWithValue("@Aantal", rng.Next(0, 25));
                        cmd.ExecuteNonQuery();
                        stats.InhoudRegels++;
                    }
                }
            }
        }

        private static void Exec(SqlConnection conn, SqlTransaction tx, string sql)
        {
            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.CommandTimeout = 120;
                cmd.ExecuteNonQuery();
            }
        }

        public class SeedResult
        {
            public bool success { get; set; }
            public string message { get; set; }
        }

        private class SeedStats
        {
            public int Leveranciers;
            public int Artikelen;
            public int Fotos;
            public int Personen;
            public int Bakken;
            public int InhoudRegels;

            public override string ToString()
            {
                var sb = new StringBuilder("Testdata geladen: ");
                sb.Append(Leveranciers).Append(" leveranciers, ");
                sb.Append(Artikelen).Append(" artikelen, ");
                sb.Append(Fotos).Append(" fotos, ");
                sb.Append(Personen).Append(" personen, ");
                sb.Append(Bakken).Append(" bakken (");
                sb.Append(InhoudRegels).Append(" inhoudregels).");
                return sb.ToString();
            }
        }
    }
}
