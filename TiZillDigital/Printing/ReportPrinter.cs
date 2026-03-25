using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace TiZillDigital.Printing;

public static class ReportPrinter
{
    public static void PrintLines(string title, IEnumerable<string> lines)
    {
        var list = lines.ToList();
        var index = 0;
        var doc = new PrintDocument();
        doc.DocumentName = title;
        doc.PrintPage += (_, e) =>
        {
            using var fTitle = new Font("Segoe UI", 14, FontStyle.Bold);
            using var fBody = new Font("Consolas", 10);
            var y = 20f;
            e.Graphics.DrawString(title, fTitle, Brushes.Black, 20, y);
            y += 40;
            while (index < list.Count)
            {
                e.Graphics.DrawString(list[index], fBody, Brushes.Black, 20, y);
                y += 20;
                index++;
                if (y > e.MarginBounds.Bottom - 20)
                {
                    e.HasMorePages = true;
                    return;
                }
            }
            e.HasMorePages = false;
        };
        using var preview = new PrintPreviewDialog { Document = doc, Width = 1000, Height = 700 };
        preview.ShowDialog();
    }
}
