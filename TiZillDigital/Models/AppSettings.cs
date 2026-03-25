namespace TiZillDigital.Models;

public class AppSettings
{
    public string BusinessName { get; set; } = "TI ZILL DIGITAL";
    public string Address { get; set; } = "Mauritius";
    public string Phone { get; set; } = string.Empty;
    public string PrinterType { get; set; } = "USB";
    public string PrinterUsbVid { get; set; } = string.Empty;
    public string PrinterUsbPid { get; set; } = string.Empty;
    public string PrinterSerialPort { get; set; } = "COM1";
    public string PrinterSerialBaud { get; set; } = "9600";
    public string PrinterNetworkIp { get; set; } = string.Empty;
    public string PrinterNetworkPort { get; set; } = "9100";
    public string PrinterWindowsName { get; set; } = string.Empty;
    public string Currency { get; set; } = "MUR";
    public string ReceiptFooter { get; set; } = "Thank you for your purchase!";
    public bool ReceiptShowLogo { get; set; }
}
