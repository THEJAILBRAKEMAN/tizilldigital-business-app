namespace TiZillDigital.Models;

public class Payment
{
    public string Id { get; set; } = string.Empty;
    public string CreditId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaidDate { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
