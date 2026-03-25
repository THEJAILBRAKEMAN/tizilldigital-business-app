namespace TiZillDigital.Models;

public class CreditSale
{
    public string Id { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public string SaleDate { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public decimal Outstanding => Amount - Paid;
}
