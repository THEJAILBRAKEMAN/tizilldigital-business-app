namespace TiZillDigital.Models;

public class Game
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Platform { get; set; } = "Steam";
    public string Genre { get; set; } = "Action";
    public decimal CostPrice { get; set; }
    public decimal SellPrice { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string AddedDate { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
