<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Calendar.aspx.cs" Inherits="MyCalendar" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">

  <title></title>

  <script src="js/jquery-1.4.4.min.js" type="text/javascript"></script>
  <script src="js/jquery-ui-1.8.7.custom.min.js" type="text/javascript"></script>
  <link href="css/trontastic/jquery-ui-1.8.7.custom.css" rel="stylesheet" type="text/css" />

  <script type="text/javascript">

    $(document).ready(function () {
      $(".dailyeditor").draggable();
    });

    function afficherPayeOpt() {
      var optz = {};
      var pos = $("#btnPayeOpt").position();
      $("#divPayeOptions").css({ "top": pos.top + 15, "left": pos.left });
      $("#divPayeOptions").show('slide', optz, 200);
    }

    function hidePayeOpt() {
      $("#divPayeOptions").slideUp('fast');
    }

  </script>

  <style type="text/css">

    body {
      margin: 0px 0px 0px 2px;
      font-family: Consolas;
      font-size: 10pt;
    }

    a {
      text-decoration: none;
    }
      
    #topbar {
      background-color: Maroon;
      border-bottom: solid 1px black;
      color: White;
      padding-left: 7px;
    }

    .notepad {
      float: left;
      width: 275px;
      padding: 0px 5px 0px 5px;
      border: solid 3px gray;
      height: 1074px;
    }

    .calspot {
      float: left;
      padding: 0px 5px 10px 5px;
      height: 100%;
    }
      
    .caltitle { 
      text-align: center;
      font-size: 12pt;
      font-weight: bold;
      width: 840px;
      border: 0px none transparent;
    }

    .dailyeditor {
      position: absolute;
      top: 150px;
      left: 300px;
      width: 535px;
      height: 240px;
      background-color: White;
      border-style: solid;
      border-width: 3px;
      border-color: Gray;
      z-index: 1000;
      cursor: move;
    }
      
    .textbox1 {
      font-size: 8pt;
      background-color: #008080 ;
      border: none 0px black;
      color: #1AFFFF;
    }
      
    .textbox2 {
      font-size: 8pt;
      background-color: #c0c0c0 ;
      border: solid 1px Gray;
      color: blue;
    }

    .button1 {
      font-size: 7pt;
      cursor: pointer;        
      border: 0;
    }

    .button2 {
      font-size: 8pt;
      cursor: pointer;        
    }

    .calmois {
      text-transform: capitalize;
    }

    .selday {
      font-size: 11pt;
      font-weight: bold;
      color: Black;
    }

    .invis {
      display: none;
    }

    #divPayeOptions {
      border: 3px solid #c0c0c0;
      background-color: #f0f0f0;
      position: absolute;
      top: 20px;
      left: 20px;
      width: 160px;
      height: 170px;
      display: none;
      padding: 10px;
    }

    @media print {
      .noprint {
        display: none;
      }
    }

  </style>

</head>

