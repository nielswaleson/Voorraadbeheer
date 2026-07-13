using System;
using System.Web;

namespace YourProject
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            DatabaseBootstrap.EnsureDatabase();
        }
    }
}
