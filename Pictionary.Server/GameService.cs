using Pictionary.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Pictionary.Server.Services;

public class GameService
{
    private readonly Random _random = new();
    private GameState _gameState = new();
    private readonly List<string> _words = new()
    {
        "dog", "cat", "house", "car", "tree", "flower", "sun", "moon", "star",
        "book", "chair", "table", "phone", "computer", "pizza", "banana"
    };

    private Timer? _countdownTimer;
    
    // Add an event for game state changes
    public event Action? GameStateChanged;
    
    public GameState GetGameState() => _gameState;
    
    public string GetRandomWord() => _words[_random.Next(_words.Count)];
    
    public void AddPlayer(string connectionId, string name)
    {
        var player = new Player
        {
            ConnectionId = connectionId,
            Name = name
        };
        
        _gameState.Players.Add(player);
        
        // Start countdown if we have at least 2 players and not already counting down or playing
        if (_gameState.Players.Count >= 2 && _gameState.Status == GameStatus.WaitingForPlayers)
        {
            StartCountdown();
        }
        
        // Notify clients about the updated game state
        GameStateChanged?.Invoke();
    }
    
    public void RemovePlayer(string connectionId)
    {
        _gameState.Players.RemoveAll(p => p.ConnectionId == connectionId);
        
        // If drawer leaves during the game, end the round and start a new one
        if (_gameState.Status == GameStatus.Playing && connectionId == _gameState.DrawerId)
        {
            ResetGame();
        }
        
        // Reset the game if we don't have enough players
        if (_gameState.Players.Count < 2)
        {
            ResetGame();
        }
        
        // Notify clients about the updated game state
        GameStateChanged?.Invoke();
    }
    
    public bool CheckGuess(string connectionId, string guess)
    {
        // Only non-drawer can guess and only during active gameplay
        if (_gameState.Status != GameStatus.Playing || connectionId == _gameState.DrawerId)
            return false;
            
        bool isCorrect = string.Equals(guess.Trim(), _gameState.CurrentWord, StringComparison.OrdinalIgnoreCase);
        
        if (isCorrect)
        {
            var player = _gameState.Players.First(p => p.ConnectionId == connectionId);
            player.Score += 10;
            
            // Add score to drawer too
            var drawer = _gameState.Players.First(p => p.ConnectionId == _gameState.DrawerId);
            drawer.Score += 5;
            
            // Notify clients about the updated game state
            GameStateChanged?.Invoke();
        }
        
        return isCorrect;
    }
    
    public void StartCountdown()
    {
        _gameState.Status = GameStatus.Countdown;
        _gameState.CountdownSeconds = 5;
        
        // Notify clients about the initial countdown
        GameStateChanged?.Invoke();
        
        _countdownTimer = new Timer(_ =>
        {
            _gameState.CountdownSeconds--;
            
            // Notify clients about the updated countdown
            GameStateChanged?.Invoke();
            
            if (_gameState.CountdownSeconds <= 0)
            {
                StartGame();
            }
        }, null, 0, 1000);
    }
    
    public void StartGame()
    {
        _countdownTimer?.Dispose();
        _countdownTimer = null;
        
        _gameState.Status = GameStatus.Playing;
        _gameState.CurrentWord = GetRandomWord();
        
        // Randomly select a drawer
        int drawerIndex = _random.Next(_gameState.Players.Count);
        var drawer = _gameState.Players[drawerIndex];
        
        _gameState.Players.ForEach(p => p.IsDrawer = false);
        drawer.IsDrawer = true;
        _gameState.DrawerId = drawer.ConnectionId;
        
        // Notify clients about the game starting
        GameStateChanged?.Invoke();
    }
    
    public void ResetGame()
    {
        _countdownTimer?.Dispose();
        _countdownTimer = null;
        
        _gameState.Status = _gameState.Players.Count >= 2 
            ? GameStatus.Countdown 
            : GameStatus.WaitingForPlayers;
            
        _gameState.CurrentWord = string.Empty;
        _gameState.DrawerId = null;
        _gameState.Players.ForEach(p => p.IsDrawer = false);
        
        // Notify clients about the game state reset
        GameStateChanged?.Invoke();
        
        if (_gameState.Status == GameStatus.Countdown)
        {
            StartCountdown();
        }
    }
}