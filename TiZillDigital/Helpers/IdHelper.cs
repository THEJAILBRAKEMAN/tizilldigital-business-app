namespace TiZillDigital.Helpers;

public static class IdHelper
{
    public static string NewId() => Guid.NewGuid().ToString("N")[..10].ToUpper();
}
