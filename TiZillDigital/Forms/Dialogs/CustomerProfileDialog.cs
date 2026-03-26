using System.Windows.Forms;
using TiZillDigital.Database;
using TiZillDigital.Helpers;
using TiZillDigital.Models;
using TiZillDigital.UI;

namespace TiZillDigital.Forms.Dialogs;

/// <summary>
/// Read-only modal dialog showing customer profile and history.
/// Opened by double-clicking a customer in CustomerPanel.
/// </summary>
public class CustomerProfileDialog : Form
{
    private readonly DatabaseManager _db;
    private readonly Customer _customer;

    public CustomerProfileDialog(DatabaseManager db, Customer customer)
    {
        _db = db;
        _customer = customer;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = $"Customer Profile — {_customer.CustomerCode}";
        Width = 680;
        Height = 520;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        NavTheme.ApplyToDialogForm(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        // Row 0: Title bar with customer code
        var titleLabel = new Label
        {
            Text = $"{_customer.CustomerCode} — {_customer.Name}",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = NavTheme.TextPrimary,
            Dock = DockStyle.Fill,
            AutoSize = false
        };
        var separator1 = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = NavTheme.BorderColor };
        var titlePanel = new Panel { Dock = DockStyle.Fill };
        titlePanel.Controls.Add(separator1);
        titlePanel.Controls.Add(titleLabel);
        root.Controls.Add(titlePanel, 0, 0);

        // Row 1: TabControl with 2 tabs
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildDetailsTab());
        tabs.TabPages.Add(BuildHistoryTab());
        root.Controls.Add(tabs, 0, 1);

        // Row 2: Buttons
        var separator2 = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = NavTheme.BorderColor };
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var closeBtn = new Button { Text = "Close", Width = 80, Height = 28, DialogResult = DialogResult.OK };
        var editBtn = new Button { Text = "Edit Customer", Width = 110, Height = 28 };
        var newSaleBtn = new Button { Text = "New Credit Sale", Width = 120, Height = 28 };
        var newOrderBtn = new Button { Text = "New Key Order", Width = 120, Height = 28 };

        NavTheme.StyleButtonSecondary(closeBtn);
        NavTheme.StyleButtonSecondary(editBtn);
        NavTheme.StyleButtonSecondary(newSaleBtn);
        NavTheme.StyleButtonSecondary(newOrderBtn);

        editBtn.Click += (_, _) => EditCustomer();
        newSaleBtn.Click += (_, _) => NewCreditSale();
        newOrderBtn.Click += (_, _) => NewKeyOrder();

        buttonPanel.Controls.Add(closeBtn);
        buttonPanel.Controls.Add(editBtn);
        buttonPanel.Controls.Add(newOrderBtn);
        buttonPanel.Controls.Add(newSaleBtn);

        var bottomPanel = new Panel { Dock = DockStyle.Fill };
        bottomPanel.Controls.Add(separator2);
        bottomPanel.Controls.Add(buttonPanel);
        root.Controls.Add(bottomPanel, 0, 2);

        Controls.Add(root);
        AcceptButton = closeBtn;
        CancelButton = closeBtn;
    }

    private TabPage BuildDetailsTab()
    {
        var tab = new TabPage("Details");
        tab.BackColor = NavTheme.PageBg;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;
        AddDetailRow(panel, "Customer Code:", _customer.CustomerCode, row++);
        AddDetailRow(panel, "Name:", _customer.Name, row++);
        AddDetailRow(panel, "Phone:", _customer.Phone ?? "(none)", row++);
        AddDetailRow(panel, "Email:", _customer.Email ?? "(none)", row++);
        AddDetailRow(panel, "Address:", _customer.Address ?? "(none)", row++);
        AddDetailRow(panel, "Member Since:", FormatHelper.FormatDate(_customer.CreatedAt), row++);

        // Stats section
        var statsLabel = new Label
        {
            Text = "Statistics",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Margin = new Padding(0, 12, 0, 6)
        };
        panel.Controls.Add(statsLabel, 0, row++);
        panel.SetColumnSpan(statsLabel, 2);

        // Load computed stats from database
        int outstanding = _db.CountOutstandingByCustomer(_customer.Id);
        int orders = _db.CountOrdersByCustomer(_customer.Id);

        AddDetailRow(panel, "Total Outstanding:", $"MUR {GetTotalOutstanding():N2}", row++);
        AddDetailRow(panel, "Credit Records:", outstanding.ToString(), row++);
        AddDetailRow(panel, "Total Orders:", orders.ToString(), row++);

        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage BuildHistoryTab()
    {
        var tab = new TabPage("History");
        tab.BackColor = NavTheme.PageBg;

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };

        NavTheme.ApplyToDataGridView(grid);

        // Load customer's credit sales and key orders
        var creditSales = _db.GetCreditSales()
            .Where(cs => cs.Customer == _customer.Name)
            .Select(cs => new
            {
                Date = FormatHelper.FormatDate(cs.SaleDate),
                Type = "Credit Sale",
                Item = cs.Item,
                Amount = FormatHelper.FormatMUR(cs.Amount),
                Status = cs.Outstanding > 0 ? "Owing" : "Settled"
            });

        var keyOrders = _db.GetKeyOrders()
            .Where(ko => ko.Customer == _customer.Name)
            .Select(ko => new
            {
                Date = FormatHelper.FormatDate(ko.OrderDate),
                Type = "Key Order",
                Item = ko.Game,
                Amount = FormatHelper.FormatMUR(ko.Price),
                Status = ko.Status
            });

        var combined = creditSales.Cast<dynamic>()
            .Concat(keyOrders.Cast<dynamic>())
            .OrderByDescending(x => x.Date)
            .ToList();

        grid.DataSource = combined;

        tab.Controls.Add(grid);
        return tab;
    }

    private void AddDetailRow(TableLayoutPanel panel, string label, string value, int row)
    {
        var lbl = new Label
        {
            Text = label,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = NavTheme.TextSecondary,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        var val = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 9),
            ForeColor = NavTheme.TextPrimary,
            Dock = DockStyle.Fill
        };

        panel.Controls.Add(lbl, 0, row);
        panel.Controls.Add(val, 1, row);
    }

    private decimal GetTotalOutstanding()
    {
        return _db.GetCreditSales()
            .Where(cs => cs.Customer == _customer.Name)
            .Sum(cs => cs.Outstanding);
    }

    private void EditCustomer()
    {
        var customer = _db.GetCustomerById(_customer.Id);
        if (customer == null) return;

        using var dlg = new CustomerDialog(customer);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _db.UpdateCustomer(dlg.Value);
            MessageBox.Show($"✓ Customer {customer.CustomerCode} updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void NewCreditSale()
    {
        MessageBox.Show($"Create new credit sale for {_customer.Name}.\n\nClose this dialog and use Credit Sales panel.",
            "New Credit Sale", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void NewKeyOrder()
    {
        MessageBox.Show($"Create new key order for {_customer.Name}.\n\nClose this dialog and use Key Delivery panel.",
            "New Key Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
