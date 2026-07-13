<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Voorraad.aspx.cs" Inherits="YourProject.Voorraad" MasterPageFile="~/Site.Master" %>

<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">
    Voorraad locaties
</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" />

    <link href="Content/sheet-grid.css" rel="stylesheet" />

    <div class="sheet-page">
        <h2>Voorraadbakken</h2>
        <p class="sheet-hint">Elke bak heeft een barcode (L...) en een alarmniveau. Een bak bevat 1 of meer vervangbare artikelen van dezelfde soort.</p>

        <div class="sheet-toolbar">
            <button type="button" id="btnAddRow">+ Bak</button>
            <button type="button" id="btnSave" class="primary">Bakken opslaan</button>
            <button type="button" id="btnReload">Herladen</button>
            <span id="sheetStatus" class="sheet-status"></span>
        </div>

        <div class="sheet-wrap" id="sheetWrap">
            <table class="sheet-grid" id="locatieGrid">
                <thead><tr id="sheetHead"></tr></thead>
                <tbody id="sheetBody"></tbody>
            </table>
        </div>

        <div id="inhoudSection" class="inhoud-section" hidden>
            <h3 id="inhoudTitle">Inhoud</h3>
            <div class="sheet-toolbar">
                <button type="button" id="btnAddInhoud">+ Artikelregel</button>
                <button type="button" id="btnSaveInhoud" class="primary">Inhoud opslaan</button>
                <span id="inhoudStatus" class="sheet-status"></span>
            </div>
            <div class="sheet-wrap">
                <table class="sheet-grid" id="inhoudGrid">
                    <thead><tr id="inhoudHead"></tr></thead>
                    <tbody id="inhoudBody"></tbody>
                </table>
            </div>
        </div>
    </div>

    <script src="Scripts/sheet-grid.js"></script>
    <script src="Scripts/voorraad-grid.js"></script>
</asp:Content>
