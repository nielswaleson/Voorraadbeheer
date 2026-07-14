using System.Web.Services;

namespace YourProject
{
    public partial class Beheer : System.Web.UI.Page
    {
        protected void Page_Load(object sender, System.EventArgs e) { }

        [WebMethod]
        public static TestDataSeeder.SeedResult SeedTestData(bool clearFirst)
        {
            return TestDataSeeder.Seed(clearFirst);
        }
    }
}
