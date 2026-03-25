namespace TiZillDigital.Models;

public class KeyOrder
{
    public string Id { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public string Platform { get; set; } = "Steam";
    public decimal Price { get; set; }
    public string ProductKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string OrderDate { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
