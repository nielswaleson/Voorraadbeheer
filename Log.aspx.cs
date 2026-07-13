using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Services;

namespace YourProject
{
    public partial class Log : System.Web.UI.Page
    {
        private const int DefaultPageSize = 50;

        private static string ConnString =>
            ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e) { }

        [WebMethod]
        public static LogPageData GetLog(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = DefaultPageSize;
            if (pageSize > 200) pageSize = 200;

            int skip = (page - 1) * pageSize;
            var data = new LogPageData { page = page, pageSize = pageSize, entries = new List<LogEntry>() };

            using (var conn = new SqlConnection(ConnString))
            {
                conn.Open();

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Log", conn))
                    data.totalCount = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new SqlCommand(@"
                    SELECT l.LogID, l.Tijd, CAST(l.Actie AS NVARCHAR(MAX)),
                           p.Naam, v.Locatie, CAST(v.Naam AS NVARCHAR(MAX)),
                           RTRIM(a.BestelCode), CAST(a.Naam AS NVARCHAR(MAX))
                    FROM dbo.Log l
                    LEFT JOIN dbo.Persoon p ON p.PersoonID = l.PersoonID
                    LEFT JOIN dbo.Voorraad v ON v.VoorraadID = l.VoorraadID
                    LEFT JOIN dbo.Artikel a ON a.ArtikelID = l.ArtikelID
                    ORDER BY l.Tijd DESC, l.LogID DESC
                    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY", conn))
                {
                    cmd.Parameters.AddWithValue("@Skip", skip);
                    cmd.Parameters.AddWithValue("@Take", pageSize);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var bestel = r.IsDBNull(6) ? "" : r.GetString(6);
                            var artNaam = r.IsDBNull(7) ? "" : r.GetString(7);
                            data.entries.Add(new LogEntry
                            {
                                LogID = r.GetInt32(0),
                                Tijd = r.GetDateTime(1).ToString("yyyy-MM-dd HH:mm:ss"),
                                Actie = r.IsDBNull(2) ? "" : r.GetString(2),
                                Persoon = r.IsDBNull(3) ? "-" : r.GetString(3),
                                Locatie = r.IsDBNull(4) ? "-" : r.GetString(4),
                                LocatieNaam = r.IsDBNull(5) ? "" : r.GetString(5),
                                Artikel = string.IsNullOrEmpty(bestel) ? artNaam : bestel + " - " + artNaam
                            });
                        }
                    }
                }
            }

            return data;
        }

        public class LogPageData
        {
            public List<LogEntry> entries { get; set; }
            public int page { get; set; }
            public int pageSize { get; set; }
            public int totalCount { get; set; }
        }

        public class LogEntry
        {
            public int LogID { get; set; }
            public string Tijd { get; set; }
            public string Actie { get; set; }
            public string Persoon { get; set; }
            public string Locatie { get; set; }
            public string LocatieNaam { get; set; }
            public string Artikel { get; set; }
        }
    }
}
