using System;

using System.Collections.Generic;

using System.Configuration;

using System.Data.SqlClient;

using System.Web.Script.Serialization;

using System.Web.Services;



namespace YourProject

{

    public partial class Personen : System.Web.UI.Page

    {

        private static string ConnString =>

            ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;



        protected void Page_Load(object sender, EventArgs e)

        {

        }



        [WebMethod]

        public static List<PersoonRow> GetPersonen()

        {

            var list = new List<PersoonRow>();



            using (var conn = new SqlConnection(ConnString))

            using (var cmd = new SqlCommand("SELECT PersoonID, Naam, Actief, Barcode FROM dbo.Persoon ORDER BY PersoonID", conn))

            {

                conn.Open();

                using (var reader = cmd.ExecuteReader())

                {

                    while (reader.Read())

                    {

                        list.Add(new PersoonRow

                        {

                            PersoonID = reader.GetInt32(0),

                            Naam = reader.IsDBNull(1) ? "" : reader.GetString(1),

                            Actief = !reader.IsDBNull(2) && reader.GetBoolean(2),

                            Barcode = reader.IsDBNull(3) ? "P" + reader.GetInt32(0) : reader.GetString(3)

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

                            using (var cmd = new SqlCommand("DELETE FROM dbo.Persoon WHERE PersoonID = @Id", conn))

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

                            if (string.IsNullOrEmpty(naam) && row.PersoonID <= 0) continue;



                            if (row.PersoonID <= 0)

                            {

                                using (var cmd = new SqlCommand(

                                    "INSERT INTO dbo.Persoon (Naam, Actief) OUTPUT INSERTED.PersoonID VALUES (@Naam, @Actief)", conn))

                                {

                                    cmd.Parameters.AddWithValue("@Naam", naam);

                                    cmd.Parameters.AddWithValue("@Actief", row.Actief);

                                    int newId = Convert.ToInt32(cmd.ExecuteScalar());

                                    using (var cmdBc = new SqlCommand(

                                        "UPDATE dbo.Persoon SET Barcode = @Bc WHERE PersoonID = @Id", conn))

                                    {

                                        cmdBc.Parameters.AddWithValue("@Bc", "P" + newId);

                                        cmdBc.Parameters.AddWithValue("@Id", newId);

                                        cmdBc.ExecuteNonQuery();

                                    }

                                }

                                inserted++;

                            }

                            else

                            {

                                using (var cmd = new SqlCommand(

                                    "UPDATE dbo.Persoon SET Naam = @Naam, Actief = @Actief WHERE PersoonID = @Id", conn))

                                {

                                    cmd.Parameters.AddWithValue("@Naam", naam);

                                    cmd.Parameters.AddWithValue("@Actief", row.Actief);

                                    cmd.Parameters.AddWithValue("@Id", row.PersoonID);

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



        public class PersoonRow

        {

            public int PersoonID { get; set; }

            public string Naam { get; set; }

            public bool Actief { get; set; }

            public string Barcode { get; set; }

        }



        public class SavePayload

        {

            public List<PersoonRow> rows { get; set; }

            public List<int> deletedIds { get; set; }

        }



        public class SaveResult

        {

            public bool success { get; set; }

            public string message { get; set; }

        }

    }

}

