namespace TiZillDigital.Models;

public class Expenditure
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = "Misc";
    public string ExpenseDate { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
