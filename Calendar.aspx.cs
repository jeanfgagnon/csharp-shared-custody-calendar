// @(#) Calendar.aspx.cs
//      © JF Gagnon 2006-2014
//      Calendrier de garde partagée
//      S'imprime mieux avec IE en mode 7 + quirk (à corriger pour standardiser)
//

// Mai 2014, maintenant le calendrier peut fonctionner avec deux semaines d'échange.
// Très belle impression via PDF Creator + Chrome, le fichier est purrrrrrfect

using System;
using System.Data;
using System.Data.Sql;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Calendar = System.Web.UI.WebControls.Calendar;

public partial class MyCalendar : System.Web.UI.Page {

  private const String DATA_PATH = "~/App_Data/";

  private Int32 _nbLineNotepad = 38;
  private Int32 _cellHeight = 20;
  private Int32 _cellWidth = 19;
  private Int32 _cellHeightNoteMois = 16;
  private Int32 _nbLigneNoteMois = 3;
  private Int32 _startYear = DateTime.Now.Year;
  private int _weekBrakeValue = 1;
  private DataSet _dataset = null;
  private System.Drawing.Color _PapaColor = System.Drawing.Color.LightSkyBlue;
  private System.Drawing.Color _MamaColor = System.Drawing.Color.Pink;
  private DayOfWeek _firstDayOfSwitch = DayOfWeek.Monday;  // on change de couleur ce jour

  #region "OVERRIDEN STUFF"

  protected override void OnInit(EventArgs e) {
    loadNotepad();
    loadCalspot();
    base.OnInit(e);

    return;
  }

  protected override void InitializeCulture() {
    String lng = "en-CA";
    // on doit utiliser la "request" ici car les contrôles n'existent pas encore à ce stade
    if (Request.Form["g1"] != null && Request.Form["g1"].ToString() == "radEN")
      lng = "en-CA";
    else
      lng = "fr-CA";

    Thread.CurrentThread.CurrentCulture = new CultureInfo(lng);
    Thread.CurrentThread.CurrentUICulture = new CultureInfo(lng);

    base.InitializeCulture();

    return;
  }

  #endregion

  protected void Page_Load(Object sender, EventArgs e) {
    this.Title = "jCal v4.0";
    if (!Page.IsPostBack) {
      Cache.Remove("dataset");
      this.txtCalYear.Text = _startYear.ToString();
      reloadAll();
      bindDDL();
    }
    else
      _startYear = Convert.ToInt32(this.txtCalYear.Text);

    HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);

