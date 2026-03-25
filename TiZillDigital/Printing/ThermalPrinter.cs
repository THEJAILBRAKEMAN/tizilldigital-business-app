using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ESCPOS_NET;
using ESCPOS_NET.Emitters;
using TiZillDigital.Database;

namespace TiZillDigital.Printing;

public class ThermalPrinter
{
    private readonly DatabaseManager _db;
    private readonly Dictionary<string, string> _settings;

    public ThermalPrinter(DatabaseManager db)
    {
        _db = db;
        _settings = _db.GetSettings();
    }

    public bool TestConnection(out string error)
    {
        try
        {
            var e = new EPSON();
            Print(e.CenterAlign().Concat(e.PrintLine("TI ZILL DIGITAL TEST")).Concat(e.PrintLine(DateTime.Now.ToString("G"))).Concat(e.PrintLine("OK")).Concat(e.FeedLines(3)).Concat(e.FullCutAfterFeed()).ToArray());
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public void Print(ReceiptDocument doc) => Print(doc.Data);

    public void Print(byte[] data)
    {
        try
        {
            var type = Get("printer_type", "USB");
            if (type == "Network")
            {
                var ip = Get("printer_network_ip", "127.0.0.1");
                var port = Get("printer_network_port", "9100");
                var printer = new NetworkPrinter(ip, int.Parse(port));
                printer.Write(data);
            }
            else if (type == "Serial")
            {
                var port = Get("printer_serial_port", "COM1");
                var baud = int.Parse(Get("printer_serial_baud", "9600"));
                var printer = new SerialPrinter(port, baudRate: baud);
                printer.Write(data);
            }
            else if (type == "Windows")
            {
                var pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = Get("printer_windows_name", "");
                pd.PrintPage += (_, e) => { e.Graphics.DrawString("Raw ESC/POS sent via fallback unavailable; using text test.", new Font("Consolas", 10), Brushes.Black, 5, 5); };
                pd.Print();
            }
            else
            {
                var file = new FilePrinter($"\\\\.\\{Get("printer_serial_port", "USB001")}");
                file.Write(data);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Printer Error: " + ex.Message, "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string Get(string key, string fallback) => _settings.TryGetValue(key, out var v) ? v : fallback;
}
