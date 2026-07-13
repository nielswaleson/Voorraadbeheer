<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Artikelen.aspx.cs" Inherits="YourProject.Artikelen" MasterPageFile="~/Site.Master" %>

<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">
    Artikelen beheren
</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" />

    <link href="Content/sheet-grid.css" rel="stylesheet" />

    <div class="sheet-page sheet-page-wide">
        <h2>Artikelen</h2>
        <p class="sheet-hint">
            Spreadsheet voor artikelen met leverancierskoppeling.
            <a href="Leveranciers.aspx">Leveranciers beheren</a> |
            Filter op leverancier via <code>?leverancier=ID</code> in de URL.
            Klik op <strong>📷</strong> in de tabel om foto's te beheren.
        </p>

        <div id="leverancierWarn" class="sheet-warn" style="display:none">
            Geen leveranciers gevonden. <a href="Leveranciers.aspx">Voeg eerst een leverancier toe</a> voordat je artikelen aanmaakt.
        </div>

        <div class="sheet-filters">
            <label>
                Leverancier
                <select id="filterLeverancier" class="filter-select">
                    <option value="">Alle leveranciers</option>
                </select>
            </label>
            <label class="filter-search-wrap">
                Zoeken
                <input type="search" id="filterSearch" class="filter-search" placeholder="Naam, omschrijving of bestelcode..." />
            </label>
            <span id="filterCount" class="filter-count"></span>
        </div>

        <div class="sheet-toolbar">
            <button type="button" id="btnAddRow">+ Rij</button>
            <button type="button" id="btnSave" class="primary">Opslaan</button>
            <button type="button" id="btnReload">Herladen</button>
            <div class="sheet-pager">
                <button type="button" id="btnPrevPage">&lsaquo; Vorige</button>
                <span id="pageInfo">Pagina 1</span>
                <button type="button" id="btnNextPage">Volgende &rsaquo;</button>
            </div>
            <span id="sheetStatus" class="sheet-status"></span>
        </div>

        <div class="sheet-wrap sheet-wrap-tall" id="sheetWrap">
            <table class="sheet-grid sheet-grid-artikelen" id="artikelenGrid">
                <thead>
                    <tr id="sheetHead"></tr>
                </thead>
                <tbody id="sheetBody"></tbody>
            </table>
        </div>

        <div id="fotoModal" class="foto-modal" hidden>
            <div class="foto-modal-dialog" id="fotoPanel" role="dialog" aria-modal="true">
                <div class="foto-panel-header">
                    <h3 id="fotoPanelTitle">Foto's</h3>
                    <button type="button" id="fotoPanelClose" class="foto-panel-close" title="Sluiten">&times;</button>
                </div>
                <div id="fotoDropZone" class="foto-dropzone">
                    <p class="foto-dropzone-text">Sleep foto's hierheen, plak met <strong>Ctrl+V</strong>, of</p>
                    <label class="foto-file-label">
                        kies bestanden
                        <input type="file" id="fotoFileInput" accept="image/*" multiple hidden />
                    </label>
                </div>
                <div id="fotoGallery" class="foto-gallery"></div>
                <p id="fotoPanelStatus" class="foto-panel-status"></p>
            </div>
        </div>
    </div>

    <script src="Scripts/sheet-grid.js"></script>
    <script src="Scripts/artikelen-fotos.js"></script>
    <script src="Scripts/artikelen-grid.js"></script>
</asp:Content>
