using System.Drawing;
using System.Windows.Forms;
using TiZillDigital.Database;
using TiZillDigital.Helpers;

namespace TiZillDigital.Forms;

public class DashboardPanel : UserControl
{
    private readonly DatabaseManager _db;
    private readonly Action<string> _navigate;
    private readonly Label _out = new();
    private readonly Label _exp = new();
    private readonly Label _del = new();
    private readonly Label _stock = new();
    private readonly DataGridView _grid = new();

    public DashboardPanel(DatabaseManager db, Action<string> navigate)
    {
        _db = db; _navigate = navigate;
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 140)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 180)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 10)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var head = new Panel { Dock = DockStyle.Fill };
        head.Controls.Add(new Label { Text = "TI ZILL DIGITAL Suite", Font = new Font("Segoe UI", 20, FontStyle.Bold), Dock = DockStyle.Top, Height = 42 });
        head.Controls.Add(new Label { Text = DateTime.Now.ToLongDateString(), Dock = DockStyle.Top, Height = 24, ForeColor = SystemColors.GrayText });

        var cards = new FlowLayoutPanel { Dock = DockStyle.Fill };
        cards.Controls.Add(MakeCard("Outstanding Credit", _out)); cards.Controls.Add(MakeCard("Total Expenditure", _exp)); cards.Controls.Add(MakeCard("Keys Delivered", _del)); cards.Controls.Add(MakeCard("Keys In Stock", _stock));

        var launch = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        launch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); launch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        launch.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); launch.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        launch.Controls.Add(MakeLaunch("Credit Sales"), 0, 0); launch.Controls.Add(MakeLaunch("Expenditure"), 1, 0); launch.Controls.Add(MakeLaunch("Key Delivery"), 0, 1); launch.Controls.Add(MakeLaunch("Game Box Crawler"), 1, 1);

        ConfigureGrid(_grid);
        root.Controls.Add(head, 0, 0); root.Controls.Add(cards, 0, 1); root.Controls.Add(launch, 0, 2); root.Controls.Add(new Panel(), 0, 3); root.Controls.Add(_grid, 0, 4);
        Controls.Add(root);
    }

    private Panel MakeCard(string title, Label value)
    {
        var p = new Panel { Width = 250, Height = 120, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(8) };
        p.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 24, ForeColor = SystemColors.GrayText });
        value.Text = "0"; value.Dock = DockStyle.Fill; value.Font = new Font("Segoe UI", 22, FontStyle.Bold); value.ForeColor = SystemColors.Highlight;
        p.Controls.Add(value);
        return p;
    }

    private Button MakeLaunch(string module)
    {
        var b = new Button { Dock = DockStyle.Fill, Text = module, Font = new Font("Segoe UI", 12, FontStyle.Bold), Margin = new Padding(8) };
        b.Click += (_, _) => _navigate(module);
        return b;
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill; grid.ReadOnly = true; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    public void LoadData()
    {
        _out.Text = FormatHelper.FormatMUR(_db.SumOutstandingCredit());
        _exp.Text = FormatHelper.FormatMUR(_db.SumExpenditure());
        _del.Text = _db.CountDeliveredKeys().ToString();
        _stock.Text = _db.CountKeysInStock().ToString();
        _grid.DataSource = _db.GetRecentKeyOrders().Select(k => new { k.Customer, k.Game, k.Platform, Price = FormatHelper.FormatMUR(k.Price), Date = FormatHelper.FormatDate(k.OrderDate), k.Status }).ToList();
    }
}