<body>

  <form id="form1" runat="server">

    <asp:HiddenField ID="hid_cal" runat="server" Value="0" />
    
    <div id="topbar" class="noprint">
      jCal v4.0 &copy; 2005-2014 JFG &nbsp;
      Année: <span style="font-size:6pt;"><asp:LinkButton ID="lbLeft" runat="server" CommandArgument="left" OnCommand="lbRightLeft_Command" ForeColor="White" Text="&#9668;" ToolTip="Année précédente" /></span><asp:TextBox ID="txtCalYear" runat="server" Columns="4" MaxLength="4" CssClass="textbox1" /><span style="font-size:6pt;"><asp:LinkButton ID="lbRight" runat="server" CommandArgument="right" OnCommand="lbRightLeft_Command" ForeColor="White" Text="&#9658;" ToolTip="Année suivante" /></span>
      <asp:Button ID="btnCalYear" runat="server" Text="Go!" CssClass="button1" OnClick="btnCalYear_Click" />
      &nbsp;
      <asp:RadioButton ID="radFR" runat="server" Text="Fr" GroupName="g1" AutoPostBack="true" OnCheckedChanged="radg1_CheckedChanged" ToolTip="Calendrier en français" />
      <asp:RadioButton ID="radEN" runat="server" Text="En" GroupName="g1" AutoPostBack="true" OnCheckedChanged="radg1_CheckedChanged" ToolTip="Calendrier en anglais" />
      &nbsp;
      <asp:Button ID="btnPrint" runat="server" Text="Imprimer" CssClass="button1" OnClientClick="window.print(); return false;" />
      &nbsp;
      Couleur:
      <asp:CheckBox ID="chkInvertColor" runat="server" Text="Inverser" AutoPostBack="true" />
      <asp:CheckBox ID="chkNoColor" runat="server" Text="Aucune" AutoPostBack="true" />
      <asp:CheckBox ID="chkDecalage" runat="server" Text="Décaler" AutoPostBack="true" ToolTip="Décaler d'une semaine" OnCheckedChanged="chkDecalage_CheckedChanged" />
      &nbsp;
      <input id="btnPayeOpt" type="button" value="Paye" title="Ajustements pour afficher les jours de paye" onclick="afficherPayeOpt();" class="button1" />
      &nbsp;
      <asp:RadioButton ID="rad1sem" runat="server" Text="1 sem." GroupName="nbsem" AutoPostBack="true" OnCheckedChanged="radnbsem_CheckedChanged" ToolTip="1 semaine de partage" Checked="true" />
      <asp:RadioButton ID="rad2sem" runat="server" Text="2 sem." GroupName="nbsem" AutoPostBack="true" OnCheckedChanged="radnbsem_CheckedChanged" ToolTip="2 semaines de partage" />

    </div>
    
    <div id="caltitle">
      <asp:TextBox ID="txtCalTitle" runat="server" CssClass="caltitle" />
    </div>

    <div style="position: relative; height: 1075px;">
    
      <asp:Panel ID="pnlNotepad"  CssClass="notepad" runat="server">
        <asp:Table ID="tblNotepad" runat="server" Height="100%"  style="margin-bottom: 8px;"/>
      </asp:Panel>
      
      <asp:Panel  ID="pnlCalspot" CssClass="calspot" runat="server">     
        <asp:Table ID="tblCalspot" GridLines="Both" Height="1080" CellPadding="2" runat="server" BorderWidth="3" BorderColor="Gray" BorderStyle="Solid" />
        <br />
      </asp:Panel>

    </div>
        
    <asp:Panel ID="pnlDailyEditor" runat="server" CssClass="dailyeditor" Visible="false">
      
      <div style="padding-top: 5px; padding-left: 15px">
        Date sélectionnée: <asp:Label ID="lblDateSel" runat="server" /> 
      </div>
      
      <div style="float: left; padding: 15px;">
        <asp:PlaceHolder ID="plhDailyEditorCal" runat="server" />
      </div>
      
      <div style="float: left; padding: 15px; padding-top: 10px;">
        <div style="font-size: 8pt;">Entrez vos notes pour cette date</div>
        <asp:TextBox ID="txtDailyNotes" runat="server" TextMode="MultiLine" Rows="8" Columns="37" CssClass="textbox2" /><br />
        <asp:Button ID="btnSaveDailyNote" runat="server" Text="Ok" CssClass="button2" OnClick="btnSaveDailyNote_Click" />
        <asp:Button ID="btnFermerDailyEditor" runat="server" Text="Fermer" CssClass="button2" OnClick="btnFermerDailyEditor_Click" />
        <asp:Button ID="btnCopyDailyNoteToNotepad" runat="server" Text=" « " CssClass="button2" OnClick="btnCopyDailyNoteToNotepad_Click" ToolTip="Copier l'information dans la colonne de notes" />
      </div>
      
    </asp:Panel>
    
    <div id="divPayeOptions">
      <asp:CheckBox ID="chkHilitePayDay" runat="server" Text="Afficher paye" ToolTip="Affiche les jours de paye dans une couleur différente" /><br />
      <br />
      Jour de la paye:<br />
      <asp:DropDownList ID="ddoJourDePaye" runat="server" CssClass="textbox2" /><br />
      <br />
      <asp:CheckBox ID="chkOtherWeek" runat="server" Text="Autre semaine" ToolTip="Changer la semaine de la paye" /><br />
      <br />
      <asp:Button ID="btnSetPayeOpt" runat="server" Text="Appliquer" />
      <input type="button" value="Annuler" onclick="hidePayeOpt();" />
    </div>

  </form>
</body>
</html>
