<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Log.aspx.cs" Inherits="YourProject.Log" MasterPageFile="~/Site.Master" %>

<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">
    Actielog
</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" />

    <div class="sheet-page">
        <h2>Actielog</h2>
        <p class="sheet-hint">Overzicht van uitgevoerde voorraadacties (scannen, afnemen, toevoegen, correcties).</p>

        <div class="sheet-toolbar">
            <button type="button" id="btnReloadLog">Herladen</button>
            <button type="button" id="btnPrevPage">Vorige</button>
            <span id="pageInfo">Pagina 1</span>
            <button type="button" id="btnNextPage">Volgende</button>
            <span id="logStatus" class="sheet-status"></span>
        </div>

        <div class="sheet-wrap">
            <table class="sheet-grid log-grid" id="logTable">
                <thead>
                    <tr>
                        <th>Tijd</th>
                        <th>Persoon</th>
                        <th>Locatie</th>
                        <th>Artikel</th>
                        <th>Actie</th>
                    </tr>
                </thead>
                <tbody id="logBody"></tbody>
            </table>
        </div>
    </div>

    <link href="Content/sheet-grid.css" rel="stylesheet" />
    <script src="Scripts/log-page.js"></script>
</asp:Content>
