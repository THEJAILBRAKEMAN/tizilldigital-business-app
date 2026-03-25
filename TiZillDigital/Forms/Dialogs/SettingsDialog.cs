using System.Drawing.Printing;
using System.IO.Ports;
using System.Diagnostics;
using System.Windows.Forms;
using TiZillDigital.Database;
using TiZillDigital.Printing;

namespace TiZillDigital.Forms.Dialogs;

public class SettingsDialog : Form
{
    private readonly DatabaseManager _db;
    private readonly Dictionary<string, string> _settings;
    private readonly TextBox _business = new(); private readonly TextBox _address = new(); private readonly TextBox _phone = new(); private readonly ComboBox _currency = new(); private readonly TextBox _footer = new() { Multiline = true, Height = 60 };
    private readonly RadioButton _usb = new() { Text = "USB" }; private readonly RadioButton _serial = new() { Text = "Serial/COM" }; private readonly RadioButton _network = new() { Text = "Network/TCP" }; private readonly RadioButton _windows = new() { Text = "Windows Printer" };
    private readonly TextBox _vid = new(); private readonly TextBox _pid = new(); private readonly ComboBox _com = new(); private readonly ComboBox _baud = new(); private readonly TextBox _ip = new(); private readonly NumericUpDown _port = new() { Maximum = 65535, Value = 9100 }; private readonly ComboBox _winPrinter = new();
    public SettingsDialog(DatabaseManager db)
    {
        _db = db; _settings = db.GetSettings(); InitializeComponent(); LoadExisting();
    }

