using System.Windows.Forms;
using TiZillDigital.Database;
using TiZillDigital.Forms.Dialogs;
using TiZillDigital.Helpers;
using TiZillDigital.Models;
using TiZillDigital.Printing;

namespace TiZillDigital.Forms;

public class GameCrawlerPanel : UserControl
{
    private readonly DatabaseManager _db; private readonly DataGridView _games = new(); private readonly DataGridView _keys = new(); private readonly TextBox _search = new(); private readonly ComboBox _platform = new(); private readonly ComboBox _genre = new(); private List<Game> _data = new(); private List<GameKey> _keyData = new();
    public GameCrawlerPanel(DatabaseManager db) { _db = db; InitializeComponent(); Wire(); LoadData(); }
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 320 };
        var tsTop = new ToolStrip(); tsTop.Items.Add("+ Add Game", null, (_, _) => AddGame()); tsTop.Items.Add("Edit", null, (_, _) => EditGame()); tsTop.Items.Add("Delete", null, (_, _) => DeleteGame()); tsTop.Items.Add("🖨 Print Catalog", null, (_, _) => PrintCatalog()); tsTop.Items.Add(new ToolStripLabel("Search:")); tsTop.Items.Add(new ToolStripControlHost(_search)); tsTop.Items.Add(new ToolStripLabel("Platform:")); _platform.Items.AddRange(["All", "Steam", "Epic Games", "GOG", "PlayStation", "Xbox", "Nintendo", "Other"]); _platform.SelectedIndex = 0; tsTop.Items.Add(new ToolStripControlHost(_platform)); tsTop.Items.Add(new ToolStripLabel("Genre:")); _genre.Items.AddRange(["All", "Action", "Adventure", "RPG", "Strategy", "Sports", "Simulation", "Other"]); _genre.SelectedIndex = 0; tsTop.Items.Add(new ToolStripControlHost(_genre));
        var tsBottom = new ToolStrip(); tsBottom.Items.Add("+ Add Key", null, (_, _) => AddKey()); tsBottom.Items.Add("Remove Key", null, (_, _) => RemoveKey()); tsBottom.Items.Add("📋 Copy Key", null, (_, _) => CopyKey()); tsBottom.Items.Add("Mark Sold", null, (_, _) => MarkSold()); tsBottom.Items.Add("🖨 Print Keys", null, (_, _) => PrintKeys());
        Configure(_games); Configure(_keys);
        _keys.ContextMenuStrip = new ContextMenuStrip(); _keys.ContextMenuStrip.Items.Add("Copy Key", null, (_, _) => CopyKey()); _keys.ContextMenuStrip.Items.Add("Mark Sold", null, (_, _) => MarkSold()); _keys.ContextMenuStrip.Items.Add("Remove", null, (_, _) => RemoveKey());
        var pTop = new Panel { Dock = DockStyle.Fill }; pTop.Controls.Add(_games); pTop.Controls.Add(tsTop); tsTop.Dock = DockStyle.Top; _games.Dock = DockStyle.Fill;
        var pBottom = new Panel { Dock = DockStyle.Fill }; pBottom.Controls.Add(_keys); pBottom.Controls.Add(tsBottom); tsBottom.Dock = DockStyle.Top; _keys.Dock = DockStyle.Fill;
        split.Panel1.Controls.Add(pTop); split.Panel2.Controls.Add(pBottom); Controls.Add(split);
    }
    private static void Configure(DataGridView g) { g.ReadOnly = true; g.SelectionMode = DataGridViewSelectionMode.FullRowSelect; g.AllowUserToAddRows = g.AllowUserToDeleteRows = false; g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; }
    private void Wire() { _search.TextChanged += (_, _) => BindGames(); _platform.SelectedIndexChanged += (_, _) => BindGames(); _genre.SelectedIndexChanged += (_, _) => BindGames(); _games.SelectionChanged += (_, _) => LoadKeys(); _games.DoubleClick += (_, _) => EditGame(); _keys.DoubleClick += (_, _) => RevealKey(); _games.KeyDown += KeysDown; _keys.KeyDown += KeysDown; _games.CellFormatting += (_, e) => { if (_games.Columns[e.ColumnIndex].Name == "Margin" || _games.Columns[e.ColumnIndex].Name == "KeysInStock") e.CellStyle.ForeColor = System.Drawing.Color.DarkGreen; }; }
    public void LoadData() { _data = _db.GetGames(); BindGames(); LoadKeys(); }
    private void BindGames()
    {
        IEnumerable<Game> q = _data;
        if (!string.IsNullOrWhiteSpace(_search.Text)) q = q.Where(g => g.Title.Contains(_search.Text, StringComparison.OrdinalIgnoreCase));
        if (_platform.Text is not "All" and not "") q = q.Where(g => g.Platform == _platform.Text);
        if (_genre.Text is not "All" and not "") q = q.Where(g => g.Genre == _genre.Text);
        _games.DataSource = q.Select(g => new { g.Id, g.Title, g.Platform, g.Genre, CostPrice = FormatHelper.FormatMUR(g.CostPrice), SellPrice = FormatHelper.FormatMUR(g.SellPrice), Margin = g.SellPrice == 0 ? 0 : Math.Round(((g.SellPrice - g.CostPrice) / g.SellPrice) * 100, 2), KeysInStock = _db.GetGameKeys(g.Id).Count(k => k.Status == "Available"), DateAdded = FormatHelper.FormatDate(g.AddedDate) }).ToList();
    }
    private Game? SelGame() => _games.CurrentRow == null ? null : _data.FirstOrDefault(g => g.Id == _games.CurrentRow.Cells["Id"].Value?.ToString());
    private GameKey? SelKey() => _keys.CurrentRow == null ? null : _keyData.FirstOrDefault(k => k.Id == _keys.CurrentRow.Cells["Id"].Value?.ToString());
    private void LoadKeys() { var g = SelGame(); _keyData = g == null ? new() : _db.GetGameKeys(g.Id); _keys.DataSource = _keyData.Select(k => new { k.Id, Key = "••••-••••-••••", RawKey = k.ProductKey, k.Status, DateAdded = FormatHelper.FormatDate(k.AddedDate), SoldDate = string.IsNullOrWhiteSpace(k.SoldDate) ? "" : FormatHelper.FormatDate(k.SoldDate!) }).ToList(); }
    private void AddGame() { using var d = new GameDialog(); if (d.ShowDialog() == DialogResult.OK) { _db.AddGame(d.Value); LoadData(); } }
    private void EditGame() { var g = SelGame(); if (g == null) return; using var d = new GameDialog(g); if (d.ShowDialog() == DialogResult.OK) { _db.UpdateGame(d.Value); LoadData(); } }
    private void DeleteGame() { var g = SelGame(); if (g == null) return; if (MessageBox.Show("Delete game and keys?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) { _db.DeleteGame(g.Id); LoadData(); } }
    private void AddKey() { var g = SelGame(); if (g == null) return; using var d = new AddKeyDialog(); if (d.ShowDialog() == DialogResult.OK) { _db.AddGameKeys(g.Id, d.Keys); LoadKeys(); BindGames(); } }
    private void RemoveKey() { var k = SelKey(); if (k == null) return; if (MessageBox.Show("Remove key?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) { _db.DeleteGameKey(k.Id); LoadKeys(); BindGames(); } }
    private void MarkSold() { var k = SelKey(); if (k == null) return; _db.MarkGameKeySold(k.Id); LoadKeys(); BindGames(); }
    private void CopyKey() { var k = SelKey(); if (k == null) return; Clipboard.SetText(k.ProductKey); }
    private void RevealKey() { var k = SelKey(); if (k == null) return; MessageBox.Show(k.ProductKey, "Key"); }
    private void PrintCatalog() { var set = _db.GetSettings(); var tp = new ThermalPrinter(_db); var rb = new ReceiptBuilder(set["business_name"], set["address"], set["receipt_footer"]); tp.Print(rb.PrintGameCatalog(_data, id => _db.GetGameKeys(id).Count(x => x.Status == "Available"))); }
    private void PrintKeys() { var g = SelGame(); if (g == null) return; ReportPrinter.PrintLines($"Keys - {g.Title}", _keyData.Select(k => $"{k.Status}: {k.ProductKey}")); }
    private void KeysDown(object? s, KeyEventArgs e) { if (e.Control && e.KeyCode == Keys.N) AddGame(); else if (e.Control && e.KeyCode == Keys.E) EditGame(); else if (e.Control && e.KeyCode == Keys.P) PrintCatalog(); else if (e.Control && e.KeyCode == Keys.K) RevealKey(); else if (e.KeyCode == Keys.Delete) RemoveKey(); else if (e.KeyCode == Keys.F5) LoadData(); else if (e.KeyCode == Keys.Enter) EditGame(); else if (e.KeyCode == Keys.Escape) { _search.Clear(); _games.ClearSelection(); _keys.ClearSelection(); } }
}
