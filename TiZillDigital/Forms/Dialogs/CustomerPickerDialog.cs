using System.Windows.Forms;
using TiZillDigital.Database;
using TiZillDigital.Models;
using TiZillDigital.UI;

namespace TiZillDigital.Forms.Dialogs;

/// <summary>
/// Modal dialog for searching and selecting an existing customer.
/// Used by CreditSaleDialog, KeyOrderDialog, etc.
/// </summary>
public class CustomerPickerDialog : Form
{
    public Customer? SelectedCustomer { get; private set; }

    private readonly DatabaseManager _db;
    private readonly TextBox _search = new();
    private readonly DataGridView _grid = new();
    private List<Customer> _allCustomers = new();
    private List<Customer> _filtered = new();

    public CustomerPickerDialog(DatabaseManager db)
    {
        _db = db;
        InitializeComponent();
        ApplyTheme();
        LoadCustomers();
    }

    private void InitializeComponent()
    {
        Text = "Select Customer";
        Width = 520;
        Height = 400;
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
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        // Row 0: Search box with label
        var searchLabel = new Label { Text = "Search ID, name or phone:", AutoSize = true };
        _search.PlaceholderText = "Type to filter...";
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.BackColor = Color.White;
        _search.Font = new Font("Segoe UI", 9f);
        _search.Dock = DockStyle.Fill;
        _search.TextChanged += SearchTextChanged;

        var searchPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        searchPanel.Controls.Add(searchLabel);
        searchPanel.Controls.Add(_search, 0, 0);
        root.Controls.Add(searchPanel, 0, 0);

        // Row 1: Grid
        ConfigureGrid();
        _grid.DoubleClick += (_, _) => SelectCustomer();
        _grid.KeyDown += GridKeyDown;
        root.Controls.Add(_grid, 0, 1);

        // Row 2: Separator
        var separator = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = NavTheme.BorderColor };
        root.Controls.Add(separator, 0, 2);

        // Row 3: Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var newBtn = new Button { Text = "New Customer", Width = 110, Height = 28 };
        var selectBtn = new Button { Text = "Select", Width = 80, Height = 28, DialogResult = DialogResult.OK };
        var cancelBtn = new Button { Text = "Cancel", Width = 80, Height = 28, DialogResult = DialogResult.Cancel };

        NavTheme.StyleButtonSecondary(newBtn);
        NavTheme.StyleButtonPrimary(selectBtn);
        NavTheme.StyleButtonSecondary(cancelBtn);

        newBtn.Click += (_, _) => NewCustomer();
        selectBtn.Click += (_, _) => SelectCustomer();

        buttonPanel.Controls.Add(cancelBtn);
        buttonPanel.Controls.Add(selectBtn);
        buttonPanel.Controls.Add(newBtn);
        root.Controls.Add(buttonPanel, 0, 3);

        Controls.Add(root);
        AcceptButton = selectBtn;
        CancelButton = cancelBtn;

        Shown += (_, _) => _search.Focus();
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        NavTheme.ApplyToDataGridView(_grid);
    }

    private void LoadCustomers()
    {
        _allCustomers = _db.GetCustomers();
        BindGrid();
    }

    private void SearchTextChanged(object? sender, EventArgs e)
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
    }

    private void BindGrid()
    {
        _grid.DataSource = _filtered
            .Select(c => new { c.Id, c.CustomerCode, c.Name, c.Phone })
            .ToList();
    }

    private void SelectCustomer()
    {
        if (_grid.CurrentRow == null)
        {
            MessageBox.Show("Please select a customer.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var customerId = _grid.CurrentRow.Cells["Id"].Value?.ToString();
        if (string.IsNullOrWhiteSpace(customerId))
            return;

        SelectedCustomer = _db.GetCustomerById(customerId);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void NewCustomer()
    {
        using var dlg = new CustomerDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            var code = _db.AddCustomer(dlg.Value);
            LoadCustomers();
            // Select the newly created customer
            var newCustomer = _db.GetCustomerByCode(code);
            if (newCustomer != null)
            {
                SelectedCustomer = newCustomer;
                // Scroll to and highlight the new customer
                var idx = _filtered.FindIndex(c => c.Id == newCustomer.Id);
                if (idx >= 0)
                {
                    _grid.CurrentCell = _grid.Rows[idx].Cells[0];
                }
            }
        }
    }

    private void GridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Return)
        {
            SelectCustomer();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
