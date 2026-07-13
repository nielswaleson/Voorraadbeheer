using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Script.Serialization;
using System.Web.Services;

namespace YourProject
{
    public partial class Voorraad : System.Web.UI.Page
    {
        private static string ConnString =>
            ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e) { }

        [WebMethod]
        public static VoorraadPageData GetPageData()
        {
            var data = new VoorraadPageData
            {
                locaties = new List<LocatieRow>(),
                artikelen = new List<ArtikelLookup>()
            };

            using (var conn = new SqlConnection(ConnString))
            {
                conn.Open();

                using (var cmd = new SqlCommand(@"
                    SELECT v.VoorraadID, v.Locatie, CAST(v.Naam AS NVARCHAR(MAX)), CAST(v.Omschrijving AS NVARCHAR(MAX)),
                           v.AlarmAantal
                    FROM dbo.Voorraad v
                    ORDER BY v.Locatie", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        data.locaties.Add(new LocatieRow
                        {
                            VoorraadID = r.GetInt32(0),
                            Locatie = r.IsDBNull(1) ? "" : r.GetString(1),
                            Naam = r.IsDBNull(2) ? "" : r.GetString(2),
                            Omschrijving = r.IsDBNull(3) ? "" : r.GetString(3),
                            AlarmAantal = r.IsDBNull(4) ? 5 : r.GetInt32(4)
                        });
                    }
                }

                using (var cmd = new SqlCommand(@"
                    SELECT ArtikelID, RTRIM(BestelCode), CAST(Naam AS NVARCHAR(MAX))
                    FROM dbo.Artikel
                    WHERE Actief = 1
                    ORDER BY BestelCode", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var code = r.IsDBNull(1) ? "" : r.GetString(1);
                        var naam = r.IsDBNull(2) ? "" : r.GetString(2);
                        data.artikelen.Add(new ArtikelLookup
                        {
                            ArtikelID = r.GetInt32(0),
                            Label = (string.IsNullOrEmpty(code) ? "" : code + " - ") + naam
                        });
                    }
                }
            }

