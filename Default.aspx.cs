using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Web.Services;

namespace YourProject
{
    public partial class Default : System.Web.UI.Page
    {
        private static string ConnString =>
            ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e) { }

        [WebMethod]
        public static ScanResult ResolveBarcode(string code)
        {
            code = (code ?? "").Trim();
            if (string.IsNullOrEmpty(code))
                return ScanResult.Fail("Lege barcode.");

            var upper = code.ToUpperInvariant();

            if (IsActionBarcode(upper))
                return new ScanResult { success = true, scanType = "actie_af", message = "Actie: 1 afnemen" };

            var persoonMatch = Regex.Match(upper, @"^P(?:ERSOON)?[-:]?(\d+)$");
            if (persoonMatch.Success)
                return LoadPersoon(int.Parse(persoonMatch.Groups[1].Value));

            var bakMatch = Regex.Match(upper, @"^L(?:OC)?[-:]?(\d+)$");
            if (bakMatch.Success)
                return LoadLocatie(int.Parse(bakMatch.Groups[1].Value));

            var byPersoonBarcode = FindPersoonByBarcode(code);
            if (byPersoonBarcode != null) return byPersoonBarcode;

            var byBak = FindLocatieByCode(code);
            if (byBak != null) return byBak;

            return ScanResult.Fail("Onbekende barcode: " + code + " (P=persoon, L=bak, A=actie)");
        }

        [WebMethod]
        public static BakArtikelenResult GetBakArtikelen(int voorraadId)
        {
            var result = new BakArtikelenResult { artikelen = new List<ArtikelDetail>() };
            if (voorraadId <= 0)
            {
                result.success = false;
                result.message = "Geen bak geselecteerd.";
                return result;
            }

            using (var conn = new SqlConnection(ConnString))
            {
                conn.Open();
                if (!LocatieExists(conn, voorraadId))
                {
                    result.success = false;
                    result.message = "Bak niet gevonden.";
                    return result;
                }

                var ids = new List<int>();
                using (var cmd = new SqlCommand(@"
                    SELECT vi.ArtikelID
                    FROM dbo.VoorraadInhoud vi
                    INNER JOIN dbo.Artikel a ON a.ArtikelID = vi.ArtikelID
                    WHERE vi.VoorraadID = @Id
                    ORDER BY vi.Aantal, a.BestelCode", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", voorraadId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read()) ids.Add(r.GetInt32(0));
                    }
                }

                foreach (var artikelId in ids)
                    result.artikelen.Add(BuildArtikelDetail(conn, voorraadId, artikelId));

                if (result.artikelen.Count > 0)
                {
                    result.soort = result.artikelen[0].soort ?? "";
                    result.alarmAantal = result.artikelen[0].alarmAantal;
                    result.bakTotaal = result.artikelen[0].bakTotaal;
                    result.isAlarm = result.artikelen[0].isAlarm;
                }
            }

