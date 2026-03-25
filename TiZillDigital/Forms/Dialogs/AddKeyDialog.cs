using System.Windows.Forms;

namespace TiZillDigital.Forms.Dialogs;

public class AddKeyDialog : Form
{
    public List<string> Keys { get; private set; } = new();
    private readonly TextBox _keys = new() { Multiline = true, Dock = DockStyle.Fill, Font = new System.Drawing.Font("Consolas", 10f), ScrollBars = ScrollBars.Vertical };
    private readonly Label _err = new() { ForeColor = System.Drawing.Color.Red, Dock = DockStyle.Top };
    public AddKeyDialog()
    {
        Text = "Add Keys"; Width = 500; Height = 400; StartPosition = FormStartPosition.CenterParent;
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK }; var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        ok.Click += (_, _) => { Keys = _keys.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().ToList(); if (Keys.Count == 0) { _err.Text = "Enter at least one key."; DialogResult = DialogResult.None; } };
        var fl = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 40 }; fl.Controls.Add(ok); fl.Controls.Add(cancel);
        Controls.Add(_keys); Controls.Add(_err); Controls.Add(fl); AcceptButton = ok; CancelButton = cancel;
    }
}
