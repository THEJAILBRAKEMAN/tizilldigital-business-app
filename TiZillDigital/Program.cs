using System.Windows.Forms;
using TiZillDigital.Database;
using TiZillDigital.Forms;

namespace TiZillDigital;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var db = new DatabaseManager("tizill.db");
        Application.Run(new MainForm(db));
    }
}