    private void InitializeComponent()
    {
        Text = "Settings"; Width = 700; Height = 560; StartPosition = FormStartPosition.CenterParent;
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildGeneralTab());
        tabs.TabPages.Add(BuildPrinterTab());
        Controls.Add(tabs);
    }

    private TabPage BuildGeneralTab()
    {
        var p = new TabPage("General");
        _currency.Items.AddRange(["MUR", "USD", "EUR", "GBP"]);
        var t = new TableLayoutPanel { Dock = DockStyle.Top, Height = 280, Padding = new Padding(12), ColumnCount = 2 };
        Add(t, "Business Name", _business, 0); Add(t, "Address", _address, 1); Add(t, "Phone", _phone, 2); Add(t, "Currency", _currency, 3); Add(t, "Receipt Footer", _footer, 4);
        var save = new Button { Text = "Save Settings", Dock = DockStyle.Top, Height = 36 };
        save.Click += (_, _) => { Save("business_name", _business.Text); Save("address", _address.Text); Save("phone", _phone.Text); Save("currency", _currency.Text); Save("receipt_footer", _footer.Text); MessageBox.Show("Settings saved."); };
        p.Controls.Add(save); p.Controls.Add(t); return p;
    }

    private TabPage BuildPrinterTab()
    {
        var p = new TabPage("Thermal Printer");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2 };
        var typePanel = new FlowLayoutPanel { Dock = DockStyle.Fill }; typePanel.Controls.AddRange([_usb, _serial, _network, _windows]);
        root.Controls.Add(new Label { Text = "Printer Type" }, 0, 0); root.Controls.Add(typePanel, 1, 0);
        root.Controls.Add(new Label { Text = "USB VID" }, 0, 1); root.Controls.Add(_vid, 1, 1); root.Controls.Add(new Label { Text = "USB PID" }, 0, 2); root.Controls.Add(_pid, 1, 2);
        var detect = new Button { Text = "Auto-Detect USB Printers" }; detect.Click += (_, _) => DetectUsb(); root.Controls.Add(detect, 1, 3);
        _com.Items.AddRange(SerialPort.GetPortNames()); _baud.Items.AddRange(["9600", "19200", "38400", "115200"]); if (_baud.Items.Count > 0) _baud.SelectedIndex = 0;
        root.Controls.Add(new Label { Text = "COM Port" }, 0, 4); root.Controls.Add(_com, 1, 4); root.Controls.Add(new Label { Text = "Baud" }, 0, 5); root.Controls.Add(_baud, 1, 5);
        root.Controls.Add(new Label { Text = "IP" }, 0, 6); root.Controls.Add(_ip, 1, 6); root.Controls.Add(new Label { Text = "Port" }, 0, 7); root.Controls.Add(_port, 1, 7);
        foreach (string name in PrinterSettings.InstalledPrinters) _winPrinter.Items.Add(name);
        root.Controls.Add(new Label { Text = "Windows Printer" }, 0, 8); root.Controls.Add(_winPrinter, 1, 8);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var save = new Button { Text = "Save Printer Settings" }; var test = new Button { Text = "Test Print" };
        save.Click += (_, _) => SavePrinter(); test.Click += (_, _) => { var tp = new ThermalPrinter(_db); if (tp.TestConnection(out var error)) MessageBox.Show("Test print sent successfully."); else MessageBox.Show("Printer Error: " + error); };
        actions.Controls.Add(save); actions.Controls.Add(test); root.Controls.Add(actions, 1, 9);
        p.Controls.Add(root); return p;
    }

    private static void Add(TableLayoutPanel t, string label, Control c, int row) { t.Controls.Add(new Label { Text = label }, 0, row); c.Dock = DockStyle.Fill; t.Controls.Add(c, 1, row); }
    private string Get(string key, string def = "") => _settings.TryGetValue(key, out var v) ? v : def;
    private void LoadExisting()
    {
        _business.Text = Get("business_name", "TI ZILL DIGITAL"); _address.Text = Get("address", "Mauritius"); _phone.Text = Get("phone"); _currency.Text = Get("currency", "MUR"); _footer.Text = Get("receipt_footer", "Thank you for your purchase!");
        var t = Get("printer_type", "USB"); _usb.Checked = t == "USB"; _serial.Checked = t == "Serial"; _network.Checked = t == "Network"; _windows.Checked = t == "Windows";
        _vid.Text = Get("printer_usb_vid"); _pid.Text = Get("printer_usb_pid"); _com.Text = Get("printer_serial_port", "COM1"); _baud.Text = Get("printer_serial_baud", "9600"); _ip.Text = Get("printer_network_ip"); _port.Value = decimal.TryParse(Get("printer_network_port", "9100"), out var p) ? p : 9100; _winPrinter.Text = Get("printer_windows_name");
    }
    private void Save(string key, string value) => _db.SaveSetting(key, value);
    private void SavePrinter()
    {
        Save("printer_type", _usb.Checked ? "USB" : _serial.Checked ? "Serial" : _network.Checked ? "Network" : "Windows");
        Save("printer_usb_vid", _vid.Text.Trim()); Save("printer_usb_pid", _pid.Text.Trim()); Save("printer_serial_port", _com.Text.Trim()); Save("printer_serial_baud", _baud.Text.Trim()); Save("printer_network_ip", _ip.Text.Trim()); Save("printer_network_port", _port.Value.ToString()); Save("printer_windows_name", _winPrinter.Text.Trim());
        MessageBox.Show("Printer settings saved.");
    }
    private void DetectUsb()
    {
        try
        {
            var psi = new ProcessStartInfo("cmd", "/c wmic path Win32_PnPEntity get PNPDeviceID") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var proc = Process.Start(psi);
            var output = proc?.StandardOutput.ReadToEnd() ?? string.Empty;
            proc?.WaitForExit(3000);
            foreach (var line in output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
            {
                var vid = System.Text.RegularExpressions.Regex.Match(line, "VID_([0-9A-F]{4})", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value;
                var pid = System.Text.RegularExpressions.Regex.Match(line, "PID_([0-9A-F]{4})", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(vid) && !string.IsNullOrWhiteSpace(pid)) { _vid.Text = vid; _pid.Text = pid; MessageBox.Show("USB printer detected."); return; }
            }
            MessageBox.Show("No USB printer VID/PID detected.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("USB detect failed: " + ex.Message);
        }
    }
}
