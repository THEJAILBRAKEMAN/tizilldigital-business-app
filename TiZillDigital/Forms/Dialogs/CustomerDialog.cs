using System.Windows.Forms;
using TiZillDigital.Database;
using TiZillDigital.Models;
using TiZillDigital.UI;

namespace TiZillDigital.Forms.Dialogs;

/// <summary>
/// Modal dialog for adding or editing customers.
/// </summary>
public class CustomerDialog : Form
{
    public Customer Value { get; private set; }
    
    private readonly TextBox _id = new() { ReadOnly = true, BackColor = SystemColors.Control };
    private readonly TextBox _name = new();
    private readonly TextBox _phone = new();
    private readonly TextBox _email = new();
    private readonly TextBox _address = new() { Multiline = true, Height = 50 };
    private readonly TextBox _notes = new() { Multiline = true, Height = 60 };
    private readonly Label _errName = new() { ForeColor = NavTheme.ErrorRed, Visible = false, AutoSize = true };
    
    private readonly Button _ok = new() { Text = "OK", Width = 80, Height = 28 };
    private readonly Button _saveNew = new() { Text = "Save & New", Width = 80, Height = 28 };
    private readonly Button _cancel = new() { Text = "Cancel", Width = 80, Height = 28, DialogResult = DialogResult.Cancel };

    public CustomerDialog(Customer? customer = null)
    {
        Value = customer ?? new Customer();
        InitializeComponent();
        ApplyTheme();
        if (customer != null)
        {
            BindCustomer(customer);
            Text = $"Edit Customer — {customer.CustomerCode}";
        }
        else
        {
            Text = "New Customer";
        }
    }

    private void InitializeComponent()
    {
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Width = 520;
        Height = 520;
        
        NavTheme.ApplyToDialogForm(this);

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 9
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Row 0: Customer ID (read-only)
        AddRow(table, "Customer ID", _id, 0, true);

        // Row 1: Name (required) with error label
        var nameLabel = new Label { Text = "Name*", AutoSize = true, Anchor = AnchorStyles.Left };
        table.Controls.Add(nameLabel, 0, 1);
        table.Controls.Add(_name, 1, 1);

        // Row 2: Error label for name
        table.Controls.Add(_errName, 1, 2);

        // Row 3: Phone
        AddRow(table, "Phone", _phone, 3, false);

        // Row 4: Email
        AddRow(table, "Email", _email, 4, false);

        // Row 5: Address (multiline)
        AddRow(table, "Address", _address, 5, false);

        // Row 6: Notes (multiline)
        AddRow(table, "Notes", _notes, 6, false);

        // Row 7: Separator (1px line)
        var separator = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = NavTheme.BorderColor };
        table.Controls.Add(separator, 0, 7);
        table.SetColumnSpan(separator, 2);

        // Row 8: Buttons
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
        table.Controls.Add(buttonPanel, 0, 8);
        table.SetColumnSpan(buttonPanel, 2);

        Controls.Add(table);
        AcceptButton = _ok;
        CancelButton = _cancel;
    }

    private void ApplyTheme()
    {
        _name.BorderStyle = BorderStyle.FixedSingle;
        _name.BackColor = Color.White;
        _name.Font = new Font("Segoe UI", 9f);
        
        _phone.BorderStyle = BorderStyle.FixedSingle;
        _phone.BackColor = Color.White;
        _phone.Font = new Font("Segoe UI", 9f);
        
        _email.BorderStyle = BorderStyle.FixedSingle;
        _email.BackColor = Color.White;
        _email.Font = new Font("Segoe UI", 9f);
        
        _address.BorderStyle = BorderStyle.FixedSingle;
        _address.BackColor = Color.White;
        _address.Font = new Font("Segoe UI", 9f);
        _address.ScrollBars = ScrollBars.Vertical;
        
        _notes.BorderStyle = BorderStyle.FixedSingle;
        _notes.BackColor = Color.White;
        _notes.Font = new Font("Segoe UI", 9f);
        _notes.ScrollBars = ScrollBars.Vertical;
        
        _id.BorderStyle = BorderStyle.FixedSingle;
        _id.Font = new Font("Segoe UI", 9f);
    }

    private void AddRow(TableLayoutPanel table, string label, Control control, int row, bool readOnly)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left };
        table.Controls.Add(lbl, 0, row);
        
        control.Dock = DockStyle.Fill;
        control.BorderStyle = BorderStyle.FixedSingle;
        control.BackColor = readOnly ? SystemColors.Control : Color.White;
        control.Font = new Font("Segoe UI", 9f);
        
        table.Controls.Add(control, 1, row);
    }

    private void BindCustomer(Customer customer)
    {
        _id.Text = customer.CustomerCode;
        _name.Text = customer.Name;
        _phone.Text = customer.Phone ?? string.Empty;
        _email.Text = customer.Email ?? string.Empty;
        _address.Text = customer.Address ?? string.Empty;
        _notes.Text = customer.Notes ?? string.Empty;
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
        // Keep dialog open, clear fields for next entry
        _name.Clear();
        _phone.Clear();
        _email.Clear();
        _address.Clear();
        _notes.Clear();
        _errName.Visible = false;
        _name.Focus();
    }

    private bool Validate()
    {
        _errName.Visible = false;
        
        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            _errName.Text = "Name is required.";
            _errName.Visible = true;
            _name.Focus();
            return false;
        }

        return true;
    }

    private void Save()
    {
        Value = new Customer
        {
            Id = Value.Id,
            CustomerCode = Value.CustomerCode,
            Name = _name.Text.Trim(),
            Phone = string.IsNullOrWhiteSpace(_phone.Text) ? null : _phone.Text.Trim(),
            Email = string.IsNullOrWhiteSpace(_email.Text) ? null : _email.Text.Trim(),
            Address = string.IsNullOrWhiteSpace(_address.Text) ? null : _address.Text.Trim(),
            Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim(),
            CreatedAt = Value.CreatedAt,
            UpdatedAt = Value.UpdatedAt
        };
    }
}
