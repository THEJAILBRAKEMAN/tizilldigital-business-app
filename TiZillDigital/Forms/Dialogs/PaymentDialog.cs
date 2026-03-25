using System.Windows.Forms;
using TiZillDigital.Helpers;
using TiZillDigital.Models;

namespace TiZillDigital.Forms.Dialogs;

public class PaymentDialog : Form
{
    public Payment Value { get; private set; } = new();
    private readonly NumericUpDown _amount = new() { DecimalPlaces = 2, Maximum = 1_000_000 }; private readonly DateTimePicker _date = new(); private readonly TextBox _notes = new(); private readonly Label _err = new() { ForeColor = System.Drawing.Color.Red };
    public PaymentDialog(CreditSale sale)
    {
        Text = "Record Payment"; Width = 420; Height = 280; StartPosition = FormStartPosition.CenterParent;
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2 };
        t.Controls.Add(new Label { Text = $"Customer: {sale.Customer}", AutoSize = true }, 0, 0); t.SetColumnSpan(t.Controls[^1], 2);
        t.Controls.Add(new Label { Text = $"Outstanding: {FormatHelper.FormatMUR(sale.Outstanding)}", AutoSize = true }, 0, 1); t.SetColumnSpan(t.Controls[^1], 2);
        t.Controls.Add(new Label { Text = "Amount*" }, 0, 2); t.Controls.Add(_amount, 1, 2); _amount.Value = sale.Outstanding > 0 ? sale.Outstanding : 0;
        t.Controls.Add(new Label { Text = "Date" }, 0, 3); t.Controls.Add(_date, 1, 3);
        t.Controls.Add(new Label { Text = "Notes" }, 0, 4); t.Controls.Add(_notes, 1, 4);
        t.Controls.Add(_err, 1, 5);
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK }; var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        ok.Click += (_, _) => { if (_amount.Value <= 0) { _err.Text = "Amount must be greater than zero."; DialogResult = DialogResult.None; return; } Value = new Payment { CreditId = sale.Id, Amount = _amount.Value, PaidDate = _date.Value.ToString("yyyy-MM-dd"), Notes = _notes.Text.Trim() }; };
        var fl = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill }; fl.Controls.Add(ok); fl.Controls.Add(cancel); t.Controls.Add(fl, 1, 6);
        Controls.Add(t); AcceptButton = ok; CancelButton = cancel;
    }
}
