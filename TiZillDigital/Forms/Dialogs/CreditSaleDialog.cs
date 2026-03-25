using System.Windows.Forms;
using TiZillDigital.Helpers;
using TiZillDigital.Models;

namespace TiZillDigital.Forms.Dialogs;

public class CreditSaleDialog : Form
{
    public CreditSale Value { get; private set; }
    private readonly TextBox _customer = new(); private readonly TextBox _phone = new(); private readonly TextBox _item = new(); private readonly NumericUpDown _amount = new() { DecimalPlaces = 2, Maximum = 1_000_000 }; private readonly NumericUpDown _paid = new() { DecimalPlaces = 2, Maximum = 1_000_000 }; private readonly DateTimePicker _date = new(); private readonly TextBox _notes = new() { Multiline = true, Height = 60 }; private readonly Label _err = new() { ForeColor = System.Drawing.Color.Red };
    public CreditSaleDialog(CreditSale? sale = null) { Value = sale ?? new CreditSale { SaleDate = FormatHelper.Today() }; InitializeComponent(); if (sale != null) Bind(sale); }
    private void InitializeComponent()
    {
        Text = "Credit Sale"; Width = 480; Height = 430; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 9 }; table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Add(table, "Customer*", _customer, 0); Add(table, "Phone", _phone, 1); Add(table, "Item", _item, 2); Add(table, "Amount*", _amount, 3); Add(table, "Paid", _paid, 4); Add(table, "Date", _date, 5); Add(table, "Notes", _notes, 6);
        table.Controls.Add(_err, 1, 7); var btns = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft }; var ok = new Button { Text = "OK", DialogResult = DialogResult.OK }; var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel }; ok.Click += (_, e) => { if (!ValidateData()) { DialogResult = DialogResult.None; return; } Save(); }; btns.Controls.Add(ok); btns.Controls.Add(cancel); table.Controls.Add(btns, 1, 8);
        Controls.Add(table); AcceptButton = ok; CancelButton = cancel;
    }
    private static void Add(TableLayoutPanel t, string label, Control c, int row) { t.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row); c.Dock = DockStyle.Fill; t.Controls.Add(c, 1, row); }
    private void Bind(CreditSale s) { _customer.Text = s.Customer; _phone.Text = s.Phone; _item.Text = s.Item; _amount.Value = s.Amount; _paid.Value = s.Paid; _date.Value = DateTime.Parse(s.SaleDate); _notes.Text = s.Notes; }
    private bool ValidateData() { if (string.IsNullOrWhiteSpace(_customer.Text)) { _err.Text = "Customer is required."; return false; } if (_amount.Value <= 0) { _err.Text = "Amount must be greater than zero."; return false; } _err.Text = ""; return true; }
    private void Save() => Value = new CreditSale { Id = Value.Id, Customer = _customer.Text.Trim(), Phone = _phone.Text.Trim(), Item = _item.Text.Trim(), Amount = _amount.Value, Paid = _paid.Value, SaleDate = _date.Value.ToString("yyyy-MM-dd"), Notes = _notes.Text.Trim() };
}
