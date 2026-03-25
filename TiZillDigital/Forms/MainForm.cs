using System.Drawing;
using System.Windows.Forms;
using TiZillDigital.Database;
using TiZillDigital.Forms.Dialogs;

namespace TiZillDigital.Forms;

public class MainForm : Form
{
    private readonly DatabaseManager _db;
    private readonly Panel _content = new() { Dock = DockStyle.Fill };
    private readonly ToolStripStatusLabel _moduleStatus = new("Dashboard");
    private readonly ToolStripStatusLabel _recordStatus = new("Records: 0");
    private readonly ToolStripStatusLabel _actionStatus = new("Ready");
    private readonly ToolStripStatusLabel _timeStatus = new(DateTime.Now.ToString("T"));
    private readonly Dictionary<string, Button> _navButtons = new();

    public MainForm(DatabaseManager db)
    {
        _db = db;
        InitializeComponent();
        WireEvents();
        ShowPanel("Dashboard");
    }

    private void InitializeComponent()
    {
        Text = "TI ZILL DIGITAL Suite";
        Width = 1400; Height = 900; StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        var menu = BuildMenu();
        var sidebar = BuildSidebar();
        var status = new StatusStrip();
        status.Items.AddRange(new ToolStripItem[] { _moduleStatus, new ToolStripStatusLabel("|"), _recordStatus, new ToolStripStatusLabel("|"), _actionStatus, new ToolStripStatusLabel { Spring = true }, _timeStatus });

        Controls.Add(_content);
        Controls.Add(sidebar);
        Controls.Add(status);
        MainMenuStrip = menu;
        Controls.Add(menu);
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("File");
        file.DropDownItems.Add("New Record", null, (_, _) => ForwardShortcut(Keys.Control | Keys.N)).ShortcutKeys = Keys.Control | Keys.N;
        file.DropDownItems.Add("Print", null, (_, _) => ForwardShortcut(Keys.Control | Keys.P)).ShortcutKeys = Keys.Control | Keys.P;
        file.DropDownItems.Add("Print Preview", null, (_, _) => ForwardShortcut(Keys.Control | Keys.Shift | Keys.P));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Exit", null, (_, _) => Close());

        var view = new ToolStripMenuItem("View");
        view.DropDownItems.Add("Refresh", null, (_, _) => ForwardShortcut(Keys.F5)).ShortcutKeys = Keys.F5;
        foreach (var m in new[] { "Dashboard", "Credit Sales", "Expenditure", "Key Delivery", "Game Box Crawler" })
            view.DropDownItems.Add(m, null, (_, _) => ShowPanel(m));

        var tools = new ToolStripMenuItem("Tools");
        tools.DropDownItems.Add("Settings", null, (_, _) => new SettingsDialog(_db).ShowDialog(this));
        tools.DropDownItems.Add("Printer Setup", null, (_, _) => new SettingsDialog(_db).ShowDialog(this));
        tools.DropDownItems.Add("Export to CSV", null, (_, _) => ExportCsvPrompt());
        tools.DropDownItems.Add("Backup Database", null, (_, _) => MessageBox.Show("Backup database file: tizill.db", "Backup"));

        var help = new ToolStripMenuItem("Help");
        help.DropDownItems.Add("About TI ZILL DIGITAL", null, (_, _) => MessageBox.Show("TI ZILL DIGITAL Suite\nVersion 1.0.0", "About"));
        menu.Items.AddRange(new[] { file, view, tools, help });
        return menu;
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 200, BackColor = Color.FromArgb(27, 26, 25), Padding = new Padding(8, 12, 8, 12) };
        var stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        stack.Controls.Add(CreateNavButton("⊞  Dashboard", "Dashboard"));
        stack.Controls.Add(new Label { Text = "── BUSINESS ──", ForeColor = Color.Gainsboro, Width = 170 });
        stack.Controls.Add(CreateNavButton("💳 Credit Sales", "Credit Sales"));
        stack.Controls.Add(CreateNavButton("📊 Expenditure", "Expenditure"));
        stack.Controls.Add(CreateNavButton("🔑 Key Delivery", "Key Delivery"));
        stack.Controls.Add(new Label { Text = "── TOOLS ──", ForeColor = Color.Gainsboro, Width = 170 });
        stack.Controls.Add(CreateNavButton("🎮 Game Box Crawler", "Game Box Crawler"));
        sidebar.Controls.Add(stack);
        return sidebar;
    }

    private Button CreateNavButton(string text, string key)
    {
        var b = new Button { Text = text, Width = 170, Height = 44, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.FromArgb(27, 26, 25), ForeColor = Color.FromArgb(200, 198, 196), TabStop = true };
        b.FlatAppearance.BorderSize = 0;
        b.Click += (_, _) => ShowPanel(key);
        _navButtons[key] = b;
        return b;
    }

    private void ShowPanel(string module)
    {
        UserControl panel = module switch
        {
            "Dashboard" => new DashboardPanel(_db, ShowPanel),
            "Credit Sales" => new CreditSalesPanel(_db),
            "Expenditure" => new ExpenditurePanel(_db),
            "Key Delivery" => new KeyDeliveryPanel(_db),
            _ => new GameCrawlerPanel(_db)
        };
        _content.Controls.Clear();
        panel.Dock = DockStyle.Fill;
        _content.Controls.Add(panel);
        _moduleStatus.Text = module;
        _recordStatus.Text = $"Records: {ExtractCount(panel)}";
        _actionStatus.Text = "Loaded";
        foreach (var kv in _navButtons) kv.Value.BackColor = kv.Key == module ? SystemColors.Highlight : Color.FromArgb(27, 26, 25);
        foreach (var kv in _navButtons) kv.Value.ForeColor = kv.Key == module ? Color.White : Color.FromArgb(200, 198, 196);
    }

    private int ExtractCount(UserControl panel) => panel.Controls.OfType<DataGridView>().FirstOrDefault()?.Rows.Count ?? 0;

    private void WireEvents()
    {
        var timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += (_, _) => _timeStatus.Text = DateTime.Now.ToString("T");
        timer.Start();
    }

    private void ForwardShortcut(Keys keys)
    {
        _content.Controls.OfType<Control>().FirstOrDefault()?.Focus();
        SendKeys.SendWait(ToSendKeys(keys));
    }

    private static string ToSendKeys(Keys k) => k switch
    {
        Keys.Control | Keys.N => "^n",
        Keys.Control | Keys.P => "^p",
        Keys.F5 => "{F5}",
        _ => ""
    };

    private void ExportCsvPrompt()
    {
        var table = Microsoft.VisualBasic.Interaction.InputBox("Table name: credit_sales / expenditure / key_orders / games / game_keys", "Export CSV", "credit_sales");
        if (string.IsNullOrWhiteSpace(table)) return;
        using var sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = table + ".csv" };
        if (sfd.ShowDialog() == DialogResult.OK)
        {
            _db.ExportToCsv(table.Trim(), sfd.FileName);
            MessageBox.Show($"CSV exported:\n{sfd.FileName}", "Export");
        }
    }
}
