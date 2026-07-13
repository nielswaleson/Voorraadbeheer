<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Alarm.aspx.cs" Inherits="YourProject.Alarm" MasterPageFile="~/Site.Master" %>

<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">
    Alarm lage voorraad
</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" />

    <div class="sheet-page alarm-page">
        <h2>Alarm - lage voorraad</h2>
        <p class="sheet-hint">
            Bakken waar het <strong>totaal aantal</strong> in de bak op of onder het alarmniveau van de bak staat.
            Per bak ziet de inkoper welke <strong>vervangbare artikelen</strong> (zelfde soort) besteld kunnen worden,
            of welk artikel inactief gemaakt kan worden.
        </p>

        <div class="sheet-toolbar">
            <button type="button" id="btnReloadAlarm">Herladen</button>
            <span id="alarmCount" class="sheet-status"></span>
        </div>

        <div id="alarmList" class="alarm-bak-list"></div>
    </div>

    <link href="Content/sheet-grid.css" rel="stylesheet" />
    <style>
        .alarm-bak { background: #fff; border: 1px solid #e0b4b4; border-radius: 8px; margin-bottom: 1rem; overflow: hidden; }
        .alarm-bak-header { background: #fdecea; padding: 0.75rem 1rem; display: flex; flex-wrap: wrap; gap: 0.5rem 1.5rem; align-items: baseline; }
        .alarm-bak-header strong { font-size: 1.05rem; }
        .alarm-bak-soort { color: #666; font-size: 0.9rem; }
        .alarm-bak-totaal { color: #c0392b; font-size: 0.9rem; margin-left: auto; }
        .alarm-artikel-table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
        .alarm-artikel-table th, .alarm-artikel-table td { padding: 0.5rem 0.75rem; border-top: 1px solid #eee; text-align: left; }
        .alarm-artikel-table th { background: #fafafa; font-weight: 600; }
        .alarm-artikel-table tr.laag td { background: #fff8f8; }
        .alarm-artikel-table tr.inactief td { color: #999; }
        .alarm-artikel-table .col-aantal { text-align: right; font-weight: 600; }
        .alarm-artikel-table tr.laag .col-aantal { color: #c0392b; }
        .badge-laag { background: #e74c3c; color: #fff; font-size: 0.75rem; padding: 0.15rem 0.45rem; border-radius: 4px; }
        .badge-ok { background: #ecf0f1; color: #666; font-size: 0.75rem; padding: 0.15rem 0.45rem; border-radius: 4px; }
        .btn-inactief { font-size: 0.8rem; padding: 0.25rem 0.5rem; cursor: pointer; border: 1px solid #ccc; border-radius: 4px; background: #fff; }
        .btn-inactief:hover { background: #f5f5f5; }
        .alarm-empty { color: #666; padding: 1rem; background: #fff; border-radius: 8px; }
    </style>
    <script src="Scripts/alarm-page.js"></script>
</asp:Content>
