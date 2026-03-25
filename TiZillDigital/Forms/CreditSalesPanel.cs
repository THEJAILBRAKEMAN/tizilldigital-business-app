using System.Drawing;
using System.Windows.Forms;
using TiZillDigital.Database;
using TiZillDigital.Forms.Dialogs;
using TiZillDigital.Helpers;
using TiZillDigital.Models;
using TiZillDigital.Printing;

namespace TiZillDigital.Forms;

public class CreditSalesPanel : UserControl
{
    private readonly DatabaseManager _db;
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new();
    private readonly ComboBox _status = new();
    private readonly Label _totalOut = new();
    private readonly Label _totalPaid = new();
    private readonly Label _active = new();
    private List<CreditSale> _data = new();

    public CreditSalesPanel(DatabaseManager db) { _db = db; InitializeComponent(); WireEvents(); LoadData(); }
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill; var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 }; root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        var ts = new ToolStrip();
        ts.Items.Add("+ New Credit Sale", null, (_, _) => NewSale()); ts.Items.Add("💳 Record Payment", null, (_, _) => RecordPayment()); ts.Items.Add("🖨 Print Receipt", null, (_, _) => PrintReceipt()); ts.Items.Add("📄 Print Statement", null, (_, _) => PrintStatement());
        ts.Items.Add(new ToolStripLabel("Search:")); ts.Items.Add(new ToolStripControlHost(_search)); _search.Width = 200;
        ts.Items.Add(new ToolStripLabel("Status:")); _status.Items.AddRange(new[] { "All", "Owing", "Settled" }); _status.SelectedIndex = 0; ts.Items.Add(new ToolStripControlHost(_status));
        ts.Items.Add("Refresh", null, (_, _) => LoadData());

        ConfigureGrid(_grid);
        _grid.ContextMenuStrip = new ContextMenuStrip();
        _grid.ContextMenuStrip.Items.Add("Edit", null, (_, _) => EditSelected());
        _grid.ContextMenuStrip.Items.Add("Record Payment", null, (_, _) => RecordPayment());
        _grid.ContextMenuStrip.Items.Add("Print Receipt", null, (_, _) => PrintReceipt());
        _grid.ContextMenuStrip.Items.Add("Delete", null, (_, _) => DeleteSelected());

        var stats = new GroupBox { Dock = DockStyle.Fill, Text = "Stats" };
        var f = new FlowLayoutPanel { Dock = DockStyle.Fill };
        f.Controls.Add(new Label { Text = "Total Outstanding:" }); f.Controls.Add(_totalOut); f.Controls.Add(new Label { Text = "Total Collected:" }); f.Controls.Add(_totalPaid); f.Controls.Add(new Label { Text = "Active Accounts:" }); f.Controls.Add(_active);
        stats.Controls.Add(f);

        root.Controls.Add(ts, 0, 0); root.Controls.Add(_grid, 0, 1); root.Controls.Add(stats, 0, 2); Controls.Add(root);
    }
    private static void ConfigureGrid(DataGridView g)
    {
        g.Dock = DockStyle.Fill; g.ReadOnly = true; g.SelectionMode = DataGridViewSelectionMode.FullRowSelect; g.AllowUserToAddRows = false; g.AllowUserToDeleteRows = false; g.MultiSelect = false; g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }
    private void WireEvents()
    {
        _search.TextChanged += (_, _) => BindGrid(); _status.SelectedIndexChanged += (_, _) => BindGrid(); _grid.DoubleClick += (_, _) => EditSelected(); _grid.KeyDown += GridKeyDown;
        _grid.CellFormatting += (_, e) => { if (_grid.Columns[e.ColumnIndex].Name == "Outstanding" && decimal.TryParse((_grid.Rows[e.RowIndex].Cells["OutstandingRaw"].Value ?? "0").ToString(), out var o)) e.CellStyle.ForeColor = o > 0 ? Color.Red : Color.Green; };
    }
    public void LoadData() { _data = _db.GetCreditSales(); BindGrid(); }
    private void BindGrid()
    {
        IEnumerable<CreditSale> q = _data;
        if (!string.IsNullOrWhiteSpace(_search.Text)) q = q.Where(x => x.Customer.Contains(_search.Text, StringComparison.OrdinalIgnoreCase) || x.Item.Contains(_search.Text, StringComparison.OrdinalIgnoreCase));
        if (_status.SelectedItem?.ToString() == "Owing") q = q.Where(x => x.Outstanding > 0);
        if (_status.SelectedItem?.ToString() == "Settled") q = q.Where(x => x.Outstanding <= 0);
        _grid.DataSource = q.Select(x => new { x.Id, x.Customer, x.Phone, x.Item, Amount = FormatHelper.FormatMUR(x.Amount), Paid = FormatHelper.FormatMUR(x.Paid), Outstanding = FormatHelper.FormatMUR(x.Outstanding), OutstandingRaw = x.Outstanding, Date = FormatHelper.FormatDate(x.SaleDate), Status = x.Outstanding > 0 ? "Owing" : "Settled", Actions = "..." }).ToList();
        _totalOut.Text = FormatHelper.FormatMUR(q.Sum(x => x.Outstanding)); _totalPaid.Text = FormatHelper.FormatMUR(q.Sum(x => x.Paid)); _active.Text = q.Count(x => x.Outstanding > 0).ToString();
    }
    private CreditSale? Selected() => _grid.CurrentRow == null ? null : _data.FirstOrDefault(x => x.Id == _grid.CurrentRow.Cells["Id"].Value?.ToString());
    private void NewSale() { using var d = new CreditSaleDialog(); if (d.ShowDialog() == DialogResult.OK) { _db.AddCreditSale(d.Value); LoadData(); } }
    private void EditSelected() { var s = Selected(); if (s == null) return; using var d = new CreditSaleDialog(s); if (d.ShowDialog() == DialogResult.OK) { _db.UpdateCreditSale(d.Value); LoadData(); } }
    private void DeleteSelected() { var s = Selected(); if (s == null) return; if (MessageBox.Show("Delete selected sale?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) { _db.DeleteCreditSale(s.Id); LoadData(); } }
    private void RecordPayment() { var s = Selected(); if (s == null) return; using var d = new PaymentDialog(s); if (d.ShowDialog() == DialogResult.OK) { _db.RecordPayment(d.Value); LoadData(); } }
    private void PrintReceipt() { var s = Selected(); if (s == null) return; var set = _db.GetSettings(); var tp = new ThermalPrinter(_db); var rb = new ReceiptBuilder(set["business_name"], set["address"], set["receipt_footer"]); tp.Print(rb.PrintCreditReceipt(s)); }
    private void PrintStatement() { var s = Selected(); if (s == null) return; ReportPrinter.PrintLines("Credit Statement", ["Customer: " + s.Customer, "Item: " + s.Item, "Amount: " + FormatHelper.FormatMUR(s.Amount), "Paid: " + FormatHelper.FormatMUR(s.Paid), "Outstanding: " + FormatHelper.FormatMUR(s.Outstanding)]); }
    private void GridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.N) NewSale(); else if (e.Control && e.KeyCode == Keys.E) EditSelected(); else if (e.Control && e.KeyCode == Keys.P) PrintReceipt(); else if (e.KeyCode == Keys.Delete) DeleteSelected(); else if (e.KeyCode == Keys.F5) LoadData(); else if (e.KeyCode == Keys.Enter) EditSelected(); else if (e.KeyCode == Keys.Escape) { _search.Clear(); _grid.ClearSelection(); }
    }
}
