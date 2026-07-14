<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Beheer.aspx.cs" Inherits="YourProject.Beheer" MasterPageFile="~/Site.Master" %>

<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">
    Beheer
</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" />

    <div class="sheet-page beheer-page">
        <h2>Beheer</h2>
        <p class="sheet-hint">Database en testdata voor ontwikkeling en demo.</p>

        <div class="beheer-panel">
            <h3>Testdata</h3>
            <p>Vult de database met 30 leveranciers, 1000 artikelen (met fotos), 5 personen en 150 voorraadbakken.</p>
            <label class="beheer-check">
                <input type="checkbox" id="chkClearFirst" />
                Eerst alle bestaande data wissen
            </label>
            <div class="sheet-toolbar">
                <button type="button" id="btnSeedTestData" class="primary">Vul database met testdata</button>
                <span id="seedStatus" class="sheet-status"></span>
            </div>
            <p class="beheer-note">Duurt ongeveer 30-60 seconden. Laat dit venster open tot het klaar is.</p>
        </div>
    </div>

    <link href="Content/sheet-grid.css" rel="stylesheet" />
    <style>
        .beheer-panel {
            background: #fff;
            border: 1px solid #ddd;
            border-radius: 8px;
            padding: 1.25rem 1.5rem;
            max-width: 640px;
            box-shadow: 0 1px 3px rgba(0,0,0,0.08);
        }
        .beheer-panel h3 { margin: 0 0 0.5rem 0; }
        .beheer-panel p { color: #555; margin: 0 0 0.75rem 0; font-size: 0.95rem; }
        .beheer-check { display: block; margin: 0.75rem 0 1rem 0; font-size: 0.9rem; }
        .beheer-note { font-size: 0.8rem; color: #888; margin-top: 0.75rem; }
        .sheet-toolbar button.primary {
            padding: 0.55rem 1rem;
            background: #3498db;
            color: #fff;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 0.95rem;
        }
        .sheet-toolbar button.primary:disabled { opacity: 0.6; cursor: wait; }
        .sheet-status.ok { color: #1e7e46; }
        .sheet-status.err { color: #c0392b; }
    </style>
    <script src="Scripts/beheer-page.js"></script>
</asp:Content>
