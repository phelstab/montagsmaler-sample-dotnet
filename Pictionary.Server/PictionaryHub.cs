using Microsoft.AspNetCore.SignalR;
using Pictionary.Server.Services;
using Pictionary.Shared.Models;

namespace Pictionary.Server.Hubs;

public class PictionaryHub : Hub
{
    private readonly GameService _gameService;
    private readonly IHubContext<PictionaryHub> _hubContext;

    public PictionaryHub(GameService gameService, IHubContext<PictionaryHub> hubContext)
    {
        _gameService = gameService;
        _hubContext = hubContext;
        
        // Subscribe to game state changes
        _gameService.GameStateChanged += async () => 
        {
            await _hubContext.Clients.All.SendAsync("UpdateGameState", _gameService.GetGameState());
        };
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _gameService.RemovePlayer(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGame(string playerName)
    {
        _gameService.AddPlayer(Context.ConnectionId, playerName);
    }

    public async Task SendDrawing(DrawingUpdate drawingUpdate)
    {
        var gameState = _gameService.GetGameState();
        if (gameState.Status == GameStatus.Playing && Context.ConnectionId == gameState.DrawerId)
        {
            await Clients.Others.SendAsync("ReceiveDrawing", drawingUpdate);
        }
    }

    public async Task ClearCanvas()
    {
        var gameState = _gameService.GetGameState();
        if (gameState.Status == GameStatus.Playing && Context.ConnectionId == gameState.DrawerId)
        {
            await Clients.All.SendAsync("ClearCanvas");
        }
    }

    public async Task SubmitGuess(string guess)
    {
        bool isCorrect = _gameService.CheckGuess(Context.ConnectionId, guess);
        
        if (isCorrect)
        {
            var gameState = _gameService.GetGameState();
            await Clients.All.SendAsync("CorrectGuess", Context.ConnectionId, gameState.CurrentWord);
            
            // After a correct guess, wait 3 seconds and start a new round
            await Task.Delay(3000);
            _gameService.ResetGame();
        }
    }

    public async Task GetGameState()
    {
        await Clients.Caller.SendAsync("UpdateGameState", _gameService.GetGameState());
    }
    
    public string GetWord()
    {
        var gameState = _gameService.GetGameState();
        if (gameState.Status == GameStatus.Playing && Context.ConnectionId == gameState.DrawerId)
        {
            return gameState.CurrentWord;
        }
        return string.Empty;
    }
}