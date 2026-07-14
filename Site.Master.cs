using System;
using System.IO;
using System.Web.UI;

namespace YourProject
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string page = Path.GetFileName(Request.Path).ToLowerInvariant();
            lnkHome.CssClass = page == "default.aspx" ? "active" : "";
            lnkPersonen.CssClass = page == "personen.aspx" ? "active" : "";
            lnkLeveranciers.CssClass = page == "leveranciers.aspx" ? "active" : "";
            lnkArtikelen.CssClass = page == "artikelen.aspx" ? "active" : "";
            lnkVoorraad.CssClass = page == "voorraad.aspx" ? "active" : "";
            lnkLog.CssClass = page == "log.aspx" ? "active" : "";
            lnkAlarm.CssClass = page == "alarm.aspx" ? "active" : "";
            lnkBeheer.CssClass = page == "beheer.aspx" ? "active" : "";
        }
    }
}
