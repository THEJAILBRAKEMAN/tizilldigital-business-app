using System.Windows.Forms;
using TiZillDigital.Helpers;
using TiZillDigital.Models;

namespace TiZillDigital.Forms.Dialogs;

public class GameDialog : Form
{
    public Game Value { get; private set; }
    private readonly TextBox _title = new(); private readonly ComboBox _platform = new(); private readonly ComboBox _genre = new(); private readonly NumericUpDown _cost = new() { DecimalPlaces = 2, Maximum = 1_000_000 }; private readonly NumericUpDown _sell = new() { DecimalPlaces = 2, Maximum = 1_000_000 }; private readonly TextBox _notes = new(); private readonly DateTimePicker _date = new(); private readonly Label _err = new() { ForeColor = System.Drawing.Color.Red };
    public GameDialog(Game? game = null) { Value = game ?? new Game { AddedDate = FormatHelper.Today() }; InitializeComponent(); if (game != null) Bind(game); }
    private void InitializeComponent()
    {
        Text = "Game"; Width = 470; Height = 390; StartPosition = FormStartPosition.CenterParent;
        _platform.Items.AddRange(["Steam", "Epic Games", "GOG", "PlayStation", "Xbox", "Nintendo", "Other"]); _platform.SelectedIndex = 0;
        _genre.Items.AddRange(["Action", "Adventure", "RPG", "Strategy", "Sports", "Simulation", "Other"]); _genre.SelectedIndex = 0;
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2 }; Add(t, "Title*", _title, 0); Add(t, "Platform", _platform, 1); Add(t, "Genre", _genre, 2); Add(t, "Cost Price", _cost, 3); Add(t, "Sell Price", _sell, 4); Add(t, "Notes", _notes, 5); Add(t, "Date Added", _date, 6); t.Controls.Add(_err, 1, 7);
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK }; var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        ok.Click += (_, _) => { if (string.IsNullOrWhiteSpace(_title.Text)) { _err.Text = "Title is required."; DialogResult = DialogResult.None; return; } Value = new Game { Id = Value.Id, Title = _title.Text.Trim(), Platform = _platform.Text, Genre = _genre.Text, CostPrice = _cost.Value, SellPrice = _sell.Value, Notes = _notes.Text.Trim(), AddedDate = _date.Value.ToString("yyyy-MM-dd") }; };
        var fl = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill }; fl.Controls.Add(ok); fl.Controls.Add(cancel); t.Controls.Add(fl, 1, 8); Controls.Add(t); AcceptButton = ok; CancelButton = cancel;
    }
    private static void Add(TableLayoutPanel t, string label, Control c, int r) { t.Controls.Add(new Label { Text = label }, 0, r); c.Dock = DockStyle.Fill; t.Controls.Add(c, 1, r); }
    private void Bind(Game g) { _title.Text = g.Title; _platform.Text = g.Platform; _genre.Text = g.Genre; _cost.Value = g.CostPrice; _sell.Value = g.SellPrice; _notes.Text = g.Notes; _date.Value = DateTime.Parse(g.AddedDate); }
}
