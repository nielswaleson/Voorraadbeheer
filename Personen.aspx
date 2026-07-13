<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Personen.aspx.cs" Inherits="YourProject.Personen" MasterPageFile="~/Site.Master" %>

<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">
    Personen beheren
</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" />

    <link href="Content/sheet-grid.css" rel="stylesheet" />

    <div class="sheet-page">
        <h2>Personen</h2>
        <p class="sheet-hint">Bewerk cellen direct in de tabel. Sleep of Shift+klik om meerdere cellen te selecteren. Kopieer met Ctrl+C en plak met Ctrl+V (Excel-formaat).</p>

        <div class="sheet-toolbar">
            <button type="button" id="btnAddRow">+ Rij</button>
            <button type="button" id="btnSave" class="primary">Opslaan</button>
            <button type="button" id="btnReload">Herladen</button>
            <span id="sheetStatus" class="sheet-status"></span>
        </div>

        <div class="sheet-wrap" id="sheetWrap">
            <table class="sheet-grid" id="personenGrid">
                <thead>
                    <tr id="sheetHead"></tr>
                </thead>
                <tbody id="sheetBody"></tbody>
            </table>
        </div>
    </div>

    <script src="Scripts/sheet-grid.js"></script>
    <script src="Scripts/personen-grid.js"></script>
</asp:Content>