    return;
  }

  private void bindDDL() {
    this.ddoJourDePaye.Items.Add(new ListItem("Dimanche", "0"));
    this.ddoJourDePaye.Items.Add(new ListItem("Lundi", "1"));
    this.ddoJourDePaye.Items.Add(new ListItem("Mardi", "2"));
    this.ddoJourDePaye.Items.Add(new ListItem("Mercredi", "3"));
    this.ddoJourDePaye.Items.Add(new ListItem("Jeudi", "4"));
    this.ddoJourDePaye.Items.Add(new ListItem("Vendredi", "5"));
    this.ddoJourDePaye.Items.Add(new ListItem("Samedi", "6"));

    // jour de paye par défaut: Jeudi, assez universel
    this.ddoJourDePaye.SelectedIndex = 4;

    return;
  }

  private void initPage() {
    this.txtCalTitle.Text = "Calendrier de garde pargagée " + _startYear.ToString();
    this.txtCalYear.Text = _startYear.ToString();
    if (Thread.CurrentThread.CurrentCulture.Name.Substring(0, 2).ToUpper() == "EN")
      this.radEN.Checked = true;
    else
      this.radFR.Checked = true;

    return;
  }

  private void reloadAll() {
    if (this.txtCalYear.Text != String.Empty) {
      Int32 y = _startYear;
      if (Int32.TryParse(this.txtCalYear.Text, out y)) {
        _startYear = y;
        initPage();
        loadDataset(true);
        redrawCalendar();
        initNotepad();
        initMonthlyNotes();
      }
    }

    return;
  }

  private void initMonthlyNotes() {
    wipeMonthlyNotes();
    DataView v = new DataView(_dataset.Tables["monthlynotes"]);
    v.RowFilter = String.Format("id_cal={0}", CurrentCalendarID);
    v.Sort = "calmonth";
    if (v.Count > 0) {
      for (Int32 i = 0; i < 12; i++) {
        DataRow r = v[i].Row;
        for (Int32 k = 0; k < 3; k++) {
          String ctrlId = String.Format("month_note_{0}_{1}", r["calmonth"], k + 1);
          TextBox box = (TextBox)FindControl(ctrlId);
          if (box != null) {
            box.Text = r[String.Format("note_{0}", k + 1)].ToString();
          }
        }
      }
    }

    return;
  }

  private void wipeMonthlyNotes() {
    for (Int32 i = 0; i < 12; i++) {
      for (Int32 k = 0; k < 3; k++) {
        String ctrlId = String.Format("month_note_{0}_{1}", i + 1, k + 1);
        TextBox box = (TextBox)FindControl(ctrlId);
        if (box != null) {
          box.Text = String.Empty;
        }
      }
    }

    return;
  }

  private void loadDataset() {
    loadDataset(false);

    return;
  }

  private void loadDataset(Boolean force) {
    String xmlFileName = getXmlFileName();
    _dataset = new DataSet("GPCal");
    if (System.IO.File.Exists(xmlFileName)) {
      if (force || Cache["dataset"] == null) {
        _dataset.ReadXml(xmlFileName, XmlReadMode.Auto);
        Cache["dataset"] = _dataset;
        CurrentCalendarID = 0;
      }
      else
        _dataset = Cache["dataset"] as DataSet;

      if (force) {
        DataRow[] rowz = _dataset.Tables["cals"].Select("calyear=" + _startYear);
        if (rowz.Length > 0) {
          CurrentCalendarID = (Int32)rowz[0]["id_cal"];
        }
      }

    }
    else {
      createDatasetStructure();
      Cache["dataset"] = _dataset;
    }

    return;
  }

  private String getXmlFileName() {
    String rv = String.Empty;
    String osPath = Server.MapPath(DATA_PATH);
    if (createDir(osPath)) {
      rv = String.Format("{0}data.xml", osPath);
    }

    return rv;
  }

  private Boolean createDir(String dirPath) {
    Boolean rv = true;
    if (!Directory.Exists(dirPath)) {
      try {
        Directory.CreateDirectory(dirPath);
      }
      catch /* ALL */ {
        rv = false;
      }
    }

    return rv;
  }

  private void createBlankRec(String tableName, Int32 id_cal) {
    DataRow r = _dataset.Tables[tableName].NewRow();
    _dataset.Tables[tableName].Rows.Add(r);

    return;
  }

  private void saveCALS() {
    DataRow r = null;
    if (CurrentCalendarID == 0) {
      // un nouveau record.
      CurrentCalendarID = _dataset.Tables["cals"].Rows.Count + 1;
      r = _dataset.Tables["cals"].NewRow();
      r["id_cal"] = CurrentCalendarID;
      r["calyear"] = _startYear;
      _dataset.Tables["cals"].Rows.Add(r);
    }
    else {
      r = _dataset.Tables["cals"].Select("id_cal=" + CurrentCalendarID.ToString())[0];
    }
    r["date_mod"] = DateTime.Now;

    return;
  }

  private void saveNOTES() {
    if (CurrentCalendarID != 0) {
      for (Int32 i = 1; i < _nbLineNotepad; i++) {  // commence à un car la ligne 0 contient le titre du bloc
        Int32 noligne = i + 1;
        TextBox t = (TextBox)FindControl(String.Format("notepadTextBox_{0}", noligne));
        String contenu = t.Text.Trim();
        DataRow[] existingRow = _dataset.Tables["notes"].Select(String.Format("id_cal={0} AND noligne={1}", CurrentCalendarID, noligne));
        if (existingRow.Length == 1) {
          // cette note existe déjà
          existingRow[0]["note"] = contenu;
        }
        else if (!String.IsNullOrEmpty(contenu)) {
          DataRow newRow = _dataset.Tables["notes"].NewRow();
          Int32 id_note = getNextId("notes", "id_note");
          newRow["id_note"] = id_note;
          newRow["id_cal"] = CurrentCalendarID;
          newRow["noligne"] = noligne;
          newRow["note"] = contenu;

          _dataset.Tables["notes"].Rows.Add(newRow);
        }
      }
    }

    return;
  }

  private Int32 getNextId(String tblName, String fieldName) {
    Int32 rv = 1;
    if (_dataset.Tables[tblName].Rows.Count > 0) {
      DataView v = new DataView(_dataset.Tables[tblName]);
      v.Sort = String.Format("{0} desc", fieldName);
      rv = (Int32)v[0].Row[fieldName];
      rv++;
    }

    return rv;
  }

  private void saveMonthlyNOTES() {
    if (CurrentCalendarID != 0) {
      Int32 monthNo = 1;
      for (Int32 i = 0; i < 4; i++) {
        for (Int32 j = 0; j < 3; j++) {
          DataRow[] rowz = _dataset.Tables["monthlynotes"].Select(String.Format("id_cal={0} AND calmonth={1}", CurrentCalendarID, monthNo));
          DataRow monthlyNotesRow = _dataset.Tables["monthlynotes"].NewRow();
          if (rowz.Length == 0) {
            monthlyNotesRow["id_monthlynote"] = getNextId("monthlynotes", "id_monthlynote");
            monthlyNotesRow["id_cal"] = CurrentCalendarID;
            monthlyNotesRow["calmonth"] = monthNo;

            _dataset.Tables["monthlynotes"].Rows.Add(monthlyNotesRow);
          }
          else {
            monthlyNotesRow = rowz[0];
          }
          for (Int32 k = 0; k < 3; k++) {
            String ctrlId = String.Format("month_note_{0}_{1}", monthNo, k + 1);
            TextBox box = (TextBox)FindControl(ctrlId);
            if (box != null) {
              String fieldName = String.Format("note_{0}", k + 1);
              monthlyNotesRow[fieldName] = box.Text;
            }
          }
          monthNo++;
        }
      }

      Cache["dataset"] = _dataset;
    }

    return;
  }

  private void dataSave() {
    String xmlFileName = getXmlFileName();
    if (xmlFileName != String.Empty) {

      loadDataset();
      saveCALS();
      saveNOTES();
      saveMonthlyNOTES();
      _dataset.WriteXml(xmlFileName, XmlWriteMode.WriteSchema);

    }
    else
      throw new Exception("Impossible de retrouver le fichier de données du calendrier");

    return;
  }

  private void createDatasetStructure() {
    if (_dataset != null) {
      // table cals
      DataTable cals = new DataTable("cals");
      cals.Columns.Add(new DataColumn("id_cal", System.Type.GetType("System.Int32")));
      cals.Columns.Add(new DataColumn("calyear", System.Type.GetType("System.Int32")));
      cals.Columns.Add(new DataColumn("date_mod", System.Type.GetType("System.DateTime")));

      _dataset.Tables.Add(cals);

      // table notes
      DataTable notes = new DataTable("notes");
      notes.Columns.Add(new DataColumn("id_note", System.Type.GetType("System.Int32")));
      notes.Columns.Add(new DataColumn("id_cal", System.Type.GetType("System.Int32")));
      notes.Columns.Add(new DataColumn("noligne", System.Type.GetType("System.Int32")));
      notes.Columns.Add(new DataColumn("note", System.Type.GetType("System.String")));

      _dataset.Tables.Add(notes);

      // table monthlynotes
      DataTable monthlynotes = new DataTable("monthlynotes");
      monthlynotes.Columns.Add(new DataColumn("id_monthlynote", System.Type.GetType("System.Int32")));
      monthlynotes.Columns.Add(new DataColumn("id_cal", System.Type.GetType("System.Int32")));
      monthlynotes.Columns.Add(new DataColumn("calmonth", System.Type.GetType("System.Int32")));
      addNotesLines(monthlynotes, _nbLigneNoteMois);

      _dataset.Tables.Add(monthlynotes);

      // table dailynotes
      DataTable dailynotes = new DataTable("dailynotes");
      dailynotes.Columns.Add(new DataColumn("id_dailynote", System.Type.GetType("System.Int32")));
      dailynotes.Columns.Add(new DataColumn("id_cal", System.Type.GetType("System.Int32")));
      dailynotes.Columns.Add(new DataColumn("notedate", System.Type.GetType("System.DateTime")));
      dailynotes.Columns.Add(new DataColumn("note", System.Type.GetType("System.String")));

      _dataset.Tables.Add(dailynotes);
    }

    return;
  }

  private void addNotesLines(DataTable tbl, Int32 count) {
    for (Int32 i = 0; i < count; i++) {
      String fieldName = String.Format("note_{0}", i + 1);
      tbl.Columns.Add(new DataColumn(fieldName, System.Type.GetType("System.String")));
    }

    return;
  }

  private void loadCalspot() {
    Calendar calDaily = getCal(0);
    this.plhDailyEditorCal.Controls.Clear();
    this.plhDailyEditorCal.Controls.Add(calDaily);

    this.tblCalspot.Rows.Clear();
    TableRow r = null;
    TableCell c = null;
    Int32 m = 0;
    for (Int32 i = 0; i < 4; i++) {
      r = new TableRow();
      for (Int32 j = 0; j < 3; j++) {
        c = new TableCell();
        Calendar cal = getCal(m++);
        c.Controls.Add(cal);
        r.Cells.Add(c);
      }
      this.tblCalspot.Rows.Add(r);
      ajouteNotesMois(m - 3);
    }

    return;
  }

  private void redrawCalendar() {
    Int32 m = 0;
    for (Int32 i = 0; i < 8; i = i + 2) {
      for (Int32 j = 0; j < 3; j++) {
        TableCell c = this.tblCalspot.Rows[i].Cells[j];
        Calendar cal = (Calendar)c.Controls[0];
        cal.VisibleDate = new DateTime(_startYear, 1, 1).AddMonths(m++);
      }
    }

    return;
  }

  /// <summary>
  /// Ajoute une ligne de note sous une ligne de calendrier (3 mois)
  /// </summary>
  private void ajouteNotesMois(Int32 premierMois) {
    TableRow r = new TableRow();
    for (Int32 i = 0; i < 3; i++) {
      TableCell c = new TableCell();
      Table t = getNotesMoisTable(premierMois + i);
      c.Controls.Add(t);
      r.Cells.Add(c);
    }

    this.tblCalspot.Rows.Add(r);

    return;
  }

  private Table getNotesMoisTable(Int32 noMois) {
    Table t = new Table();
    t.Width = new Unit(100, UnitType.Percentage);
    for (Int32 i = 0; i < _nbLigneNoteMois; i++) {
      TableRow r = new TableRow();
      TableCell c = new TableCell();
      c.Style.Add("border-bottom", "solid 1px black");
      c.Height = new Unit(_cellHeightNoteMois, UnitType.Pixel);
      TextBox tb = new TextBox();
      tb.ID = String.Format("month_note_{0}_{1}", noMois + 1, i + 1);
      tb.Style.Add("border", "0");
      tb.Style.Add("height", (_cellHeightNoteMois - 2).ToString());
      tb.Style.Add("width", "100%");
      tb.Style.Add("font-size", "7pt");
      c.Controls.Add(tb);
      r.Cells.Add(c);
      t.Rows.Add(r);
    }

    return t;
  }

  private Calendar getCal(Int32 nbMonthsToAdd) {
    Calendar cal = new Calendar();
    cal.VisibleDate = new DateTime(_startYear, 1, 1).AddMonths(nbMonthsToAdd);
    cal.ShowNextPrevMonth = false;
    cal.SelectionMode = CalendarSelectionMode.Day;
    cal.OtherMonthDayStyle.ForeColor = System.Drawing.Color.White;
    cal.Font.Size = new FontUnit(8, UnitType.Point);
    cal.ShowGridLines = true;
    cal.DayStyle.ForeColor = System.Drawing.Color.Crimson;
    cal.WeekendDayStyle.ForeColor = System.Drawing.Color.BlueViolet;
    cal.WeekendDayStyle.Font.Bold = true;
    cal.WeekendDayStyle.Font.Italic = true;
    cal.DayHeaderStyle.BackColor = System.Drawing.Color.LightSteelBlue;
    cal.TitleStyle.CssClass = "calmois";
    cal.SelectedDayStyle.CssClass = "selday";
    cal.SelectedDayStyle.ForeColor = System.Drawing.Color.Black;
    cal.TitleFormat = TitleFormat.Month;
    cal.DayRender += new DayRenderEventHandler(cal_DayRender);
    cal.SelectionChanged += new EventHandler(cal_SelectionChanged);
    cal.DayNameFormat = DayNameFormat.Shortest;
    cal.DayHeaderStyle.CssClass = "calmois";
    cal.BorderStyle = BorderStyle.Solid;
    cal.BorderColor = System.Drawing.Color.Gray;
    cal.BorderWidth = new Unit(2, UnitType.Pixel);
    cal.ToolTip = "Mois #" + (nbMonthsToAdd + 1);

    return cal;
  }

  void cal_SelectionChanged(Object sender, EventArgs e) {
    Calendar cal = (Calendar)sender;
    DateTime d = cal.SelectedDate;
    Calendar newCal = (Calendar)this.plhDailyEditorCal.Controls[0];
    newCal.SelectionMode = CalendarSelectionMode.None;
    newCal.SelectedDate = d;
    newCal.VisibleDate = d;
    this.lblDateSel.Text = d.ToString("yyyy-MM-dd");
    this.pnlDailyEditor.Visible = true;
    loadDataset();
    DataRow[] rowz = _dataset.Tables["dailynotes"].Select(String.Format("id_cal={0} AND notedate='{1}'", CurrentCalendarID, d.ToShortDateString()));
    if (rowz.Length > 0) {
      this.txtDailyNotes.Text = rowz[0]["note"].ToString();
      this.btnCopyDailyNoteToNotepad.Enabled = true;
    }
    else {
      this.txtDailyNotes.Text = String.Empty;
      this.btnCopyDailyNoteToNotepad.Enabled = false;
    }

    // retire la selection pour pouvoir éditer 2 fois en ligne la même date.
    cal.SelectedDate = DateTime.MinValue;

    return;
  }
  
  void cal_DayRender(Object sender, DayRenderEventArgs e) {
    e.Cell.Width = new Unit(_cellWidth, UnitType.Pixel);
    e.Cell.Height = new Unit(_cellHeight, UnitType.Pixel);
    Color noColorBG = System.Drawing.Color.FromArgb(0xc0, 0xc0, 0xc0);

    if (!e.Day.IsOtherMonth) {

      int weekNum = weekNumber(e.Day.Date);
      bool semainePaye = weekNum % 2 == (this.chkOtherWeek.Checked ? 0 : 1);
      Color myBgColor = _PapaColor;

      if (this.rad2sem.Checked) {
        if (e.Day.Date.DayOfWeek == _firstDayOfSwitch) {
          _weekBrakeValue++;
        }

        if (_weekBrakeValue == 4) {
          _weekBrakeValue = 0;
        }

        if (_weekBrakeValue == 0 || _weekBrakeValue == 1) {
          myBgColor = _PapaColor;
        }
        else {
          myBgColor = _MamaColor;
        }
      }
      else {
        if (weekNum % 2 == 0) {
          myBgColor = _PapaColor;
        }
        else {
          myBgColor = _MamaColor;
        }
      }

      if (this.chkInvertColor.Checked) {
        if (myBgColor == _PapaColor) {
          myBgColor = _MamaColor;
        }
        else {
          myBgColor = _PapaColor;
        }
      }

      if (this.chkHilitePayDay.Checked && e.Day.Date.DayOfWeek == selectedDayOfWeek() && semainePaye)
        e.Cell.BackColor = System.Drawing.Color.Yellow;
      else
        e.Cell.BackColor = this.chkNoColor.Checked ? noColorBG : myBgColor;

      loadDataset();
      DataRow[] rowz = _dataset.Tables["dailynotes"].Select(String.Format("id_cal={0} AND notedate='{1}'", CurrentCalendarID, e.Day.Date.ToShortDateString()));
      if (rowz.Length > 0) {
        String note = rowz[0]["note"].ToString();
        if (!String.IsNullOrEmpty(note)) {
          e.Cell.Style.Add("background-image", "url(star.gif)");
          e.Cell.Style.Add("background-position", "center");
          e.Cell.Style.Add("background-repeat", "no-repeat");
          e.Cell.ForeColor = System.Drawing.Color.Black;
          e.Cell.ToolTip = note;
        }
      }
    }
    else {
      e.Cell.BackColor = e.Cell.ForeColor = System.Drawing.Color.White;
    }

    return;
  }

  private DayOfWeek selectedDayOfWeek() {
    DayOfWeek dow = DayOfWeek.Thursday;

    if (ddoJourDePaye.SelectedIndex == 0)
      dow = DayOfWeek.Sunday;
    else if (ddoJourDePaye.SelectedIndex == 1)
      dow = DayOfWeek.Monday;
    else if (ddoJourDePaye.SelectedIndex == 2)
      dow = DayOfWeek.Tuesday;
    else if (ddoJourDePaye.SelectedIndex == 3)
      dow = DayOfWeek.Wednesday;
    else if (ddoJourDePaye.SelectedIndex == 4)
      dow = DayOfWeek.Thursday;
    else if (ddoJourDePaye.SelectedIndex == 5)
      dow = DayOfWeek.Friday;
    else if (ddoJourDePaye.SelectedIndex == 6)
      dow = DayOfWeek.Saturday;

    return dow;
  }

  public Int32 weekNumber(DateTime dt) {
    CultureInfo culture = CultureInfo.CurrentCulture;
    Int32 weekNum = culture.Calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstDay, _firstDayOfSwitch);

    return weekNum;
  }

  private void loadNotepad() {
    this.tblNotepad.Width = new Unit(100, UnitType.Percentage);
    for (Int32 i = 0; i < _nbLineNotepad; i++) {
      TableRow r = new TableRow();
      TableCell c = new TableCell();
      c.Style.Add("border-bottom", "solid 1px black");
      c.Height = new Unit(_cellHeight, UnitType.Pixel);
      if (i == 0) {
        c.Style.Add("text-align", "center");
        c.Text = "Notes";
      }
      else {
        TextBox notepadTextBox = new TextBox();
        notepadTextBox.ID = String.Format("notepadTextBox_{0}", i + 1);
        notepadTextBox.Style.Add("border", "0");
        notepadTextBox.Style.Add("width", "100%");
        notepadTextBox.Style.Add("font-size", "7pt");
        c.Controls.Add(notepadTextBox);
      }
      r.Cells.Add(c);
      this.tblNotepad.Rows.Add(r);
    }

    return;
  }

  private void initNotepad() {
    wipeNotepad();
    if (_dataset != null && _dataset.Tables.Contains("notes")) {
      DataRow[] rowz = _dataset.Tables["notes"].Select("id_cal=" + CurrentCalendarID.ToString());
      foreach (DataRow r in rowz) {
        TextBox t = (TextBox)FindControl(String.Format("notepadTextBox_{0}", r["noligne"]));
        if (t != null) {
          t.Text = r["note"].ToString();
        }
      }
    }

    return;
  }

  private void wipeNotepad() {
    for (Int32 i = 1; i < _nbLineNotepad; i++) {
      TextBox t = (TextBox)FindControl(String.Format("notepadTextBox_{0}", i + 1));
      if (t != null) {
        t.Text = String.Empty;
      }
    }

    return;
  }

  protected void lbRightLeft_Command(Object sender, CommandEventArgs e) {
    if (this.txtCalYear.Text != String.Empty) {
      dataSave();
      Int32 y = 0;
      if (Int32.TryParse(this.txtCalYear.Text, out y)) {

        if (e.CommandArgument.ToString().ToLower() == "left")
          y--;
        else
          y++;

        this.txtCalYear.Text = y.ToString();
        reloadAll();
      }
      else
        this.txtCalYear.Text = _startYear.ToString();
    }

    return;
  }

  //protected void chkInvertColor_CheckedChanged(Object sender, EventArgs e) {
  //  if (this.chkInvertColor.Checked) {
  //    _PapaColor = System.Drawing.Color.Pink;
  //    _MamaColor = System.Drawing.Color.LightSkyBlue;
  //  }
  //  else {
  //    _PapaColor = System.Drawing.Color.LightSkyBlue;
  //    _MamaColor = System.Drawing.Color.Pink;
  //  }
  //  dataSave();
  //  reloadAll();

  //  return;
  //}

  protected void chkDecalage_CheckedChanged(object sender, EventArgs e) {
    _weekBrakeValue = (this.chkDecalage.Checked ? 1 : 0);
    dataSave();
    reloadAll();

    return;
  }

  protected void radnbsem_CheckedChanged(object sender, EventArgs e) {
    return;
  }

  protected void radg1_CheckedChanged(Object sender, EventArgs e) {
    reloadAll();
    return;
  }

  protected void btnCalYear_Click(Object sender, EventArgs e) {
    dataSave();
    reloadAll();

    return;
  }

  protected void btnFermerDailyEditor_Click(Object sender, EventArgs e) {
    this.pnlDailyEditor.Visible = false;

    return;
  }

  protected void btnSaveDailyNote_Click(Object sender, EventArgs e) {
    this.pnlDailyEditor.Visible = false;
    Calendar cal = (Calendar)this.plhDailyEditorCal.Controls[0];
    DateTime d = cal.SelectedDate;
    loadDataset();
    DataRow dailyRow = _dataset.Tables["dailynotes"].NewRow();
    DataRow[] rowz = _dataset.Tables["dailynotes"].Select(String.Format("id_cal={0} AND notedate='{1}'", CurrentCalendarID, d.ToShortDateString()));
    if (rowz.Length > 0) {
      dailyRow = rowz[0];
    }
    else {
      dailyRow["id_dailynote"] = getNextId("dailynotes", "id_dailynote");
      dailyRow["id_cal"] = CurrentCalendarID;
      dailyRow["notedate"] = d;
      _dataset.Tables["dailynotes"].Rows.Add(dailyRow);
    }

    dailyRow["note"] = this.txtDailyNotes.Text;
    dataSave();

    return;
  }

  protected void btnCopyDailyNoteToNotepad_Click(Object sender, EventArgs e) {
    for (Int32 i = 1; i < _nbLineNotepad; i++) {
      TextBox t = (TextBox)FindControl(String.Format("notepadTextBox_{0}", i + 1));
      if (t != null && t.Text == String.Empty) {
        Calendar cal = (Calendar)this.plhDailyEditorCal.Controls[0];
        String stuff = this.txtDailyNotes.Text;
        t.ToolTip = stuff;
        if (stuff.Length > 45)
          stuff = stuff.Substring(0, 45) + " ...";
        t.Text = String.Format("{0} - {1}", cal.SelectedDate.ToString("MMM-dd"), stuff);
        break;
      }
    }

    return;
  }

  #region "Les propriétés"

  public Int32 CurrentCalendarID {
    get {
      return Convert.ToInt32(this.hid_cal.Value);
    }
    set {
      this.hid_cal.Value = value.ToString();
    }
  }

  #endregion

} // class
