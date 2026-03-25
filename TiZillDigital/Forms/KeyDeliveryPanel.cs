using System.Windows.Forms;
using TiZillDigital.Database;
using TiZillDigital.Forms.Dialogs;
using TiZillDigital.Helpers;
using TiZillDigital.Models;
using TiZillDigital.Printing;

namespace TiZillDigital.Forms;

public class KeyDeliveryPanel : UserControl
{
    private readonly DatabaseManager _db; private readonly DataGridView _grid = new(); private readonly TextBox _search = new(); private readonly ComboBox _status = new(); private readonly ComboBox _platform = new(); private readonly CheckBox _includeKey = new() { Text = "Include Key on Receipt" }; private List<KeyOrder> _data = new();
    public KeyDeliveryPanel(DatabaseManager db) { _db = db; InitializeComponent(); Wire(); LoadData(); }
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill; var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 }; root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var top = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true }; var ts = new ToolStrip();
        ts.Items.Add("+ New Order", null, (_, _) => AddOrder()); ts.Items.Add("✓ Mark Delivered", null, (_, _) => MarkDelivered()); ts.Items.Add("🔑 Show Key", null, (_, _) => ShowKey()); ts.Items.Add("📋 Copy Key", null, (_, _) => CopyKey()); ts.Items.Add("🖨 Print Receipt", null, (_, _) => PrintReceipt()); ts.Items.Add(new ToolStripLabel("Search:")); ts.Items.Add(new ToolStripControlHost(_search)); ts.Items.Add(new ToolStripLabel("Status:")); _status.Items.AddRange(["All", "Pending", "Delivered", "Failed", "Refunded"]); _status.SelectedIndex = 0; ts.Items.Add(new ToolStripControlHost(_status)); ts.Items.Add(new ToolStripLabel("Platform:")); _platform.Items.AddRange(["All", "Steam", "Epic Games", "GOG", "PlayStation", "Xbox", "Nintendo", "Ubisoft", "EA App", "Battle.net", "Other"]); _platform.SelectedIndex = 0; ts.Items.Add(new ToolStripControlHost(_platform));
        top.Controls.Add(ts); top.Controls.Add(_includeKey);
        _grid.Dock = DockStyle.Fill; _grid.ReadOnly = true; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.AllowUserToAddRows = _grid.AllowUserToDeleteRows = false; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.ContextMenuStrip = new ContextMenuStrip(); _grid.ContextMenuStrip.Items.Add("Edit", null, (_, _) => Edit()); _grid.ContextMenuStrip.Items.Add("Mark Delivered", null, (_, _) => MarkDelivered()); _grid.ContextMenuStrip.Items.Add("Copy Key", null, (_, _) => CopyKey()); _grid.ContextMenuStrip.Items.Add("Print Receipt", null, (_, _) => PrintReceipt()); _grid.ContextMenuStrip.Items.Add("Delete", null, (_, _) => Delete());
        root.Controls.Add(top, 0, 0); root.Controls.Add(_grid, 0, 1); Controls.Add(root);
    }
    private void Wire() { _search.TextChanged += (_, _) => Bind(); _status.SelectedIndexChanged += (_, _) => Bind(); _platform.SelectedIndexChanged += (_, _) => Bind(); _grid.DoubleClick += (_, _) => Edit(); _grid.KeyDown += Keys; _grid.CellFormatting += ColorStatus; }
    public void LoadData() { _data = _db.GetKeyOrders(); Bind(); }
    private void Bind()
    {
        IEnumerable<KeyOrder> q = _data;
        if (!string.IsNullOrWhiteSpace(_search.Text)) q = q.Where(x => x.Customer.Contains(_search.Text, StringComparison.OrdinalIgnoreCase) || x.Game.Contains(_search.Text, StringComparison.OrdinalIgnoreCase));
        if (_status.Text is not "All" and not "") q = q.Where(x => x.Status == _status.Text);
        if (_platform.Text is not "All" and not "") q = q.Where(x => x.Platform == _platform.Text);
        _grid.DataSource = q.Select(x => new { x.Id, x.Customer, x.Phone, x.Game, x.Platform, Price = FormatHelper.FormatMUR(x.Price), Key = Mask(x.ProductKey), x.OrderDate, x.Status, Actions = "..." }).ToList();
    }
    private static string Mask(string k) => string.IsNullOrWhiteSpace(k) ? "" : "••••-••••-••••";
    private KeyOrder? Sel() => _grid.CurrentRow == null ? null : _data.FirstOrDefault(x => x.Id == _grid.CurrentRow.Cells["Id"].Value?.ToString());
    private void AddOrder() { using var d = new KeyOrderDialog(); if (d.ShowDialog() == DialogResult.OK) { _db.AddKeyOrder(d.Value); LoadData(); } }
    private void Edit() { var s = Sel(); if (s == null) return; using var d = new KeyOrderDialog(s); if (d.ShowDialog() == DialogResult.OK) { _db.UpdateKeyOrder(d.Value); LoadData(); } }
    private void Delete() { var s = Sel(); if (s == null) return; if (MessageBox.Show("Delete order?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) { _db.DeleteKeyOrder(s.Id); LoadData(); } }
    private void MarkDelivered() { var s = Sel(); if (s == null) return; _db.MarkDelivered(s.Id); MessageBox.Show($"Delivered key: {s.ProductKey}"); LoadData(); }
    private void ShowKey() { var s = Sel(); if (s == null) return; MessageBox.Show(s.ProductKey, "Product Key"); }
    private void CopyKey() { var s = Sel(); if (s == null) return; Clipboard.SetText(s.ProductKey ?? ""); }
    private void PrintReceipt() { var s = Sel(); if (s == null) return; var set = _db.GetSettings(); var tp = new ThermalPrinter(_db); var rb = new ReceiptBuilder(set["business_name"], set["address"], set["receipt_footer"]); tp.Print(rb.PrintKeyDeliveryReceipt(s, _includeKey.Checked)); }
    private void ColorStatus(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_grid.Columns[e.ColumnIndex].Name != "Status") return;
        var v = e.Value?.ToString();
        if (v == "Pending") e.CellStyle.ForeColor = System.Drawing.Color.Orange;
        else if (v == "Delivered") e.CellStyle.ForeColor = System.Drawing.Color.Green;
        else if (v == "Failed") e.CellStyle.ForeColor = System.Drawing.Color.Red;
        else if (v == "Refunded") e.CellStyle.ForeColor = System.Drawing.Color.Gray;
    }
    private void Keys(object? s, KeyEventArgs e) { if (e.Control && e.KeyCode == Keys.N) AddOrder(); else if (e.Control && e.KeyCode == Keys.E) Edit(); else if (e.Control && e.KeyCode == Keys.P) PrintReceipt(); else if (e.Control && e.KeyCode == Keys.K) ShowKey(); else if (e.Control && e.KeyCode == Keys.D) MarkDelivered(); else if (e.KeyCode == Keys.Delete) Delete(); else if (e.KeyCode == Keys.F5) LoadData(); else if (e.KeyCode == Keys.Enter) Edit(); else if (e.KeyCode == Keys.Escape) { _search.Clear(); _grid.ClearSelection(); } }
}
