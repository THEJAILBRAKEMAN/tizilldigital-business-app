using System.Windows.Forms;
using TiZillDigital.Database;
using TiZillDigital.Forms.Dialogs;
using TiZillDigital.Helpers;
using TiZillDigital.Models;
using TiZillDigital.Printing;

namespace TiZillDigital.Forms;

public class ExpenditurePanel : UserControl
{
    private readonly DatabaseManager _db; private readonly DataGridView _grid = new(); private readonly TextBox _search = new(); private readonly ComboBox _cat = new(); private readonly DateTimePicker _month = new(); private readonly Label _monthTotal = new(); private readonly Label _all = new(); private readonly Label _top = new(); private readonly Label _count = new(); private List<Expenditure> _data = new();
    public ExpenditurePanel(DatabaseManager db) { _db = db; InitializeComponent(); Wire(); LoadData(); }
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill; var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 }; root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        var ts = new ToolStrip(); ts.Items.Add("+ Add Expense", null, (_, _) => AddExpense()); ts.Items.Add("🖨 Print Report", null, (_, _) => PrintReport()); ts.Items.Add("📊 Export CSV", null, (_, _) => ExportCsv()); ts.Items.Add(new ToolStripLabel("Search:")); ts.Items.Add(new ToolStripControlHost(_search)); ts.Items.Add(new ToolStripLabel("Category:")); _cat.Items.AddRange(["All", "Inventory", "Transport", "Marketing", "Utilities", "Salary", "Tools & Software", "Misc"]); _cat.SelectedIndex = 0; ts.Items.Add(new ToolStripControlHost(_cat)); ts.Items.Add(new ToolStripLabel("Month:")); _month.Format = DateTimePickerFormat.Custom; _month.CustomFormat = "MMMM yyyy"; ts.Items.Add(new ToolStripControlHost(_month));
        _grid.Dock = DockStyle.Fill; _grid.ReadOnly = true; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.AllowUserToAddRows = _grid.AllowUserToDeleteRows = false; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.ContextMenuStrip = new ContextMenuStrip(); _grid.ContextMenuStrip.Items.Add("Edit", null, (_, _) => Edit()); _grid.ContextMenuStrip.Items.Add("Delete", null, (_, _) => Delete()); _grid.ContextMenuStrip.Items.Add("Print Row", null, (_, _) => PrintRow());
        var gb = new GroupBox { Dock = DockStyle.Fill, Text = "Stats" }; var fl = new FlowLayoutPanel { Dock = DockStyle.Fill }; fl.Controls.AddRange([new Label { Text = "This Month:" }, _monthTotal, new Label { Text = "All-Time:" }, _all, new Label { Text = "Top Category:" }, _top, new Label { Text = "Record Count:" }, _count]); gb.Controls.Add(fl);
        root.Controls.Add(ts, 0, 0); root.Controls.Add(_grid, 0, 1); root.Controls.Add(gb, 0, 2); Controls.Add(root);
    }
    private void Wire() { _search.TextChanged += (_, _) => Bind(); _cat.SelectedIndexChanged += (_, _) => Bind(); _month.ValueChanged += (_, _) => Bind(); _grid.DoubleClick += (_, _) => Edit(); _grid.KeyDown += Keys; }
    public void LoadData() { _data = _db.GetExpenditures(); Bind(); }
    private void Bind()
    {
        var m = _month.Value; IEnumerable<Expenditure> q = _data.Where(x => DateTime.Parse(x.ExpenseDate).Month == m.Month && DateTime.Parse(x.ExpenseDate).Year == m.Year);
        if (!string.IsNullOrWhiteSpace(_search.Text)) q = q.Where(x => x.Description.Contains(_search.Text, StringComparison.OrdinalIgnoreCase));
        if (_cat.Text is not "All" and not "") q = q.Where(x => x.Category == _cat.Text);
        _grid.DataSource = q.Select(x => new { x.Id, x.Description, x.Category, Amount = FormatHelper.FormatMUR(x.Amount), Date = FormatHelper.FormatDate(x.ExpenseDate), x.Notes, Actions = "..." }).ToList();
        _monthTotal.Text = FormatHelper.FormatMUR(q.Sum(x => x.Amount)); _all.Text = FormatHelper.FormatMUR(_data.Sum(x => x.Amount)); _top.Text = _data.GroupBy(x => x.Category).OrderByDescending(g => g.Sum(s => s.Amount)).FirstOrDefault()?.Key ?? "N/A"; _count.Text = q.Count().ToString();
    }
    private Expenditure? Sel() => _grid.CurrentRow == null ? null : _data.FirstOrDefault(x => x.Id == _grid.CurrentRow.Cells["Id"].Value?.ToString());
    private void AddExpense() { using var d = new ExpenditureDialog(); if (d.ShowDialog() == DialogResult.OK) { _db.AddExpenditure(d.Value); LoadData(); } }
    private void Edit() { var s = Sel(); if (s == null) return; using var d = new ExpenditureDialog(s); if (d.ShowDialog() == DialogResult.OK) { _db.UpdateExpenditure(d.Value); LoadData(); } }
    private void Delete() { var s = Sel(); if (s == null) return; if (MessageBox.Show("Delete expense?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) { _db.DeleteExpenditure(s.Id); LoadData(); } }
    private void PrintRow() { var s = Sel(); if (s == null) return; ReportPrinter.PrintLines("Expense", [$"{s.Description} - {FormatHelper.FormatMUR(s.Amount)}"]); }
    private void PrintReport() { var set = _db.GetSettings(); var tp = new ThermalPrinter(_db); var rb = new ReceiptBuilder(set["business_name"], set["address"], set["receipt_footer"]); tp.Print(rb.PrintExpenseSummary(_data, new DateRange(_month.Value.ToString("dd MMM yyyy"), DateTime.Now.ToString("dd MMM yyyy")))); }
    private void ExportCsv() { using var sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "expenditure.csv" }; if (sfd.ShowDialog() == DialogResult.OK) { _db.ExportToCsv("expenditure", sfd.FileName); MessageBox.Show("Exported to " + sfd.FileName); } }
    private void Keys(object? s, KeyEventArgs e) { if (e.Control && e.KeyCode == Keys.N) AddExpense(); else if (e.Control && e.KeyCode == Keys.E) Edit(); else if (e.KeyCode == Keys.Delete) Delete(); else if (e.KeyCode == Keys.F5) LoadData(); else if (e.KeyCode == Keys.Enter) Edit(); else if (e.KeyCode == Keys.Escape) { _search.Clear(); _grid.ClearSelection(); } }
}
