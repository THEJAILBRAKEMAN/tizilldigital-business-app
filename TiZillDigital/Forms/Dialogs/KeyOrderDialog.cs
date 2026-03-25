using System.Windows.Forms;
using TiZillDigital.Helpers;
using TiZillDigital.Models;

namespace TiZillDigital.Forms.Dialogs;

public class KeyOrderDialog : Form
{
    public KeyOrder Value { get; private set; }
    private readonly TextBox _customer = new(); private readonly TextBox _phone = new(); private readonly TextBox _email = new(); private readonly TextBox _game = new(); private readonly ComboBox _platform = new(); private readonly NumericUpDown _price = new() { DecimalPlaces = 2, Maximum = 1_000_000 }; private readonly TextBox _key = new(); private readonly ComboBox _status = new(); private readonly DateTimePicker _date = new(); private readonly TextBox _notes = new() { Multiline = true, Height = 60 }; private readonly Label _err = new() { ForeColor = System.Drawing.Color.Red };
    public KeyOrderDialog(KeyOrder? order = null) { Value = order ?? new KeyOrder { OrderDate = FormatHelper.Today() }; InitializeComponent(); if (order != null) Bind(order); }
    private void InitializeComponent()
    {
        Text = "Key Order"; Width = 540; Height = 520; StartPosition = FormStartPosition.CenterParent;
        _platform.Items.AddRange(["Steam", "Epic Games", "GOG", "PlayStation", "Xbox", "Nintendo", "Ubisoft", "EA App", "Battle.net", "Other"]); _platform.SelectedIndex = 0;
        _status.Items.AddRange(["Pending", "Delivered", "Failed", "Refunded"]); _status.SelectedIndex = 0;
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2 };
        Add(t, "Customer*", _customer, 0); Add(t, "Phone", _phone, 1); Add(t, "Email", _email, 2); Add(t, "Game*", _game, 3); Add(t, "Platform", _platform, 4); Add(t, "Price", _price, 5); Add(t, "Key", _key, 6); Add(t, "Status", _status, 7); Add(t, "Date", _date, 8); Add(t, "Notes", _notes, 9);
        t.Controls.Add(_err, 1, 10);
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK }; var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        ok.Click += (_, _) => { if (string.IsNullOrWhiteSpace(_customer.Text) || string.IsNullOrWhiteSpace(_game.Text)) { _err.Text = "Customer and game are required."; DialogResult = DialogResult.None; return; } Value = new KeyOrder { Id = Value.Id, Customer = _customer.Text.Trim(), Phone = _phone.Text.Trim(), Email = _email.Text.Trim(), Game = _game.Text.Trim(), Platform = _platform.Text, Price = _price.Value, ProductKey = _key.Text.Trim(), Status = _status.Text, OrderDate = _date.Value.ToString("yyyy-MM-dd"), Notes = _notes.Text.Trim() }; };
        var fl = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill }; fl.Controls.Add(ok); fl.Controls.Add(cancel); t.Controls.Add(fl, 1, 11); Controls.Add(t); AcceptButton = ok; CancelButton = cancel;
    }
    private static void Add(TableLayoutPanel t, string label, Control c, int r) { t.Controls.Add(new Label { Text = label }, 0, r); c.Dock = DockStyle.Fill; t.Controls.Add(c, 1, r); }
    private void Bind(KeyOrder k) { _customer.Text = k.Customer; _phone.Text = k.Phone; _email.Text = k.Email; _game.Text = k.Game; _platform.Text = k.Platform; _price.Value = k.Price; _key.Text = k.ProductKey; _status.Text = k.Status; _date.Value = DateTime.Parse(k.OrderDate); _notes.Text = k.Notes; }
}