            return data;
        }

        [WebMethod]
        public static List<InhoudRow> GetInhoud(int voorraadId)
        {
            var list = new List<InhoudRow>();
            if (voorraadId <= 0) return list;

            using (var conn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(@"
                SELECT vi.VoorraadInhoudID, vi.ArtikelID, vi.Aantal,
                       RTRIM(a.BestelCode), CAST(a.Naam AS NVARCHAR(MAX))
                FROM dbo.VoorraadInhoud vi
                INNER JOIN dbo.Artikel a ON a.ArtikelID = vi.ArtikelID
                WHERE vi.VoorraadID = @Id
                ORDER BY a.BestelCode", conn))
            {
                cmd.Parameters.AddWithValue("@Id", voorraadId);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new InhoudRow
                        {
                            VoorraadInhoudID = r.GetInt32(0),
                            ArtikelID = r.GetInt32(1),
                            Aantal = r.GetInt32(2),
                            ArtikelLabel = (r.IsDBNull(3) ? "" : r.GetString(3)) + " - " + (r.IsDBNull(4) ? "" : r.GetString(4))
                        });
                    }
                }
            }

            return list;
        }

        [WebMethod]
        public static SaveResult SaveLocaties(string data)
        {
            try
            {
                var payload = new JavaScriptSerializer().Deserialize<LocatieSavePayload>(data ?? "{}");
                if (payload == null) payload = new LocatieSavePayload();

                using (var conn = new SqlConnection(ConnString))
                {
                    conn.Open();
                    int inserted = 0, updated = 0;

                    if (payload.deletedIds != null)
                    {
                        foreach (var id in payload.deletedIds)
                        {
                            if (id <= 0) continue;
                            using (var cmd = new SqlCommand("DELETE FROM dbo.Voorraad WHERE VoorraadID = @Id", conn))
                            {
                                cmd.Parameters.AddWithValue("@Id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    if (payload.rows != null)
                    {
                        foreach (var row in payload.rows)
                        {
                            if (row == null) continue;
                            var naam = (row.Naam ?? "").Trim();
                            if (string.IsNullOrEmpty(naam) && row.VoorraadID <= 0) continue;

                            if (row.VoorraadID <= 0)
                            {
                                using (var cmd = new SqlCommand(@"
                                    INSERT INTO dbo.Voorraad (Locatie, Naam, Omschrijving, AlarmAantal)
                                    OUTPUT INSERTED.VoorraadID
                                    VALUES ('', @Naam, @Omschrijving, @Alarm)", conn))
                                {
                                    cmd.Parameters.AddWithValue("@Naam", row.Naam ?? "");
                                    cmd.Parameters.AddWithValue("@Omschrijving", row.Omschrijving ?? "");
                                    cmd.Parameters.AddWithValue("@Alarm", row.AlarmAantal > 0 ? row.AlarmAantal : 5);
                                    int newId = Convert.ToInt32(cmd.ExecuteScalar());
                                    string barcode = "L" + newId.ToString("D5");
                                    using (var cmdBc = new SqlCommand(
                                        "UPDATE dbo.Voorraad SET Locatie = @L WHERE VoorraadID = @Id", conn))
                                    {
                                        cmdBc.Parameters.AddWithValue("@L", barcode);
                                        cmdBc.Parameters.AddWithValue("@Id", newId);
                                        cmdBc.ExecuteNonQuery();
                                    }
                                }
                                inserted++;
                            }
                            else
                            {
                                using (var cmd = new SqlCommand(@"
                                    UPDATE dbo.Voorraad
                                    SET Naam = @Naam, Omschrijving = @Omschrijving, AlarmAantal = @Alarm
                                    WHERE VoorraadID = @Id", conn))
                                {
                                    cmd.Parameters.AddWithValue("@Naam", row.Naam ?? "");
                                    cmd.Parameters.AddWithValue("@Omschrijving", row.Omschrijving ?? "");
                                    cmd.Parameters.AddWithValue("@Alarm", row.AlarmAantal > 0 ? row.AlarmAantal : 5);
                                    cmd.Parameters.AddWithValue("@Id", row.VoorraadID);
                                    cmd.ExecuteNonQuery();
                                }
                                updated++;
                            }
                        }
                    }

                    return new SaveResult
                    {
                        success = true,
                        message = string.Format("Locaties opgeslagen ({0} nieuw, {1} bijgewerkt)", inserted, updated)
                    };
                }
            }
            catch (Exception ex)
            {
                return new SaveResult { success = false, message = ex.Message };
            }
        }

        [WebMethod]
        public static SaveResult SaveInhoud(int voorraadId, string data)
        {
            if (voorraadId <= 0)
                return new SaveResult { success = false, message = "Geen locatie geselecteerd." };

            try
            {
                var payload = new JavaScriptSerializer().Deserialize<InhoudSavePayload>(data ?? "{}");
                if (payload == null) payload = new InhoudSavePayload();

                using (var conn = new SqlConnection(ConnString))
                {
                    conn.Open();
                    int inserted = 0, updated = 0;

                    if (payload.deletedIds != null)
                    {
                        foreach (var id in payload.deletedIds)
                        {
                            if (id <= 0) continue;
                            using (var cmd = new SqlCommand(
                                "DELETE FROM dbo.VoorraadInhoud WHERE VoorraadInhoudID = @Id AND VoorraadID = @V", conn))
                            {
                                cmd.Parameters.AddWithValue("@Id", id);
                                cmd.Parameters.AddWithValue("@V", voorraadId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    if (payload.rows != null)
                    {
                        foreach (var row in payload.rows)
                        {
                            if (row == null || row.ArtikelID <= 0) continue;

                            if (row.VoorraadInhoudID <= 0)
                            {
                                using (var cmd = new SqlCommand(@"
                                    INSERT INTO dbo.VoorraadInhoud (VoorraadID, ArtikelID, Aantal)
                                    VALUES (@V, @A, @Aantal)", conn))
                                {
                                    cmd.Parameters.AddWithValue("@V", voorraadId);
                                    cmd.Parameters.AddWithValue("@A", row.ArtikelID);
                                    cmd.Parameters.AddWithValue("@Aantal", row.Aantal);
                                    cmd.ExecuteNonQuery();
                                }
                                inserted++;
                            }
                            else
                            {
                                using (var cmd = new SqlCommand(@"
                                    UPDATE dbo.VoorraadInhoud
                                    SET ArtikelID = @A, Aantal = @Aantal
                                    WHERE VoorraadInhoudID = @Id AND VoorraadID = @V", conn))
                                {
                                    cmd.Parameters.AddWithValue("@A", row.ArtikelID);
                                    cmd.Parameters.AddWithValue("@Aantal", row.Aantal);
                                    cmd.Parameters.AddWithValue("@Id", row.VoorraadInhoudID);
                                    cmd.Parameters.AddWithValue("@V", voorraadId);
                                    cmd.ExecuteNonQuery();
                                }
                                updated++;
                            }
                        }
                    }

                    return new SaveResult
                    {
                        success = true,
                        message = string.Format("Inhoud opgeslagen ({0} nieuw, {1} bijgewerkt)", inserted, updated)
                    };
                }
            }
            catch (Exception ex)
            {
                return new SaveResult { success = false, message = ex.Message };
            }
        }

        public class VoorraadPageData
        {
            public List<LocatieRow> locaties { get; set; }
            public List<ArtikelLookup> artikelen { get; set; }
        }

        public class LocatieRow
        {
            public int VoorraadID { get; set; }
            public string Locatie { get; set; }
            public string Naam { get; set; }
            public string Omschrijving { get; set; }
            public int AlarmAantal { get; set; }
        }

        public class InhoudRow
        {
            public int VoorraadInhoudID { get; set; }
            public int ArtikelID { get; set; }
            public int Aantal { get; set; }
            public string ArtikelLabel { get; set; }
        }

        public class ArtikelLookup
        {
            public int ArtikelID { get; set; }
            public string Label { get; set; }
        }

        public class LocatieSavePayload
        {
            public List<LocatieRow> rows { get; set; }
            public List<int> deletedIds { get; set; }
        }

        public class InhoudSavePayload
        {
            public List<InhoudRow> rows { get; set; }
            public List<int> deletedIds { get; set; }
        }

        public class SaveResult
        {
            public bool success { get; set; }
            public string message { get; set; }
        }
    }
}
