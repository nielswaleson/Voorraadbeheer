using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Services;

namespace YourProject
{
    public partial class Alarm : System.Web.UI.Page
    {
        private static string ConnString =>
            ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e) { }

        [WebMethod]
        public static List<AlarmBak> GetAlarmList()
        {
            var bakken = new Dictionary<int, AlarmBak>();

            using (var conn = new SqlConnection(ConnString))
            {
                conn.Open();

                using (var cmd = new SqlCommand(@"
                    SELECT v.VoorraadID, v.Locatie, CAST(v.Naam AS NVARCHAR(MAX)), v.AlarmAantal,
                           vi.VoorraadInhoudID, vi.ArtikelID, vi.Aantal,
                           RTRIM(a.BestelCode), CAST(a.Naam AS NVARCHAR(MAX)),
                           CAST(a.Soort AS NVARCHAR(100)), l.Naam, ISNULL(a.Actief, 1)
                    FROM dbo.VoorraadInhoud vi
                    INNER JOIN dbo.Voorraad v ON v.VoorraadID = vi.VoorraadID
                    INNER JOIN dbo.Artikel a ON a.ArtikelID = vi.ArtikelID
                    INNER JOIN dbo.Leverancier l ON l.LeverancierID = a.LeverancierID
                    ORDER BY v.Locatie, a.BestelCode", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        int voorraadId = r.GetInt32(0);
                        if (!bakken.ContainsKey(voorraadId))
                        {
                            bakken[voorraadId] = new AlarmBak
                            {
                                VoorraadID = voorraadId,
                                Barcode = r.IsDBNull(1) ? "" : r.GetString(1),
                                Naam = r.IsDBNull(2) ? "" : r.GetString(2),
                                AlarmAantal = r.IsDBNull(3) ? 5 : r.GetInt32(3),
                                TotaalAantal = 0,
                                artikelen = new List<AlarmArtikel>()
                            };
                        }

                        var aantal = r.GetInt32(6);
                        var bestel = r.IsDBNull(7) ? "" : r.GetString(7);
                        var artNaam = r.IsDBNull(8) ? "" : r.GetString(8);
                        var bak = bakken[voorraadId];
                        bak.TotaalAantal += aantal;

                        bak.artikelen.Add(new AlarmArtikel
                        {
                            VoorraadInhoudID = r.GetInt32(4),
                            ArtikelID = r.GetInt32(5),
                            BestelCode = bestel,
                            Naam = artNaam,
                            Soort = r.IsDBNull(9) ? "" : r.GetString(9),
                            Leverancier = r.IsDBNull(10) ? "" : r.GetString(10),
                            Aantal = aantal,
                            Actief = !r.IsDBNull(11) && r.GetBoolean(11)
                        });
                    }
                }
            }

            var result = new List<AlarmBak>();
            foreach (var bak in bakken.Values)
            {
                if (bak.TotaalAantal <= bak.AlarmAantal)
                {
                    bak.Tekort = bak.AlarmAantal - bak.TotaalAantal;
                    result.Add(bak);
                }
            }

            result.Sort((a, b) => string.Compare(a.Barcode, b.Barcode, StringComparison.Ordinal));
            return result;
        }

        [WebMethod]
        public static SaveResult SetArtikelActief(int artikelId, bool actief)
        {
            if (artikelId <= 0)
                return new SaveResult { success = false, message = "Geen artikel." };

            try
            {
                using (var conn = new SqlConnection(ConnString))
                using (var cmd = new SqlCommand("UPDATE dbo.Artikel SET Actief = @Actief WHERE ArtikelID = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Actief", actief);
                    cmd.Parameters.AddWithValue("@Id", artikelId);
                    conn.Open();
                    int n = cmd.ExecuteNonQuery();
                    if (n == 0) return new SaveResult { success = false, message = "Artikel niet gevonden." };
                }

                return new SaveResult
                {
                    success = true,
                    message = actief ? "Artikel weer actief." : "Artikel inactief gemaakt."
                };
            }
            catch (Exception ex)
            {
                return new SaveResult { success = false, message = ex.Message };
            }
        }

        public class AlarmBak
        {
            public int VoorraadID { get; set; }
            public string Barcode { get; set; }
            public string Naam { get; set; }
            public int AlarmAantal { get; set; }
            public int TotaalAantal { get; set; }
            public int Tekort { get; set; }
            public List<AlarmArtikel> artikelen { get; set; }
        }

        public class AlarmArtikel
        {
            public int VoorraadInhoudID { get; set; }
            public int ArtikelID { get; set; }
            public string BestelCode { get; set; }
            public string Naam { get; set; }
            public string Soort { get; set; }
            public string Leverancier { get; set; }
            public int Aantal { get; set; }
            public bool Actief { get; set; }
        }

        public class SaveResult
        {
            public bool success { get; set; }
            public string message { get; set; }
        }
    }
}
