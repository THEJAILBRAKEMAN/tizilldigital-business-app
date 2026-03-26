using System.Windows.Forms;
using TiZillDigital.Database;
using TiZillDigital.Helpers;
using TiZillDigital.Models;
using TiZillDigital.UI;

namespace TiZillDigital.Forms.Dialogs;

/// <summary>
/// Modal dialog for creating or editing credit sales.
/// Features:
/// - Live outstanding calculation (updates as Amount/Paid change)
/// - Customer picker dialog integration
/// - "Save & New" button to continue adding records
/// - Inline field validation
/// </summary>
public class CreditSaleDialog : Form
{
    public CreditSale Value { get; private set; }

    private readonly DatabaseManager _db;
    private readonly TextBox _customerDisplay = new() { ReadOnly = true, BackColor = SystemColors.Control };
    private readonly Button _customerBrowse = new() { Text = "Browse...", Width = 90, Height = 24 };
    private readonly TextBox _phone = new();
    private readonly TextBox _item = new();
    private readonly NumericUpDown _amount = new() { DecimalPlaces = 2, Maximum = 1_000_000, Value = 0 };
    private readonly NumericUpDown _paid = new() { DecimalPlaces = 2, Maximum = 1_000_000, Value = 0 };
    private readonly Label _outstandingValue = new() { Font = new Font("Segoe UI", 14, FontStyle.Bold), AutoSize = false, Height = 32, TextAlign = ContentAlignment.MiddleLeft };
    private readonly DateTimePicker _date = new();
    private readonly TextBox _notes = new() { Multiline = true, Height = 60 };
    private readonly TextBox _email = new();

    private readonly Label _errCustomer = new() { ForeColor = NavTheme.ErrorRed, Visible = false, AutoSize = true };
    private readonly Label _errAmount = new() { ForeColor = NavTheme.ErrorRed, Visible = false, AutoSize = true };
    private readonly Label _errPaid = new() { ForeColor = NavTheme.ErrorRed, Visible = false, AutoSize = true };

    private readonly Button _ok = new() { Text = "OK", Width = 80, Height = 28 };
    private readonly Button _saveNew = new() { Text = "Save & New", Width = 100, Height = 28 };
    private readonly Button _cancel = new() { Text = "Cancel", Width = 80, Height = 28, DialogResult = DialogResult.Cancel };

    private Customer? _selectedCustomer;

    public CreditSaleDialog(DatabaseManager db, CreditSale? sale = null)
    {
        _db = db;
        Value = sale ?? new CreditSale { SaleDate = FormatHelper.Today() };
        
        if (sale != null)
        {
            Text = $"Edit Credit Sale — {sale.Customer}";
        }
        else
        {
            Text = "New Credit Sale";
        }

        InitializeComponent();
        ApplyTheme();
        WireEvents();

        if (sale != null)
        {
            BindSale(sale);
        }
    }

