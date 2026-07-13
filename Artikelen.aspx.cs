using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Script.Serialization;
using System.Web.Services;

namespace YourProject
{
    public partial class Artikelen : System.Web.UI.Page
    {
        private const int DefaultPageSize = 100;
        private const int MaxPageSize = 500;

        private static string ConnString =>
            ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        [WebMethod]
        public static PageData GetPageData(int leverancierId, string search, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = DefaultPageSize;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            search = (search ?? "").Trim();
            int skip = (page - 1) * pageSize;

            var data = new PageData
            {
                leveranciers = new List<LeverancierLookup>(),
                artikelen = new List<ArtikelRow>(),
                page = page,
                pageSize = pageSize
            };

            using (var conn = new SqlConnection(ConnString))
            {
                conn.Open();

                using (var cmd = new SqlCommand(
                    "SELECT LeverancierID, Naam FROM dbo.Leverancier WHERE Actief = 1 ORDER BY CAST(Naam AS NVARCHAR(MAX))", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        data.leveranciers.Add(new LeverancierLookup
                        {
                            LeverancierID = reader.GetInt32(0),
                            Naam = reader.IsDBNull(1) ? "" : reader.GetString(1)
                        });
                    }
                }

                string where = @"
                    WHERE (@LeverancierId = 0 OR a.LeverancierID = @LeverancierId)
                      AND (@Search = '' OR CAST(a.Naam AS NVARCHAR(MAX)) LIKE @SearchLike
                           OR CAST(a.Omschrijving AS NVARCHAR(MAX)) LIKE @SearchLike
                           OR RTRIM(a.BestelCode) LIKE @SearchLike)";

                using (var cmdCount = new SqlCommand("SELECT COUNT(*) FROM dbo.Artikel a " + where, conn))
                {
                    cmdCount.Parameters.AddWithValue("@LeverancierId", leverancierId);
                    cmdCount.Parameters.AddWithValue("@Search", search);
                    cmdCount.Parameters.AddWithValue("@SearchLike", "%" + search + "%");
                    data.totalCount = Convert.ToInt32(cmdCount.ExecuteScalar());
                }

                using (var cmd = new SqlCommand(@"
                    SELECT a.ArtikelID, a.LeverancierID, a.Naam, a.Omschrijving, a.BestelCode,
                           (SELECT COUNT(*) FROM dbo.Foto f WHERE f.ArtikelID = a.ArtikelID) AS FotoCount
                    FROM dbo.Artikel a
                    " + where + @"
                    ORDER BY CAST(a.Naam AS NVARCHAR(MAX)), a.ArtikelID
                    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY", conn))
                {
                    cmd.Parameters.AddWithValue("@LeverancierId", leverancierId);
                    cmd.Parameters.AddWithValue("@Search", search);
                    cmd.Parameters.AddWithValue("@SearchLike", "%" + search + "%");
                    cmd.Parameters.AddWithValue("@Skip", skip);
                    cmd.Parameters.AddWithValue("@Take", pageSize);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.artikelen.Add(new ArtikelRow
                            {
                                ArtikelID = reader.GetInt32(0),
                                LeverancierID = reader.GetInt32(1),
                                Naam = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Omschrijving = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                BestelCode = reader.IsDBNull(4) ? "" : reader.GetString(4).Trim(),
                                FotoCount = reader.GetInt32(5)
                            });
                        }
                    }
                }
            }

            return data;
        }

