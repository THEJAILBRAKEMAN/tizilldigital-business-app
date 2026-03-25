using System.Windows.Forms;
using TiZillDigital.Helpers;
using TiZillDigital.Models;

namespace TiZillDigital.Forms.Dialogs;

public class ExpenditureDialog : Form
{
    public Expenditure Value { get; private set; }
    private readonly TextBox _desc = new(); private readonly NumericUpDown _amount = new() { DecimalPlaces = 2, Maximum = 1_000_000 }; private readonly ComboBox _cat = new(); private readonly DateTimePicker _date = new(); private readonly TextBox _notes = new(); private readonly Label _err = new() { ForeColor = System.Drawing.Color.Red };
    public ExpenditureDialog(Expenditure? e = null) { Value = e ?? new Expenditure { ExpenseDate = FormatHelper.Today(), Category = "Misc" }; InitializeComponent(); if (e != null) Bind(e); }
    private void InitializeComponent()
    {
        Text = "Expenditure"; Width = 460; Height = 360; StartPosition = FormStartPosition.CenterParent;
        _cat.Items.AddRange(["Inventory", "Transport", "Marketing", "Utilities", "Salary", "Tools & Software", "Misc"]); _cat.SelectedItem = "Misc";
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2 }; Add(t, "Description*", _desc, 0); Add(t, "Amount*", _amount, 1); Add(t, "Category", _cat, 2); Add(t, "Date", _date, 3); Add(t, "Notes", _notes, 4); t.Controls.Add(_err, 1, 5);
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK }; var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel }; ok.Click += (_, _) => { if (string.IsNullOrWhiteSpace(_desc.Text) || _amount.Value <= 0) { _err.Text = "Description and amount are required."; DialogResult = DialogResult.None; return; } Value = new Expenditure { Id = Value.Id, Description = _desc.Text.Trim(), Amount = _amount.Value, Category = _cat.Text, ExpenseDate = _date.Value.ToString("yyyy-MM-dd"), Notes = _notes.Text.Trim() }; };
        var fl = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill }; fl.Controls.Add(ok); fl.Controls.Add(cancel); t.Controls.Add(fl, 1, 6); Controls.Add(t); AcceptButton = ok; CancelButton = cancel;
    }
    private static void Add(TableLayoutPanel t, string label, Control c, int r) { t.Controls.Add(new Label { Text = label }, 0, r); c.Dock = DockStyle.Fill; t.Controls.Add(c, 1, r); }
    private void Bind(Expenditure e) { _desc.Text = e.Description; _amount.Value = e.Amount; _cat.Text = e.Category; _date.Value = DateTime.Parse(e.ExpenseDate); _notes.Text = e.Notes; }
}
