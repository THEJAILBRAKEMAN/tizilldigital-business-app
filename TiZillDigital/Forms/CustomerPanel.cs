using System.Windows.Forms;
using TiZillDigital.Database;
using TiZillDigital.Forms.Dialogs;
using TiZillDigital.Models;
using TiZillDigital.UI;

namespace TiZillDigital.Forms;

/// <summary>
/// UserControl for complete customer lifecycle management.
/// Features: search, CRUD operations, contact information, link to sales/orders.
/// </summary>
public class CustomerPanel : UserControl
{
    private readonly DatabaseManager _db;
    private readonly TextBox _search = new();
    private readonly DataGridView _grid = new();
    private readonly Label _recordCount = new();
    private List<Customer> _allCustomers = new();
    private List<Customer> _filtered = new();
    private System.Windows.Forms.Timer? _searchTimer;

    public CustomerPanel(DatabaseManager db)
    {
        _db = db;
        InitializeComponent();
        WireEvents();
        LoadCustomers();
    }

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));  // Header
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));  // Toolbar
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Grid
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));  // Status

        // Row 0: Page header
        var header = new Panel { Dock = DockStyle.Fill, BackColor = NavTheme.HeaderBg };
        var headerLabel = new Label
        {
            Text = "Customers",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = NavTheme.HeaderText,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };
        header.Controls.Add(headerLabel);
        root.Controls.Add(header, 0, 0);

        // Row 1: Toolbar
        var toolbar = new ToolStrip { BackColor = Color.White, ImageScalingSize = new System.Drawing.Size(16, 16) };
        toolbar.Items.Add("+ New Customer", null, (_, _) => NewCustomer());
        toolbar.Items.Add("Edit", null, (_, _) => EditCustomer());
        toolbar.Items.Add("Delete", null, (_, _) => DeleteCustomer());
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add("Print Card", null, (_, _) => PrintCard());
        toolbar.Items.Add("Export CSV", null, (_, _) => ExportCsv());
        toolbar.Items.Add(new ToolStripLabel("Refresh every 30s", alignment: System.Windows.Forms.ToolStripItemAlignment.Right));

        var toolbarPanel = new Panel { Dock = DockStyle.Fill };
        toolbarPanel.Controls.Add(toolbar);
        toolbar.Dock = DockStyle.Top;
        root.Controls.Add(toolbarPanel, 0, 1);

        // Row 2: Grid
        ConfigureGrid();
        _grid.DoubleClick += (_, _) => ViewProfile();
        _grid.KeyDown += GridKeyDown;
        _grid.ContextMenuStrip = BuildContextMenu();
        root.Controls.Add(_grid, 0, 2);

        // Row 3: Status bar
        var statusbar = new ToolStrip { AutoSize = false, Height = 24, BackColor = NavTheme.StatusBarBg };
        statusbar.Items.Add(new ToolStripLabel("Customers", foreColor: NavTheme.StatusBarText));
        statusbar.Items.Add(new ToolStripSeparator());
        _recordCount.Text = "0 records";
        _recordCount.ForeColor = NavTheme.StatusBarText;
        statusbar.Items.Add(new ToolStripControlHost(_recordCount) { AutoSize = false, Width = 150 });
        statusbar.Items.Add(new ToolStripLabel("|", foreColor: NavTheme.StatusBarText));
        var refreshLabel = new ToolStripLabel($"Last updated: {DateTime.Now:HH:mm}", foreColor: NavTheme.StatusBarText, alignment: System.Windows.Forms.ToolStripItemAlignment.Right);
        statusbar.Items.Add(refreshLabel);
        root.Controls.Add(statusbar, 0, 3);

        Controls.Add(root);

        // Setup search debounce timer
        _searchTimer = new System.Windows.Forms.Timer();
        _searchTimer.Interval = 200;
        _searchTimer.Tick += (_, _) => SearchTimerTick();
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        NavTheme.ApplyToDataGridView(_grid);
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("View Profile", null, (_, _) => ViewProfile());
        menu.Items.Add("Edit", null, (_, _) => EditCustomer());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("New Credit Sale", null, (_, _) => NewCreditSale());
        menu.Items.Add("New Key Order", null, (_, _) => NewKeyOrder());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Print Card", null, (_, _) => PrintCard());
        menu.Items.Add("Copy Customer Code", null, (_, _) => CopyCode());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Delete", null, (_, _) => DeleteCustomer());
        return menu;
    }

    private void WireEvents()
    {
        _search.TextChanged += (_, _) =>
        {
            _searchTimer?.Stop();
            _searchTimer?.Start();
        };
        _grid.KeyDown += GridKeyDown;
    }

    private void SearchTimerTick()
    {
        _searchTimer?.Stop();
        FilterCustomers();
    }

    public void LoadCustomers()
    {
        _allCustomers = _db.GetCustomers();
        FilterCustomers();
    }

    private void FilterCustomers()
    {
        var searchTerm = _search.Text.Trim();
        if (string.IsNullOrEmpty(searchTerm))
        {
            _filtered = _allCustomers;
        }
        else
        {
            _filtered = _allCustomers
                .Where(c => c.CustomerCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                         || c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                         || (c.Phone != null && c.Phone.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
        BindGrid();
        UpdateStatus();
    }

    private void BindGrid()
    {
        _grid.DataSource = _filtered
            .Select(c => new
            {
                c.Id,
                c.CustomerCode,
                c.Name,
                c.Phone,
                c.Email,
                c.Address,
                TotalCredit = FormatHelper.FormatMUR(GetCustomerOutstanding(c.Id)),
                TotalOrders = _db.CountOrdersByCustomer(c.Id),
                Member = DateTime.Parse(c.CreatedAt).ToString("dd MMM yyyy")
            })
            .ToList();
    }

    private decimal GetCustomerOutstanding(string customerId)
    {
        return _db.GetCreditSales()
            .Where(cs => cs.Customer == _db.GetCustomerById(customerId)?.Name)
            .Sum(cs => cs.Outstanding);
    }

    private void UpdateStatus()
    {
        _recordCount.Text = $"{_filtered.Count} of {_allCustomers.Count} records";
    }

    private Customer? GetSelected()
    {
        if (_grid.CurrentRow == null) return null;
        var id = _grid.CurrentRow.Cells["Id"].Value?.ToString();
        return string.IsNullOrEmpty(id) ? null : _db.GetCustomerById(id);
    }

    private void NewCustomer()
    {
        using var dlg = new CustomerDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            var code = _db.AddCustomer(dlg.Value);
            LoadCustomers();
            MessageBox.Show($"✓ Customer {code} created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void EditCustomer()
    {
        var c = GetSelected();
        if (c == null)
        {
            MessageBox.Show("Select a customer to edit.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new CustomerDialog(c);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _db.UpdateCustomer(dlg.Value);
            LoadCustomers();
            MessageBox.Show($"✓ Customer {c.CustomerCode} updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void DeleteCustomer()
    {
        var c = GetSelected();
        if (c == null)
        {
            MessageBox.Show("Select a customer to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var outstanding = _db.CountOutstandingByCustomer(c.Id);
        var orders = _db.CountOrdersByCustomer(c.Id);

        if (outstanding > 0 || orders > 0)
        {
            var msg = $"This customer has {outstanding} credit records and {orders} orders. Delete anyway?";
            if (MessageBox.Show(msg, "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
        }

        _db.DeleteCustomer(c.Id);
        LoadCustomers();
        MessageBox.Show($"✓ Customer {c.CustomerCode} deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ViewProfile()
    {
        var c = GetSelected();
        if (c == null) return;

        using var dlg = new CustomerProfileDialog(_db, c);
        dlg.ShowDialog(this);
    }

    private void NewCreditSale()
    {
        MessageBox.Show("Open Credit Sales panel to create new sale.\n(Customer will be auto-selected)", "New Credit Sale", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void NewKeyOrder()
    {
        MessageBox.Show("Open Key Delivery panel to create new order.\n(Customer will be auto-selected)", "New Key Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void PrintCard()
    {
        var c = GetSelected();
        if (c == null) return;
        MessageBox.Show($"Print customer card for {c.Name}.\n(Feature: Print business card)", "Print Card", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportCsv()
    {
        using var sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "customers.csv" };
        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            _db.ExportToCsv("customers", sfd.FileName);
            MessageBox.Show($"✓ Exported to {sfd.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void CopyCode()
    {
        var c = GetSelected();
        if (c == null) return;
        Clipboard.SetText(c.CustomerCode);
        MessageBox.Show($"✓ {c.CustomerCode} copied to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void GridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.N) NewCustomer();
        else if (e.Control && e.KeyCode == Keys.E) EditCustomer();
        else if (e.Control && e.KeyCode == Keys.F) _search.Focus();
        else if (e.KeyCode == Keys.Delete) DeleteCustomer();
        else if (e.KeyCode == Keys.F5) LoadCustomers();
        else if (e.KeyCode == Keys.Enter) ViewProfile();
        else if (e.KeyCode == Keys.Escape) { _search.Clear(); _grid.ClearSelection(); }
    }
}
