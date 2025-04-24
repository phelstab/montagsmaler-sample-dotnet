namespace Pictionary.Shared.Models;

public class Player
{
    public string ConnectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDrawer { get; set; }
    public int Score { get; set; }
}