        [WebMethod]
        public static SaveResult SaveChanges(string data)
        {
            try
            {
                var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                var payload = serializer.Deserialize<SavePayload>(data ?? "{}");
                if (payload == null) payload = new SavePayload();

                using (var conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    if (payload.deletedIds != null)
                    {
                        foreach (var id in payload.deletedIds)
                        {
                            if (id <= 0) continue;
                            using (var cmdDelFoto = new SqlCommand("DELETE FROM dbo.Foto WHERE ArtikelID = @Id", conn))
                            {
                                cmdDelFoto.Parameters.AddWithValue("@Id", id);
                                cmdDelFoto.ExecuteNonQuery();
                            }
                            using (var cmd = new SqlCommand("DELETE FROM dbo.Artikel WHERE ArtikelID = @Id", conn))
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
                            if (string.IsNullOrEmpty(naam) && row.ArtikelID <= 0) continue;
                            if (row.LeverancierID <= 0)
                                return new SaveResult { success = false, message = "Elk artikel moet een leverancier hebben." };

                            var omschrijving = row.Omschrijving ?? "";
                            var bestelCode = (row.BestelCode ?? "").Trim();

                            if (row.ArtikelID <= 0)
                            {
                                using (var cmd = new SqlCommand(@"
                                    INSERT INTO dbo.Artikel (LeverancierID, Naam, Omschrijving, BestelCode)
                                    VALUES (@LeverancierID, @Naam, @Omschrijving, @BestelCode)", conn))
                                {
                                    cmd.Parameters.AddWithValue("@LeverancierID", row.LeverancierID);
                                    cmd.Parameters.AddWithValue("@Naam", naam);
                                    cmd.Parameters.AddWithValue("@Omschrijving", omschrijving);
                                    cmd.Parameters.AddWithValue("@BestelCode", string.IsNullOrEmpty(bestelCode) ? (object)DBNull.Value : bestelCode);
                                    cmd.ExecuteNonQuery();
                                }
                                inserted++;
                            }
                            else
                            {
                                using (var cmd = new SqlCommand(@"
                                    UPDATE dbo.Artikel
                                    SET LeverancierID = @LeverancierID, Naam = @Naam,
                                        Omschrijving = @Omschrijving, BestelCode = @BestelCode
                                    WHERE ArtikelID = @Id", conn))
                                {
                                    cmd.Parameters.AddWithValue("@LeverancierID", row.LeverancierID);
                                    cmd.Parameters.AddWithValue("@Naam", naam);
                                    cmd.Parameters.AddWithValue("@Omschrijving", omschrijving);
                                    cmd.Parameters.AddWithValue("@BestelCode", string.IsNullOrEmpty(bestelCode) ? (object)DBNull.Value : bestelCode);
                                    cmd.Parameters.AddWithValue("@Id", row.ArtikelID);
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
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                    return new SaveResult { success = false, message = "Kan niet verwijderen: artikel is nog gekoppeld aan voorraad, foto's of log." };
                return new SaveResult { success = false, message = ex.Message };
            }
            catch (Exception ex)
            {
                return new SaveResult { success = false, message = ex.Message };
            }
        }

        [WebMethod]
        public static List<FotoInfo> GetFotoList(int artikelId)
        {
            var list = new List<FotoInfo>();
            if (artikelId <= 0) return list;

            using (var conn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(
                "SELECT FotoID, Tijd FROM dbo.Foto WHERE ArtikelID = @Id ORDER BY Tijd DESC, FotoID DESC", conn))
            {
                cmd.Parameters.AddWithValue("@Id", artikelId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new FotoInfo
                        {
                            FotoID = reader.GetInt32(0),
                            Tijd = reader.GetDateTime(1).ToString("yyyy-MM-dd HH:mm")
                        });
                    }
                }
            }

            return list;
        }

        [WebMethod]
        public static SaveResult DeleteFoto(int fotoId)
        {
            try
            {
                if (fotoId <= 0)
                    return new SaveResult { success = false, message = "Ongeldige foto." };

                using (var conn = new SqlConnection(ConnString))
                using (var cmd = new SqlCommand("DELETE FROM dbo.Foto WHERE FotoID = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", fotoId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                return new SaveResult { success = true, message = "Foto verwijderd." };
            }
            catch (Exception ex)
            {
                return new SaveResult { success = false, message = ex.Message };
            }
        }

        public class PageData
        {
            public List<LeverancierLookup> leveranciers { get; set; }
            public List<ArtikelRow> artikelen { get; set; }
            public int totalCount { get; set; }
            public int page { get; set; }
            public int pageSize { get; set; }
        }

        public class LeverancierLookup
        {
            public int LeverancierID { get; set; }
            public string Naam { get; set; }
        }

        public class ArtikelRow
        {
            public int ArtikelID { get; set; }
            public int LeverancierID { get; set; }
            public string Naam { get; set; }
            public string Omschrijving { get; set; }
            public string BestelCode { get; set; }
            public int FotoCount { get; set; }
        }

        public class FotoInfo
        {
            public int FotoID { get; set; }
            public string Tijd { get; set; }
        }

        public class SavePayload
        {
            public List<ArtikelRow> rows { get; set; }
            public List<int> deletedIds { get; set; }
        }

        public class SaveResult
        {
            public bool success { get; set; }
            public string message { get; set; }
        }
    }
}
