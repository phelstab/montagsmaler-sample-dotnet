namespace Pictionary.Shared.Models;

public enum GameStatus
{
    WaitingForPlayers,
    Countdown,
    Playing,
    RoundEnd
}

public class GameState
{
    public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;
    public List<Player> Players { get; set; } = new();
    public string CurrentWord { get; set; } = string.Empty;
    public int CountdownSeconds { get; set; }
    public string? DrawerId { get; set; }
}