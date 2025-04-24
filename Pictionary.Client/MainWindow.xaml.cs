using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.AspNetCore.SignalR.Client;
using Pictionary.Shared;
using Pictionary.Shared.Models;

namespace Pictionary.Client
{
    public partial class MainWindow : Window
    {
        private HubConnection? _connection;
        private bool _isDrawing;
        private System.Windows.Point _lastPoint; // Use fully qualified name here
        private bool _isDrawer;
        private string? _currentWord;
        private ObservableCollection<Player> _players = new();

        public MainWindow()
        {
            InitializeComponent();
            PlayersListBox.ItemsSource = _players;
            PlayersListBox.DisplayMemberPath = "Name";

            ClearCanvasButton.IsEnabled = false;
            GuessInput.IsEnabled = false;
            SubmitButton.IsEnabled = false;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_connection != null)
                return;

            // Create connection - updated port to match server configuration
            _connection = new HubConnectionBuilder()
                .WithUrl($"http://localhost:5088{Constants.HubUrl}")
                .WithAutomaticReconnect()
                .Build();

            // Set up handlers for receiving messages
            _connection.On<GameState>("UpdateGameState", OnGameStateUpdated);
            _connection.On<DrawingUpdate>("ReceiveDrawing", OnDrawingReceived);
            _connection.On("ClearCanvas", OnClearCanvas);
            _connection.On<string, string>("CorrectGuess", OnCorrectGuess);

            try
            {
                await _connection.StartAsync();
                GameStatusText.Text = "Connected to server";
                ConnectButton.IsEnabled = false;
                PlayerNameInput.IsEnabled = false;

                // Join the game with player name
                string playerName = string.IsNullOrWhiteSpace(PlayerNameInput.Text) ? "Player" : PlayerNameInput.Text;
                await _connection.InvokeAsync("JoinGame", playerName);

                // Get current game state
                await _connection.InvokeAsync("GetGameState");
            }
            catch (Exception ex)
            {
                GameStatusText.Text = $"Connection failed: {ex.Message}";
            }
        }

        private void OnGameStateUpdated(GameState gameState)
        {
            Dispatcher.Invoke(() =>
            {
                _players.Clear();
                foreach (var player in gameState.Players)
                {
                    _players.Add(player);
                }

                var myPlayer = gameState.Players.FirstOrDefault(p => _connection != null && p.ConnectionId == _connection.ConnectionId);
                _isDrawer = myPlayer?.IsDrawer ?? false;

                switch (gameState.Status)
                {
                    case GameStatus.WaitingForPlayers:
                        GameStatusText.Text = "Waiting for players...";
                        GameInfoText.Text = $"Players: {gameState.Players.Count}/2 minimum";
                        ClearCanvasButton.IsEnabled = false;
                        GuessInput.IsEnabled = false;
                        SubmitButton.IsEnabled = false;
                        break;

                    case GameStatus.Countdown:
                        GameStatusText.Text = "Game is starting...";
                        GameInfoText.Text = $"Game starts in {gameState.CountdownSeconds} seconds";
                        ClearCanvasButton.IsEnabled = false;
                        GuessInput.IsEnabled = false;
                        SubmitButton.IsEnabled = false;
                        break;

                    case GameStatus.Playing:
                        if (_isDrawer)
                        {
                            GetWord();
                            GameStatusText.Text = "Your turn to draw!";
                            GameInfoText.Text = $"Draw: {_currentWord}";
                            ClearCanvasButton.IsEnabled = true;
                            GuessInput.IsEnabled = false;
                            SubmitButton.IsEnabled = false;
                        }
                        else
                        {
                            GameStatusText.Text = "Guess the word!";
                            GameInfoText.Text = "Type your guess below";
                            ClearCanvasButton.IsEnabled = false;
                            GuessInput.IsEnabled = true;
                            SubmitButton.IsEnabled = true;
                        }
                        break;

                    case GameStatus.RoundEnd:
                        GameStatusText.Text = "Round ended!";
                        ClearCanvasButton.IsEnabled = false;
                        GuessInput.IsEnabled = false;
                        SubmitButton.IsEnabled = false;
                        break;
                }
            });
        }

        private async void GetWord()
        {
            if (_connection != null && _isDrawer)
            {
                _currentWord = await _connection.InvokeAsync<string>("GetWord");
            }
        }

        private void OnDrawingReceived(DrawingUpdate drawingUpdate)
        {
            Dispatcher.Invoke(() =>
            {
                if (!_isDrawer)
                {
                    DrawLine(
                        drawingUpdate.From.X,
                        drawingUpdate.From.Y,
                        drawingUpdate.To.X,
                        drawingUpdate.To.Y,
                        drawingUpdate.Color,
                        drawingUpdate.Thickness
                    );
                }
            });
        }

        private void OnClearCanvas()
        {
            Dispatcher.Invoke(() =>
            {
                WhiteboardCanvas.Children.Clear();
            });
        }

        private void OnCorrectGuess(string playerId, string word)
        {
            Dispatcher.Invoke(() =>
            {
                var player = _players.FirstOrDefault(p => p.ConnectionId == playerId);
                if (player != null)
                {
                    GameInfoText.Text = $"{player.Name} guessed correctly: {word}";
                }
            });
        }

        private void WhiteboardCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawer)
            {
                _isDrawing = true;
                _lastPoint = e.GetPosition(WhiteboardCanvas);
            }
        }

        private async void WhiteboardCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawer && _isDrawing && _connection != null)
            {
                System.Windows.Point currentPoint = e.GetPosition(WhiteboardCanvas); // Use fully qualified name here

                // Draw line locally
                DrawLine(_lastPoint.X, _lastPoint.Y, currentPoint.X, currentPoint.Y, "#000000", 2);

                // Send drawing update to server
                var drawingUpdate = new DrawingUpdate
                {
                    From = new Pictionary.Shared.Models.Point { X = _lastPoint.X, Y = _lastPoint.Y },
                    To = new Pictionary.Shared.Models.Point { X = currentPoint.X, Y = currentPoint.Y },
                    Color = "#000000",
                    Thickness = 2
                };

                await _connection.InvokeAsync("SendDrawing", drawingUpdate);

                _lastPoint = currentPoint;
            }
        }

        private void WhiteboardCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawer)
            {
                _isDrawing = false;
            }
        }

        private void DrawLine(double x1, double y1, double x2, double y2, string colorHex, int thickness)
        {
            Line line = new Line
            {
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                StrokeThickness = thickness,
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2
            };

            WhiteboardCanvas.Children.Add(line);
        }

        private async void ClearCanvasButton_Click(object sender, RoutedEventArgs e)
        {
            if (_connection != null && _isDrawer)
            {
                WhiteboardCanvas.Children.Clear();
                await _connection.InvokeAsync("ClearCanvas");
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            await SubmitGuess();
        }

        private async void GuessInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SubmitGuess();
            }
        }

        private async System.Threading.Tasks.Task SubmitGuess()
        {
            if (_connection != null && !_isDrawer && !string.IsNullOrWhiteSpace(GuessInput.Text))
            {
                await _connection.InvokeAsync("SubmitGuess", GuessInput.Text);
                GuessInput.Clear();
            }
        }
    }
}