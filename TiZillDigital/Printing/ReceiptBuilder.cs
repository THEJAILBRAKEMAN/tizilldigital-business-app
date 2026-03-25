using ESCPOS_NET.Emitters;
using TiZillDigital.Helpers;
using TiZillDigital.Models;

namespace TiZillDigital.Printing;

public record DateRange(string Start, string End);
public record ReceiptDocument(byte[] Data);

public class ReceiptBuilder
{
    private readonly EPSON _e = new();
    private readonly string _business;
    private readonly string _address;
    private readonly string _footer;

    public ReceiptBuilder(string business, string address, string footer)
    {
        _business = business;
        _address = address;
        _footer = footer;
    }

    public ReceiptDocument PrintCreditReceipt(CreditSale sale)
    {
        var lines = new List<byte[]> {
            _e.CenterAlign(), _e.BoldOn(), _e.DoubleHeightWidthOn(), _e.PrintLine(_business), _e.DoubleHeightWidthOff(), _e.BoldOff(), _e.PrintLine(_address),
            _e.PrintLine(new string('-',48)), _e.BoldOn(), _e.PrintLine("CREDIT SALE RECEIPT"), _e.BoldOff(),
            _e.LeftAlign(), _e.PrintLine($"Receipt #: {FormatHelper.ShortId()}"), _e.PrintLine($"Date: {FormatHelper.FormatDate(sale.SaleDate)}"), _e.PrintLine(new string('-',48)),
            _e.PrintLine($"Customer : {sale.Customer}"), _e.PrintLine($"Phone    : {sale.Phone}"), _e.PrintLine($"Item     : {sale.Item}"), _e.PrintLine(new string('-',48)),
            _e.PrintLine(FormatHelper.PadColumns("Total Amount", FormatHelper.FormatMUR(sale.Amount))), _e.PrintLine(FormatHelper.PadColumns("Amount Paid", FormatHelper.FormatMUR(sale.Paid))),
            sale.Outstanding > 0 ? _e.BoldOn() : Array.Empty<byte>(), _e.PrintLine(FormatHelper.PadColumns("Outstanding", FormatHelper.FormatMUR(sale.Outstanding))), sale.Outstanding > 0 ? _e.BoldOff() : Array.Empty<byte>(),
            _e.PrintLine(new string('-',48)), _e.CenterAlign(), _e.PrintLine(_footer), _e.PrintLine(""), _e.PrintLine(""), _e.PrintLine(""), _e.PrintLine(""), _e.FullCutAfterFeed()
        };
        return new ReceiptDocument(lines.SelectMany(x => x).ToArray());
    }

    public ReceiptDocument PrintKeyDeliveryReceipt(KeyOrder order, bool includeKey)
    {
        var all = new List<byte[]> { _e.CenterAlign(), _e.BoldOn(), _e.DoubleHeightOn(), _e.PrintLine(_business), _e.DoubleHeightOff(), _e.BoldOff(), _e.PrintLine(_address), _e.PrintLine(new string('-',48)), _e.LeftAlign(), _e.PrintLine($"Receipt #: {FormatHelper.ShortId()}"), _e.PrintLine($"Date: {FormatHelper.FormatDate(order.OrderDate)}"), _e.PrintLine($"Customer: {order.Customer}"), _e.PrintLine($"Phone: {order.Phone}"), _e.PrintLine(new string('-',48)), _e.PrintLine($"Game: {order.Game}"), _e.PrintLine($"Platform: {order.Platform}"), _e.PrintLine(FormatHelper.PadColumns("Price", FormatHelper.FormatMUR(order.Price))) };
        if (includeKey) all.AddRange([_e.PrintLine(new string('-',48)), _e.BoldOn(), _e.PrintLine($"KEY: {order.ProductKey}"), _e.BoldOff()]);
        all.AddRange([_e.PrintLine(new string('-',48)), _e.CenterAlign(), _e.PrintLine(_footer), _e.PrintLine(""), _e.PrintLine(""), _e.PrintLine(""), _e.PrintLine(""), _e.FullCutAfterFeed()]);
        return new ReceiptDocument(all.SelectMany(x => x).ToArray());
    }

    public ReceiptDocument PrintExpenseSummary(List<Expenditure> items, DateRange range)
    {
        var data = new List<byte[]> { _e.CenterAlign(), _e.BoldOn(), _e.PrintLine("EXPENSE SUMMARY"), _e.BoldOff(), _e.PrintLine($"{range.Start} to {range.End}"), _e.PrintLine(new string('-',48)), _e.LeftAlign() };
        foreach (var i in items) data.Add(_e.PrintLine(FormatHelper.PadColumns(FormatHelper.Truncate(i.Description, 30), FormatHelper.FormatMUR(i.Amount))));
        var total = items.Sum(i => i.Amount);
        data.AddRange([_e.PrintLine(new string('-',48)), _e.BoldOn(), _e.PrintLine(FormatHelper.PadColumns("Grand Total", FormatHelper.FormatMUR(total))), _e.BoldOff(), _e.PrintLine(""), _e.PrintLine(""), _e.PrintLine(""), _e.PrintLine(""), _e.FullCutAfterFeed()]);
        return new ReceiptDocument(data.SelectMany(x => x).ToArray());
    }

    public ReceiptDocument PrintGameCatalog(List<Game> games, Func<string, int> keyCounter)
    {
        var data = new List<byte[]> { _e.CenterAlign(), _e.BoldOn(), _e.PrintLine("GAME CATALOG"), _e.BoldOff(), _e.PrintLine(new string('-',48)), _e.LeftAlign() };
        foreach (var g in games) data.Add(_e.PrintLine(FormatHelper.PadColumns($"{FormatHelper.Truncate(g.Title,24)} ({keyCounter(g.Id)})", FormatHelper.FormatMUR(g.SellPrice))));
        data.AddRange([_e.PrintLine(""), _e.PrintLine(""), _e.PrintLine(""), _e.PrintLine(""), _e.FullCutAfterFeed()]);
        return new ReceiptDocument(data.SelectMany(x => x).ToArray());
    }
}
