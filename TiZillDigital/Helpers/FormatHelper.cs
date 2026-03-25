namespace TiZillDigital.Helpers;

public static class FormatHelper
{
    public static string FormatMUR(decimal amount) => $"MUR {amount:N2}";
    public static string FormatDate(string isoDate) => DateTime.Parse(isoDate).ToString("dd MMM yyyy");
    public static string PadColumns(string left, string right, int width = 48)
    {
        var padding = width - left.Length - right.Length;
        if (padding < 1) padding = 1;
        return left + new string(' ', padding) + right;
    }
    public static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
    public static string Today() => DateTime.Now.ToString("yyyy-MM-dd");
    public static string ShortId() => Guid.NewGuid().ToString("N")[..10].ToUpper();
}