            result.success = true;
            result.message = result.artikelen.Count + " artikel(en) in bak";
            return result;
        }

        [WebMethod]
        public static ScanResult GetArtikelAtLocatie(int voorraadId, int artikelId)
        {
            if (voorraadId <= 0) return ScanResult.Fail("Scan eerst een bak (L...).");
            if (artikelId <= 0) return ScanResult.Fail("Geen artikel.");

            using (var conn = new SqlConnection(ConnString))
            {
                conn.Open();
                if (!LocatieExists(conn, voorraadId))
                    return ScanResult.Fail("Bak niet gevonden.");
                if (!ArtikelExists(conn, artikelId))
                    return ScanResult.Fail("Artikel niet gevonden.");
                if (!ArtikelInBak(conn, voorraadId, artikelId))
                    return ScanResult.Fail("Artikel is niet toegewezen aan deze bak.");

                var detail = BuildArtikelDetail(conn, voorraadId, artikelId);
                if (!detail.actief)
                    return ScanResult.Fail("Artikel is inactief.");

                return new ScanResult
                {
                    success = true,
                    scanType = "artikel",
                    artikel = detail,
                    message = detail.naam + " @ " + detail.locatie
                };
            }
        }

        [WebMethod]
        public static ActionResult AdjustVoorraad(int persoonId, int voorraadId, int artikelId, string mode, int amount)
        {
            if (persoonId <= 0) return ActionResult.Fail("Scan eerst een persoon (P...).");
            if (voorraadId <= 0) return ActionResult.Fail("Scan eerst een bak (L...).");
            if (artikelId <= 0) return ActionResult.Fail("Geen artikel geselecteerd.");
            if (amount < 0) return ActionResult.Fail("Ongeldig aantal.");

            mode = (mode ?? "").ToLowerInvariant();

            try
            {
                using (var conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    if (!IsActivePersoon(conn, persoonId))
                        return ActionResult.Fail("Persoon niet gevonden of niet actief.");
                    if (!LocatieExists(conn, voorraadId))
                        return ActionResult.Fail("Bak niet gevonden.");
                    if (!ArtikelExists(conn, artikelId))
                        return ActionResult.Fail("Artikel niet gevonden.");
                    if (!ArtikelInBak(conn, voorraadId, artikelId))
                        return ActionResult.Fail("Artikel is niet toegewezen aan deze bak.");

                    int inhoudId = GetInhoudId(conn, voorraadId, artikelId);
                    if (inhoudId <= 0)
                        return ActionResult.Fail("Geen voorraadregel voor dit artikel in de bak.");
                    int huidig = GetInhoudAantal(conn, inhoudId);
                    int nieuw = huidig;
                    string actie;

                    switch (mode)
                    {
                        case "remove":
                            if (amount <= 0) amount = 1;
                            nieuw = huidig - amount;
                            if (nieuw < 0) return ActionResult.Fail("Onvoldoende voorraad (huidig: " + huidig + ").");
                            actie = amount == 1 ? "Afnemen 1" : "Afnemen " + amount;
                            break;
                        case "add":
                            if (amount <= 0) amount = 1;
                            nieuw = huidig + amount;
                            actie = amount == 1 ? "Toevoegen 1" : "Toevoegen " + amount;
                            break;
                        case "set":
                            nieuw = amount;
                            actie = "Correctie naar " + amount;
                            break;
                        default:
                            return ActionResult.Fail("Onbekende actie.");
                    }

                    using (var cmd = new SqlCommand("UPDATE dbo.VoorraadInhoud SET Aantal = @Aantal WHERE VoorraadInhoudID = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Aantal", nieuw);
                        cmd.Parameters.AddWithValue("@Id", inhoudId);
                        cmd.ExecuteNonQuery();
                    }

                    string locLabel = GetLocatieLabel(conn, voorraadId);
                    InsertLog(conn, persoonId, voorraadId, artikelId,
                        actie + " @ " + locLabel + " (" + huidig + " -> " + nieuw + ")");

                    var detail = BuildArtikelDetail(conn, voorraadId, artikelId);
                    return new ActionResult
                    {
                        success = true,
                        message = actie + " @ " + locLabel + ". Voorraad: " + nieuw,
                        aantal = nieuw,
                        artikel = detail
                    };
                }
            }
            catch (Exception ex)
            {
                return ActionResult.Fail(ex.Message);
            }
        }

        private static bool IsActionBarcode(string upper)
        {
            if (upper == "A-AF" || upper == "A-AFNEMEN" || upper == "A-VERWIJDER" || upper == "A-REMOVE" || upper == "A-1")
                return true;
            if (upper.StartsWith("A") && (upper.Contains("AF") || upper.Contains("VERWIJDER") || upper.Contains("REMOVE")))
                return true;
            // Oud formaat (achterwaarts compatibel)
            return upper == "ACT-AF" || upper == "AF" || upper == "AF-1" || upper == "-1";
        }

        private static ScanResult FindPersoonByBarcode(string code)
        {
            using (var conn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(
                "SELECT PersoonID FROM dbo.Persoon WHERE Barcode = @Code", conn))
            {
                cmd.Parameters.AddWithValue("@Code", code.Trim());
                conn.Open();
                var id = cmd.ExecuteScalar();
                if (id == null) return null;
                return LoadPersoon(Convert.ToInt32(id));
            }
        }

        private static ScanResult LoadPersoon(int persoonId)
        {
            using (var conn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(
                "SELECT Naam, Actief, Barcode FROM dbo.Persoon WHERE PersoonID = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", persoonId);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return ScanResult.Fail("Persoon P" + persoonId + " niet gevonden.");
                    if (r.IsDBNull(1) || !r.GetBoolean(1)) return ScanResult.Fail("Persoon is niet actief.");
                    var naam = r.IsDBNull(0) ? "" : r.GetString(0);
                    var barcode = r.IsDBNull(2) ? "P" + persoonId : r.GetString(2);
                    return new ScanResult
                    {
                        success = true,
                        scanType = "persoon",
                        persoonId = persoonId,
                        persoonNaam = naam,
                        message = "Persoon: " + naam + " (" + barcode + ")"
                    };
                }
            }
        }

        private static ScanResult LoadLocatie(int voorraadId)
        {
            using (var conn = new SqlConnection(ConnString))
            {
                conn.Open();
                var loc = GetLocatieInfo(conn, voorraadId);
                if (loc == null) return ScanResult.Fail("Bak L" + voorraadId + " niet gevonden.");
                return new ScanResult
                {
                    success = true,
                    scanType = "locatie",
                    voorraadId = loc.voorraadId,
                    locatie = loc.locatie,
                    locatieNaam = loc.naam,
                    message = "Bak: " + loc.locatie + " - " + loc.naam
                };
            }
        }

        private static ScanResult FindLocatieByCode(string code)
        {
            using (var conn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(
                "SELECT VoorraadID FROM dbo.Voorraad WHERE Locatie = @Code", conn))
            {
                cmd.Parameters.AddWithValue("@Code", code.Trim());
                conn.Open();
                var id = cmd.ExecuteScalar();
                if (id == null) return null;
                return LoadLocatie(Convert.ToInt32(id));
            }
        }

        private static ScanResult LoadArtikelScan(int artikelId)
        {
            using (var conn = new SqlConnection(ConnString))
            {
                conn.Open();
                if (!ArtikelExists(conn, artikelId))
                    return ScanResult.Fail("Artikel A" + artikelId + " niet gevonden.");
                return new ScanResult
                {
                    success = true,
                    scanType = "artikel",
                    artikel = new ArtikelDetail { artikelId = artikelId },
                    message = "Artikel gescand (selecteer locatie indien nodig)"
                };
            }
        }

        private static ScanResult FindArtikelByBestelCode(string code)
        {
            using (var conn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(
                "SELECT ArtikelID FROM dbo.Artikel WHERE RTRIM(BestelCode) = @Code AND Actief = 1", conn))
            {
                cmd.Parameters.AddWithValue("@Code", code.Trim());
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result == null) return null;
                return LoadArtikelScan(Convert.ToInt32(result));
            }
        }

        public static ArtikelDetail BuildArtikelDetail(SqlConnection conn, int voorraadId, int artikelId)
        {
            var detail = new ArtikelDetail { artikelId = artikelId, voorraadId = voorraadId };

            using (var cmd = new SqlCommand(@"
                SELECT a.Naam, a.Omschrijving, RTRIM(a.BestelCode), l.Naam,
                       ISNULL(vi.Aantal, 0), v.Locatie, CAST(v.Naam AS NVARCHAR(MAX)),
                       ISNULL(a.Actief, 1), CAST(a.Soort AS NVARCHAR(100)),
                       ISNULL(v.AlarmAantal, 5),
                       ISNULL((SELECT SUM(vi2.Aantal) FROM dbo.VoorraadInhoud vi2 WHERE vi2.VoorraadID = @VoorraadID), 0)
                FROM dbo.Artikel a
                INNER JOIN dbo.Leverancier l ON l.LeverancierID = a.LeverancierID
                LEFT JOIN dbo.Voorraad v ON v.VoorraadID = @VoorraadID
                LEFT JOIN dbo.VoorraadInhoud vi ON vi.VoorraadID = @VoorraadID AND vi.ArtikelID = a.ArtikelID
                WHERE a.ArtikelID = @ArtikelID", conn))
            {
                cmd.Parameters.AddWithValue("@VoorraadID", voorraadId);
                cmd.Parameters.AddWithValue("@ArtikelID", artikelId);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        detail.naam = r.IsDBNull(0) ? "" : r.GetString(0);
                        detail.omschrijving = r.IsDBNull(1) ? "" : r.GetString(1);
                        detail.bestelCode = r.IsDBNull(2) ? "" : r.GetString(2);
                        detail.leverancier = r.IsDBNull(3) ? "" : r.GetString(3);
                        detail.aantal = r.IsDBNull(4) ? 0 : r.GetInt32(4);
                        detail.locatie = r.IsDBNull(5) ? "" : r.GetString(5);
                        detail.locatieNaam = r.IsDBNull(6) ? "" : r.GetString(6);
                        detail.actief = !r.IsDBNull(7) && r.GetBoolean(7);
                        detail.soort = r.IsDBNull(8) ? "" : r.GetString(8);
                        detail.alarmAantal = r.IsDBNull(9) ? 5 : r.GetInt32(9);
                        detail.bakTotaal = r.IsDBNull(10) ? 0 : r.GetInt32(10);
                    }
                }
            }

            using (var cmd = new SqlCommand(
                "SELECT TOP 1 FotoID FROM dbo.Foto WHERE ArtikelID = @Id ORDER BY Tijd DESC", conn))
            {
                cmd.Parameters.AddWithValue("@Id", artikelId);
                var fotoId = cmd.ExecuteScalar();
                if (fotoId != null)
                    detail.fotoUrl = "ArtikelFoto.ashx?id=" + fotoId;
            }

            detail.isAlarm = detail.bakTotaal <= detail.alarmAantal;
            return detail;
        }

        private static LocatieInfo GetLocatieInfo(SqlConnection conn, int voorraadId)
        {
            using (var cmd = new SqlCommand(
                "SELECT VoorraadID, Locatie, CAST(Naam AS NVARCHAR(MAX)) FROM dbo.Voorraad WHERE VoorraadID = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", voorraadId);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new LocatieInfo
                    {
                        voorraadId = r.GetInt32(0),
                        locatie = r.IsDBNull(1) ? "" : r.GetString(1),
                        naam = r.IsDBNull(2) ? "" : r.GetString(2)
                    };
                }
            }
        }

        private static string GetLocatieLabel(SqlConnection conn, int voorraadId)
        {
            var loc = GetLocatieInfo(conn, voorraadId);
            return loc == null ? "?" : loc.locatie;
        }

        private static bool IsActivePersoon(SqlConnection conn, int persoonId)
        {
            using (var cmd = new SqlCommand("SELECT 1 FROM dbo.Persoon WHERE PersoonID = @Id AND Actief = 1", conn))
            {
                cmd.Parameters.AddWithValue("@Id", persoonId);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static bool LocatieExists(SqlConnection conn, int voorraadId)
        {
            using (var cmd = new SqlCommand("SELECT 1 FROM dbo.Voorraad WHERE VoorraadID = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", voorraadId);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static bool ArtikelExists(SqlConnection conn, int artikelId)
        {
            using (var cmd = new SqlCommand("SELECT 1 FROM dbo.Artikel WHERE ArtikelID = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", artikelId);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static bool ArtikelInBak(SqlConnection conn, int voorraadId, int artikelId)
        {
            using (var cmd = new SqlCommand(
                "SELECT 1 FROM dbo.VoorraadInhoud WHERE VoorraadID = @V AND ArtikelID = @A", conn))
            {
                cmd.Parameters.AddWithValue("@V", voorraadId);
                cmd.Parameters.AddWithValue("@A", artikelId);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static int GetInhoudId(SqlConnection conn, int voorraadId, int artikelId)
        {
            using (var cmd = new SqlCommand(
                "SELECT VoorraadInhoudID FROM dbo.VoorraadInhoud WHERE VoorraadID = @V AND ArtikelID = @A", conn))
            {
                cmd.Parameters.AddWithValue("@V", voorraadId);
                cmd.Parameters.AddWithValue("@A", artikelId);
                var existing = cmd.ExecuteScalar();
                return existing == null ? 0 : Convert.ToInt32(existing);
            }
        }

        private static int GetInhoudAantal(SqlConnection conn, int inhoudId)
        {
            using (var cmd = new SqlCommand("SELECT Aantal FROM dbo.VoorraadInhoud WHERE VoorraadInhoudID = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", inhoudId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void InsertLog(SqlConnection conn, int persoonId, int voorraadId, int artikelId, string actie)
        {
            using (var cmd = new SqlCommand(@"
                INSERT INTO dbo.Log (Actie, Tijd, PersoonID, VoorraadID, ArtikelID)
                VALUES (@Actie, SYSDATETIME(), @PersoonID, @VoorraadID, @ArtikelID)", conn))
            {
                cmd.Parameters.AddWithValue("@Actie", actie);
                cmd.Parameters.AddWithValue("@PersoonID", persoonId);
                cmd.Parameters.AddWithValue("@VoorraadID", voorraadId);
                cmd.Parameters.AddWithValue("@ArtikelID", artikelId);
                cmd.ExecuteNonQuery();
            }
        }

        private static ScanResult FailScan(string msg)
        {
            return new ScanResult { success = false, message = msg };
        }

        private static ActionResult FailAction(string msg)
        {
            return new ActionResult { success = false, message = msg };
        }

        public class ScanResult
        {
            public bool success { get; set; }
            public string scanType { get; set; }
            public string message { get; set; }
            public int persoonId { get; set; }
            public string persoonNaam { get; set; }
            public int voorraadId { get; set; }
            public string locatie { get; set; }
            public string locatieNaam { get; set; }
            public ArtikelDetail artikel { get; set; }

            public static ScanResult Fail(string msg) => new ScanResult { success = false, message = msg };
        }

        public class ActionResult
        {
            public bool success { get; set; }
            public string message { get; set; }
            public int aantal { get; set; }
            public ArtikelDetail artikel { get; set; }

            public static ActionResult Fail(string msg) => new ActionResult { success = false, message = msg };
        }

        public class ArtikelDetail
        {
            public int artikelId { get; set; }
            public int voorraadId { get; set; }
            public string naam { get; set; }
            public string omschrijving { get; set; }
            public string bestelCode { get; set; }
            public string leverancier { get; set; }
            public string locatie { get; set; }
            public string locatieNaam { get; set; }
            public int aantal { get; set; }
            public int bakTotaal { get; set; }
            public int alarmAantal { get; set; }
            public bool isAlarm { get; set; }
            public bool actief { get; set; }
            public string soort { get; set; }
            public string fotoUrl { get; set; }
        }

        public class LocatieInfo
        {
            public int voorraadId { get; set; }
            public string locatie { get; set; }
            public string naam { get; set; }
        }

        public class BakArtikelenResult
        {
            public bool success { get; set; }
            public string message { get; set; }
            public string soort { get; set; }
            public int alarmAantal { get; set; }
            public int bakTotaal { get; set; }
            public bool isAlarm { get; set; }
            public List<ArtikelDetail> artikelen { get; set; }
        }
    }
}
