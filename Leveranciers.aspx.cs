using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Script.Serialization;
using System.Web.Services;

namespace YourProject
{
    public partial class Leveranciers : System.Web.UI.Page
    {
        private static string ConnString =>
            ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        [WebMethod]
        public static List<LeverancierRow> GetLeveranciers()
        {
            var list = new List<LeverancierRow>();

            using (var conn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand("SELECT LeverancierID, Naam, Actief FROM dbo.Leverancier ORDER BY LeverancierID", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new LeverancierRow
                        {
                            LeverancierID = reader.GetInt32(0),
                            Naam = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Actief = !reader.IsDBNull(2) && reader.GetBoolean(2)
                        });
                    }
                }
            }

            return list;
        }

        [WebMethod]
        public static SaveResult SaveChanges(string data)
        {
            try
            {
                var payload = new JavaScriptSerializer().Deserialize<SavePayload>(data ?? "{}");
                if (payload == null) payload = new SavePayload();

                using (var conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    if (payload.deletedIds != null)
                    {
                        foreach (var id in payload.deletedIds)
                        {
                            if (id <= 0) continue;
                            using (var cmd = new SqlCommand("DELETE FROM dbo.Leverancier WHERE LeverancierID = @Id", conn))
                            {
                                cmd.Parameters.AddWithValue("@Id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    int inserted = 0, updated = 0;

                    if (payload.rows != null)
                    {
                        foreach (var row in payload.rows)
                        {
                            if (row == null) continue;
                            var naam = (row.Naam ?? "").Trim();
                            if (string.IsNullOrEmpty(naam) && row.LeverancierID <= 0) continue;

                            if (row.LeverancierID <= 0)
                            {
                                using (var cmd = new SqlCommand(
                                    "INSERT INTO dbo.Leverancier (Naam, Actief) VALUES (@Naam, @Actief)", conn))
                                {
                                    cmd.Parameters.AddWithValue("@Naam", naam);
                                    cmd.Parameters.AddWithValue("@Actief", row.Actief);
                                    cmd.ExecuteNonQuery();
                                }
                                inserted++;
                            }
                            else
                            {
                                using (var cmd = new SqlCommand(
                                    "UPDATE dbo.Leverancier SET Naam = @Naam, Actief = @Actief WHERE LeverancierID = @Id", conn))
                                {
                                    cmd.Parameters.AddWithValue("@Naam", naam);
                                    cmd.Parameters.AddWithValue("@Actief", row.Actief);
                                    cmd.Parameters.AddWithValue("@Id", row.LeverancierID);
                                    cmd.ExecuteNonQuery();
                                }
                                updated++;
                            }
                        }
                    }

                    return new SaveResult
                    {
                        success = true,
                        message = string.Format("Opgeslagen ({0} nieuw, {1} bijgewerkt)", inserted, updated)
                    };
                }
            }
            catch (Exception ex)
            {
                return new SaveResult { success = false, message = ex.Message };
            }
        }

        public class LeverancierRow
        {
            public int LeverancierID { get; set; }
            public string Naam { get; set; }
            public bool Actief { get; set; }
        }

        public class SavePayload
        {
            public List<LeverancierRow> rows { get; set; }
            public List<int> deletedIds { get; set; }
        }

        public class SaveResult
        {
            public bool success { get; set; }
            public string message { get; set; }
        }
    }
}
