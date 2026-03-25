namespace TiZillDigital.Models;

public class GameKey
{
    public string Id { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string ProductKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Available";
    public string AddedDate { get; set; } = string.Empty;
    public string? SoldDate { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}
