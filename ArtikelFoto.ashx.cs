using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;

namespace YourProject
{
    public class ArtikelFoto : IHttpHandler
    {
        private static string ConnString =>
            ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public void ProcessRequest(HttpContext context)
        {
            if (context.Request.HttpMethod == "GET")
            {
                ServeImage(context);
                return;
            }

            if (context.Request.HttpMethod == "POST")
            {
                if (context.Request.ContentType != null &&
                    context.Request.ContentType.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    UploadJson(context);
                }
                else
                {
                    UploadMultipart(context);
                }
                return;
            }

            context.Response.StatusCode = 405;
        }

        private static void ServeImage(HttpContext context)
        {
            int fotoId;
            if (!int.TryParse(context.Request["id"], out fotoId) || fotoId <= 0)
            {
                context.Response.StatusCode = 400;
                return;
            }

            byte[] bytes;
            using (var conn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand("SELECT Foto FROM dbo.Foto WHERE FotoID = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", fotoId);
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    context.Response.StatusCode = 404;
                    return;
                }
                bytes = (byte[])result;
            }

            context.Response.ContentType = DetectMimeType(bytes);
            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetMaxAge(TimeSpan.FromHours(1));
            context.Response.BinaryWrite(bytes);
        }

        private static void UploadMultipart(HttpContext context)
        {
            int artikelId;
            if (!int.TryParse(context.Request["artikelId"], out artikelId) || artikelId <= 0)
            {
                WriteJson(context, false, "Ongeldig artikel.");
                return;
            }

            if (!ArtikelExists(artikelId))
            {
                WriteJson(context, false, "Artikel niet gevonden. Sla het artikel eerst op.");
                return;
            }

            int count = 0;
            var files = context.Request.Files;
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (file == null || file.ContentLength == 0) continue;

                byte[] bytes = new byte[file.ContentLength];
                file.InputStream.Read(bytes, 0, file.ContentLength);
                if (!IsImage(bytes)) continue;

                InsertFoto(artikelId, bytes);
                count++;
            }

            if (count == 0)
            {
                WriteJson(context, false, "Geen geldige afbeeldingen ontvangen.");
                return;
            }

            WriteJson(context, true, count + " foto('s) toegevoegd.");
        }

        private static void UploadJson(HttpContext context)
        {
            string body;
            using (var reader = new StreamReader(context.Request.InputStream))
                body = reader.ReadToEnd();

            var payload = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }
                .Deserialize<JsonUploadPayload>(body ?? "{}");

            if (payload == null || payload.artikelId <= 0)
            {
                WriteJson(context, false, "Ongeldig artikel.");
                return;
            }

            if (!ArtikelExists(payload.artikelId))
            {
                WriteJson(context, false, "Artikel niet gevonden. Sla het artikel eerst op.");
                return;
            }

            int count = 0;
            if (payload.images != null)
            {
                foreach (var img in payload.images)
                {
                    if (img == null || string.IsNullOrWhiteSpace(img.data)) continue;
                    var base64 = img.data;
                    var comma = base64.IndexOf(',');
                    if (comma >= 0) base64 = base64.Substring(comma + 1);

                    byte[] bytes;
                    try
                    {
                        bytes = Convert.FromBase64String(base64);
                    }
                    catch
                    {
                        continue;
                    }

                    if (!IsImage(bytes)) continue;
                    InsertFoto(payload.artikelId, bytes);
                    count++;
                }
            }

            if (count == 0)
            {
                WriteJson(context, false, "Geen geldige afbeeldingen ontvangen.");
                return;
            }

            WriteJson(context, true, count + " foto('s) toegevoegd.");
        }

        private static bool ArtikelExists(int artikelId)
        {
            using (var conn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand("SELECT 1 FROM dbo.Artikel WHERE ArtikelID = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", artikelId);
                conn.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        private static void InsertFoto(int artikelId, byte[] bytes)
        {
            using (var conn = new SqlConnection(ConnString))
            using (var cmd = new SqlCommand(
                "INSERT INTO dbo.Foto (Foto, ArtikelID, Tijd) VALUES (@Foto, @ArtikelID, SYSDATETIME())", conn))
            {
                cmd.Parameters.Add("@Foto", System.Data.SqlDbType.Image).Value = bytes;
                cmd.Parameters.AddWithValue("@ArtikelID", artikelId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static bool IsImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4) return false;
            if (bytes[0] == 0xFF && bytes[1] == 0xD8) return true;
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return true;
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) return true;
            if (bytes.Length >= 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46) return true;
            return false;
        }

        private static string DetectMimeType(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4) return "application/octet-stream";
            if (bytes[0] == 0xFF && bytes[1] == 0xD8) return "image/jpeg";
            if (bytes[0] == 0x89 && bytes[1] == 0x50) return "image/png";
            if (bytes[0] == 0x47 && bytes[1] == 0x49) return "image/gif";
            if (bytes.Length >= 12 && bytes[8] == 0x57 && bytes[9] == 0x45) return "image/webp";
            return "image/jpeg";
        }

        private static void WriteJson(HttpContext context, bool success, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.Write(new JavaScriptSerializer().Serialize(new { success, message }));
        }

        public bool IsReusable => false;

        private class JsonUploadPayload
        {
            public int artikelId { get; set; }
            public JsonImage[] images { get; set; }
        }

        private class JsonImage
        {
            public string name { get; set; }
            public string data { get; set; }
        }
    }
}