    private void InitializeComponent()
    {
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Width = 540;
        Height = 600;

        NavTheme.ApplyToDialogForm(this);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 3,
            RowCount = 11
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        // Row 0: Customer (with Browse button)
        var customerLabel = new Label { Text = "Customer*", AutoSize = true, Anchor = AnchorStyles.Left };
        var customerPanel = new Panel { Dock = DockStyle.Fill };
        _customerDisplay.Dock = DockStyle.Fill;
        _customerBrowse.Click += CustomerBrowseClick;
        customerPanel.Controls.Add(_customerDisplay);
        customerPanel.Controls.Add(_customerBrowse);
        _customerBrowse.Dock = DockStyle.Right;
        _customerBrowse.Margin = new Padding(8, 0, 0, 0);

        table.Controls.Add(customerLabel, 0, 0);
        table.Controls.Add(customerPanel, 1, 0);
        table.SetColumnSpan(customerPanel, 2);

        // Row 1: Customer error label
        table.Controls.Add(_errCustomer, 1, 1);
        table.SetColumnSpan(_errCustomer, 2);

        // Row 2: Phone
        AddRow(table, "Phone", _phone, 2);

        // Row 3: Email
        AddRow(table, "Email", _email, 3);

        // Row 4: Item
        AddRow(table, "Item", _item, 4);

        // Row 5: Amount
        var amountLabel = new Label { Text = "Amount*", AutoSize = true, Anchor = AnchorStyles.Left };
        _amount.BorderStyle = BorderStyle.FixedSingle;
        _amount.Font = new Font("Segoe UI", 9f);
        table.Controls.Add(amountLabel, 0, 5);
        table.Controls.Add(_amount, 1, 5);

        // Row 6: Amount error label
        table.Controls.Add(_errAmount, 1, 6);
        table.SetColumnSpan(_errAmount, 2);

        // Row 7: Paid
        var paidLabel = new Label { Text = "Paid", AutoSize = true, Anchor = AnchorStyles.Left };
        _paid.BorderStyle = BorderStyle.FixedSingle;
        _paid.Font = new Font("Segoe UI", 9f);
        table.Controls.Add(paidLabel, 0, 7);
        table.Controls.Add(_paid, 1, 7);

        // Row 8: Paid error label
        table.Controls.Add(_errPaid, 1, 8);
        table.SetColumnSpan(_errPaid, 2);

        // Row 9: Outstanding (LIVE CALCULATION)
        var outstandingLabel = new Label { Text = "Outstanding", AutoSize = true, Anchor = AnchorStyles.Left };
        _outstandingValue.Text = "MUR 0.00";
        _outstandingValue.BackColor = NavTheme.PanelBg;
        _outstandingValue.BorderStyle = BorderStyle.FixedSingle;
        table.Controls.Add(outstandingLabel, 0, 9);
        table.Controls.Add(_outstandingValue, 1, 9);
        table.SetColumnSpan(_outstandingValue, 2);

        // Row 10: Separator
        var separator = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = NavTheme.BorderColor };
        table.Controls.Add(separator, 0, 10);
        table.SetColumnSpan(separator, 3);

        // Row 11: Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        NavTheme.StyleButtonPrimary(_ok);
        NavTheme.StyleButtonSecondary(_saveNew);
        NavTheme.StyleButtonSecondary(_cancel);

        _ok.DialogResult = DialogResult.OK;
        _ok.Click += OkClick;
        _saveNew.Click += SaveNewClick;

        buttonPanel.Controls.Add(_cancel);
        buttonPanel.Controls.Add(_saveNew);
        buttonPanel.Controls.Add(_ok);

        table.Controls.Add(buttonPanel, 0, 11);
        table.SetColumnSpan(buttonPanel, 3);

        mainLayout.Controls.Add(table, 0, 0);

