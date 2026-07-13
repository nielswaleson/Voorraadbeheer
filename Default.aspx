<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="YourProject.Default" MasterPageFile="~/Site.Master" %>

<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">
    Voorraad scan
</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" />

    <link href="Content/scan-workstation.css" rel="stylesheet" />

    <div class="scan-page">
        <div class="scan-header">
            <h2>Voorraad scan</h2>
            <p class="scan-hint">
                Scan volgorde: <strong>persoon</strong> (<code>P1</code>) -&gt;
                <strong>bak</strong> (<code>L00001</code>) -&gt;
                <strong>kies artikel</strong> (klik op foto).
                Actie 1 afnemen: <code>A-AF</code>
            </p>
        </div>

        <div class="scan-input-wrap">
            <label for="scanInput">Barcode</label>
            <input type="text" id="scanInput" class="scan-input" autocomplete="off" autofocus placeholder="Scan barcode..." />
        </div>

        <div id="scanStatus" class="scan-status">Scan persoon (P...) -&gt; bak (L...) -&gt; kies artikel</div>

        <div class="session-bars">
            <div id="persoonBar" class="session-bar" hidden>
                <span class="session-label">Persoon:</span>
                <strong id="persoonNaam"></strong>
            </div>
            <div id="locatieBar" class="session-bar" hidden>
                <span class="session-label">Bak:</span>
                <strong id="locatieLabel"></strong>
            </div>
            <button type="button" id="btnClearSession" class="btn-link">Sessie wissen</button>
        </div>

        <div id="artikelKeuze" class="artikel-keuze" hidden>
            <h3 class="artikel-keuze-title">Kies artikel</h3>
            <p id="artikelKeuzeSoort" class="artikel-keuze-soort"></p>
            <div id="artikelKaarten" class="artikel-kaarten"></div>
        </div>

        <div id="artikelPanel" class="artikel-panel" hidden>
            <div id="artikelPhotoWrap" class="artikel-photo-wrap" hidden>
                <img id="artikelFoto" class="artikel-foto" alt="" />
            </div>
            <div class="artikel-info">
                <h3 id="artikelNaam"></h3>
                <p id="artikelMeta" class="artikel-meta"></p>
                <p id="artikelOmschrijving" class="artikel-omschrijving"></p>
                <div class="voorraad-display">
                    <span class="voorraad-label">Dit artikel in bak</span>
                    <span id="voorraadAantal" class="voorraad-aantal">0</span>
                </div>
                <div class="voorraad-display bak-totaal-row">
                    <span class="voorraad-label">Totaal in bak</span>
                    <span id="bakTotaal" class="voorraad-aantal bak-totaal">0</span>
                </div>
                <p id="alarmInfo" class="alarm-info"></p>

                <div class="voorraad-actions">
                    <div class="action-row">
                        <button type="button" id="btnRemove1" class="btn-action btn-remove primary">-1 (standaard)</button>
                        <label class="qty-inline">
                            -<input type="number" id="qtyRemove" min="1" value="1" class="qty-input" />
                            <button type="button" id="btnRemoveN" class="btn-action btn-remove">Afnemen</button>
                        </label>
                    </div>
                    <div class="action-row">
                        <label class="qty-inline">
                            +<input type="number" id="qtyAdd" min="1" value="1" class="qty-input" />
                            <button type="button" id="btnAddN" class="btn-action btn-add">Toevoegen</button>
                        </label>
                    </div>
                    <div class="action-row">
                        <label class="qty-inline">
                            Corrigeer naar
                            <input type="number" id="qtySet" min="0" value="0" class="qty-input qty-input-wide" />
                            <button type="button" id="btnSetQty" class="btn-action">Instellen</button>
                        </label>
                    </div>
                </div>
                <p class="actie-hint">Of scan <code>A-AF</code> om direct 1 af te nemen.</p>
            </div>
        </div>

        <div id="scanLog" class="scan-log"></div>
    </div>

    <script src="Scripts/scan-workstation.js"></script>
</asp:Content>