        // Add date and notes in a secondary panel
        var bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 0, 12, 12),
            ColumnCount = 2,
            RowCount = 2
        };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var dateLabel = new Label { Text = "Date", AutoSize = true, Anchor = AnchorStyles.Left };
        _date.Font = new Font("Segoe UI", 9f);
        bottomPanel.Controls.Add(dateLabel, 0, 0);
        bottomPanel.Controls.Add(_date, 1, 0);

        var notesLabel = new Label { Text = "Notes", AutoSize = true, Anchor = AnchorStyles.Left };
        _notes.BorderStyle = BorderStyle.FixedSingle;
        _notes.BackColor = Color.White;
        _notes.Font = new Font("Segoe UI", 9f);
        _notes.ScrollBars = ScrollBars.Vertical;
        _notes.Dock = DockStyle.Fill;
        bottomPanel.Controls.Add(notesLabel, 0, 1);
        bottomPanel.Controls.Add(_notes, 1, 1);

        mainLayout.Controls.Add(bottomPanel, 0, 1);

        Controls.Add(mainLayout);
        AcceptButton = _ok;
        CancelButton = _cancel;
    }

    private void ApplyTheme()
    {
        _customerDisplay.BorderStyle = BorderStyle.FixedSingle;
        _customerDisplay.Font = new Font("Segoe UI", 9f);

        _phone.BorderStyle = BorderStyle.FixedSingle;
        _phone.BackColor = Color.White;
        _phone.Font = new Font("Segoe UI", 9f);

        _email.BorderStyle = BorderStyle.FixedSingle;
        _email.BackColor = Color.White;
        _email.Font = new Font("Segoe UI", 9f);

        _item.BorderStyle = BorderStyle.FixedSingle;
        _item.BackColor = Color.White;
        _item.Font = new Font("Segoe UI", 9f);

        NavTheme.StyleButtonSecondary(_customerBrowse);
    }

    private void AddRow(TableLayoutPanel table, string label, Control control, int row)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left };
        control.Dock = DockStyle.Fill;
        control.BorderStyle = BorderStyle.FixedSingle;
        control.BackColor = Color.White;
        control.Font = new Font("Segoe UI", 9f);

        table.Controls.Add(lbl, 0, row);
        table.Controls.Add(control, 1, row);
        table.SetColumnSpan(control, 2);
    }

    private void WireEvents()
    {
        _amount.ValueChanged += UpdateOutstanding;
        _paid.ValueChanged += UpdateOutstanding;
    }

    private void UpdateOutstanding(object? sender, EventArgs e)
    {
        var outstanding = _amount.Value - _paid.Value;
        _outstandingValue.Text = FormatHelper.FormatMUR(outstanding);

        if (outstanding > 0)
        {
            _outstandingValue.ForeColor = NavTheme.ErrorRed;
        }
        else if (outstanding == 0)
        {
            _outstandingValue.ForeColor = NavTheme.SuccessGreen;
        }
        else
        {
            _outstandingValue.ForeColor = NavTheme.WarningAmber;
        }
    }

    private void CustomerBrowseClick(object? sender, EventArgs e)
    {
        using var dlg = new CustomerPickerDialog(_db);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedCustomer != null)
        {
            _selectedCustomer = dlg.SelectedCustomer;
            _customerDisplay.Text = _selectedCustomer.CustomerCode;
            _phone.Text = _selectedCustomer.Phone ?? string.Empty;
            _email.Text = _selectedCustomer.Email ?? string.Empty;
        }
    }

    private void BindSale(CreditSale sale)
    {
        _customerDisplay.Text = sale.Customer;
        _phone.Text = sale.Phone;
        _email.Text = sale.Email;
        _item.Text = sale.Item;
        _amount.Value = sale.Amount;
        _paid.Value = sale.Paid;
        _date.Value = DateTime.Parse(sale.SaleDate);
        _notes.Text = sale.Notes;

        UpdateOutstanding(null, EventArgs.Empty);
    }

    private void OkClick(object? sender, EventArgs e)
    {
        if (!Validate())
        {
            DialogResult = DialogResult.None;
            return;
        }
        Save();
    }

    private void SaveNewClick(object? sender, EventArgs e)
    {
        if (!Validate())
        {
            DialogResult = DialogResult.None;
            return;
        }
        Save();
        // Keep dialog open, clear fields
        _selectedCustomer = null;
        _customerDisplay.Clear();
        _phone.Clear();
        _email.Clear();
        _item.Clear();
        _amount.Value = 0;
        _paid.Value = 0;
        _date.Value = DateTime.Now;
        _notes.Clear();
        _errCustomer.Visible = false;
        _errAmount.Visible = false;
        _errPaid.Visible = false;
        _customerDisplay.Focus();
    }

    private bool Validate()
    {
        _errCustomer.Visible = false;
        _errAmount.Visible = false;
        _errPaid.Visible = false;

        if (string.IsNullOrWhiteSpace(_customerDisplay.Text))
        {
            _errCustomer.Text = "Customer is required. Click Browse to select.";
            _errCustomer.Visible = true;
            return false;
        }

        if (_amount.Value <= 0)
        {
            _errAmount.Text = "Amount must be greater than zero.";
            _errAmount.Visible = true;
            return false;
        }

        if (_paid.Value > _amount.Value)
        {
            _errPaid.Text = "Paid cannot exceed total amount.";
            _errPaid.Visible = true;
            return false;
        }

        return true;
    }

    private void Save()
    {
        Value = new CreditSale
        {
            Id = Value.Id,
            Customer = _customerDisplay.Text.Trim(),
            Phone = _phone.Text.Trim(),
            Item = _item.Text.Trim(),
            Amount = _amount.Value,
            Paid = _paid.Value,
            SaleDate = _date.Value.ToString("yyyy-MM-dd"),
            Notes = _notes.Text.Trim(),
            Email = _email.Text.Trim()
        };
    }
}
