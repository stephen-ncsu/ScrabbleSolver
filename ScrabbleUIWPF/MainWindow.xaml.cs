using IronOcr;
using Microsoft.Win32;
using OpenCvSharp;
using ScrabbleSolver;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace ScrabbleUIWPF
{
    public partial class MainWindow : System.Windows.Window
    {
        private char[,] _boardState = new char[15, 15];
        private List<Move> _moves = new List<Move>();
        private int _errorCount = 0;
        private string _fileName = @".\TestData\scrabble_board.png";
        private string? _currentBoardFile = null;

        public MainWindow()
        {
            InitializeComponent();
            IronOcr.License.LicenseKey = "IRONSUITE.UOPITT1.GMAIL.COM.4621-25E37A5567-BYUH7GI3BXY25WGR-NMHDJVUDHSD4-PNXR7RRD5ZKN-DBQOJRG2YGPC-FQGWGZDNDFUW-AKSBDAQ35NZM-SP7NSC-TZFRWH2KXV2QUA-DEPLOYMENT.TRIAL-HI4GH5.TRIAL.EXPIRES.13.MAR.2026";
            FilePathTextBox.Text = _fileName;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*",
                Title = "Select Scrabble Board Image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _fileName = openFileDialog.FileName;
                FilePathTextBox.Text = _fileName;
                StatusTextBlock.Text = "File selected. Click 'Process Image' to analyze.";
            }
        }

        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowBusyIndicator("Processing image...");
                StatusTextBlock.Text = "Processing image...";

                char[,]? boardState = null;
                char[]? rackState = null;
                List<int>? unrecognizedRackTiles = null;
                Dictionary<(int row, int col), OpenCvSharp.Mat>? boardTileImages = null;
                Dictionary<int, OpenCvSharp.Mat>? rackTileImages = null;

                await Task.Run(() =>
                {
                    // Process rack
                    var rackImagePrep = new RackImagePrep();
                    rackState = rackImagePrep.Run(_fileName);
                    unrecognizedRackTiles = rackImagePrep.GetUnrecognizedTiles();
                    rackTileImages = rackImagePrep.GetUnrecognizedTileImages();

                    // Process board
                    var boardImagePrep = new BoardImagePrep();
                    Cv2.DestroyAllWindows();
                    boardState = boardImagePrep.Run(_fileName);
                    boardTileImages = boardImagePrep.GetUnrecognizedTileImages();
                });

                HideBusyIndicator();

                // Check if there are tiles to correct
                int totalUnrecognized = boardTileImages!.Count + unrecognizedRackTiles!.Count;

                if (totalUnrecognized > 0)
                {
                    // Show tile correction workflow
                    var tileCorrectionWindow = new TileCorrectionWindow(
                        boardState!,
                        rackState!,
                        boardTileImages,
                        rackTileImages
                    );

                    var correctionResult = tileCorrectionWindow.ShowDialog();

                    if (correctionResult == true)
                    {
                        // Get corrected states
                        boardState = tileCorrectionWindow.GetCorrectedBoardState();
                        rackState = tileCorrectionWindow.GetCorrectedRackState();

                        // Check how many are still uncorrected (marked as *)
                        int stillUnrecognized = CountUnrecognizedTiles(boardState, rackState);

                        // Update board and rack - no highlighting since tiles are corrected
                        CurrentBoardControl.SetBoardState(boardState!, null);
                        RackControl.SetRack(rackState!, null);

                        if (stillUnrecognized > 0)
                        {
                            StatusTextBlock.Text = $"⚠️ Image loaded. {stillUnrecognized} tile(s) still marked as wildcard (*). You can edit them if needed.";
                            StatusTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                        }
                        else
                        {
                            StatusTextBlock.Text = $"✓ All tiles corrected! Rack: {new string(rackState).Trim()}";
                            StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                        }
                    }
                    else
                    {
                        // User cancelled correction
                        StatusTextBlock.Text = "Tile correction cancelled.";
                        StatusTextBlock.Foreground = new SolidColorBrush(Colors.Gray);
                    }
                }
                else
                {
                    // No corrections needed - directly update board and rack
                    CurrentBoardControl.SetBoardState(boardState!, null);
                    RackControl.SetRack(rackState!, null);

                    StatusTextBlock.Text = $"✓ Image processed successfully! Rack: {new string(rackState).Trim()}";
                    StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing image: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error processing image.";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
            finally
            {
                HideBusyIndicator();
            }
        }

        private async void SolveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rackString = RackControl.GetRackString();

                if (string.IsNullOrWhiteSpace(rackString))
                {
                    MessageBox.Show("Please add tiles to your rack before solving.", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ShowBusyIndicator("Solving...");
                StatusTextBlock.Text = "Solving...";

                // Get current board state from the control
                _boardState = CurrentBoardControl.GetBoardState();

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                await Task.Run(() =>
                {
                    var solver = new Solver(_boardState, rackString);
                    _moves = solver.Solve();
                });

                stopWatch.Stop();

                Serilog.Log.Logger.Information("Solver found {MoveCount} moves in {ElapsedMilliseconds} ms", 
                    _moves.Count, stopWatch.ElapsedMilliseconds);

                StatusTextBlock.Text = $"Found {_moves.Count} moves in {stopWatch.Elapsed.TotalSeconds:F2}s. Click 'Calculate Scores' to rank them.";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                MoveInfoTextBox.Text = $"Found {_moves.Count} possible moves.\n\nClick 'Calculate Scores' to evaluate and rank them.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error solving: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error during solve.";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
            finally
            {
                HideBusyIndicator();
            }
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_moves.Count == 0)
                {
                    MessageBox.Show("Please solve first before calculating scores.", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StatusTextBlock.Text = "Calculating scores...";
                var scorer = new Scorer();
                foreach (var move in _moves)
                {
                    move.Score = scorer.GetScoreForMove(move);
                }

                _moves = _moves.OrderByDescending(m => m.Score).ToList();

                if (_moves.Any())
                {
                    MoveIndexTextBox.Text = "0";

                    var bestMove = _moves[0];
                    var changedPositions = bestMove.GetChangedPositions();

                    StringBuilder moveInfo = new StringBuilder();
                    moveInfo.AppendLine($"Best Move Score: {bestMove.Score}");
                    moveInfo.AppendLine($"Word: {string.Join("", changedPositions.Select(p => p.Letter))}");
                    moveInfo.AppendLine($"Changed Positions: {string.Join(", ", changedPositions.Select(pos => $"{pos.Letter} at ({pos.Col + 1}, {pos.Row + 1})"))}");

                    MoveInfoTextBox.Text = moveInfo.ToString();

                    // Highlight the new tiles on the move board
                    var highlightPositions = changedPositions.Select(p => (p.Col, p.Row)).ToList();
                    MovesBoardControl.SetBoardState(bestMove.GetBoardState(), highlightPositions);

                    StatusTextBlock.Text = $"Best move: {bestMove.Score} points. Total {_moves.Count} moves found.";
                }
                else
                {
                    MoveInfoTextBox.Text = "No valid moves found.";
                    StatusTextBlock.Text = "No valid moves found.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating scores: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error calculating scores.";
            }
        }

        private void ShowMoveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(MoveIndexTextBox.Text, out int moveIndex) || 
                    moveIndex < 0 || moveIndex >= _moves.Count)
                {
                    MessageBox.Show($"Invalid move index. Please enter a number between 0 and {_moves.Count - 1}.", 
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var move = _moves[moveIndex];
                var changedPositions = move.GetChangedPositions();

                StringBuilder moveInfo = new StringBuilder();
                moveInfo.AppendLine($"Move #{moveIndex} - Score: {move.Score}");
                moveInfo.AppendLine($"Word: {string.Join("", changedPositions.Select(p => p.Letter))}");
                moveInfo.AppendLine($"Changed Positions: {string.Join(", ", changedPositions.Select(pos => $"{pos.Letter} at ({pos.Col + 1}, {pos.Row + 1})"))}");

                MoveInfoTextBox.Text = moveInfo.ToString();

                // Highlight the new tiles on the move board
                var highlightPositions = changedPositions.Select(p => (p.Col, p.Row)).ToList();
                MovesBoardControl.SetBoardState(move.GetBoardState(), highlightPositions);

                StatusTextBlock.Text = $"Showing move #{moveIndex} with score {move.Score}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing move: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowBusyIndicator(string message)
        {
            BusyMessage.Text = message;
            BusyOverlay.Visibility = Visibility.Visible;
        }

        private void HideBusyIndicator()
        {
            BusyOverlay.Visibility = Visibility.Collapsed;
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear the board and rack?",
                "Clear Board",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Clear both board controls
                CurrentBoardControl.ClearBoard();
                MovesBoardControl.ClearBoard();

                // Clear rack
                RackControl.ClearRack();

                // Clear moves
                _moves.Clear();

                // Clear board state
                _boardState = new char[15, 15];

                // Clear UI
                MoveInfoTextBox.Clear();
                MoveIndexTextBox.Text = "0";
                FilePathTextBox.Clear();

                // Clear board file reference
                _currentBoardFile = null;
                BoardFileLabel.Text = string.Empty;

                // Reset status
                StatusTextBlock.Text = "Board and rack cleared. Load an image to start.";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void SaveBoardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Scrabble Board Files (*.scrabble)|*.scrabble|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = ".scrabble",
                    Title = "Save Board State",
                    FileName = $"Board_{DateTime.Now:yyyyMMdd_HHmmss}.scrabble"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Get current board state
                    var currentBoard = CurrentBoardControl.GetBoardState();
                    var currentRack = RackControl.GetRack();

                    // Create board state data
                    var boardStateData = BoardStateData.FromBoardAndRack(currentBoard, currentRack, _fileName);

                    // Save to file
                    boardStateData.SaveToFile(saveFileDialog.FileName);

                    _currentBoardFile = saveFileDialog.FileName;
                    BoardFileLabel.Text = $"Saved: {Path.GetFileName(saveFileDialog.FileName)}";

                    StatusTextBlock.Text = $"Board saved successfully to {Path.GetFileName(saveFileDialog.FileName)}";
                    StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);

                    MessageBox.Show($"Board state saved successfully!", "Save Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving board: {ex.Message}", "Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error saving board.";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void LoadBoardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Scrabble Board Files (*.scrabble)|*.scrabble|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    Title = "Load Board State"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // Load board state data
                    var boardStateData = BoardStateData.LoadFromFile(openFileDialog.FileName);

                    if (boardStateData == null)
                    {
                        MessageBox.Show("Failed to load board state. File may be corrupted.", "Load Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Apply to UI
                    var loadedBoard = boardStateData.GetBoard();
                    var loadedRack = boardStateData.GetRack();

                    CurrentBoardControl.SetBoardState(loadedBoard);
                    RackControl.SetRack(loadedRack);

                    // Update board state
                    _boardState = loadedBoard;

                    // Update file references
                    _currentBoardFile = openFileDialog.FileName;
                    BoardFileLabel.Text = $"Loaded: {Path.GetFileName(openFileDialog.FileName)}";

                    if (!string.IsNullOrEmpty(boardStateData.OriginalImagePath))
                    {
                        _fileName = boardStateData.OriginalImagePath;
                        FilePathTextBox.Text = boardStateData.OriginalImagePath;
                    }

                    // Clear moves (board changed)
                    _moves.Clear();
                    MoveInfoTextBox.Clear();
                    MoveIndexTextBox.Text = "0";

                    StatusTextBlock.Text = $"Board loaded successfully from {Path.GetFileName(openFileDialog.FileName)} (Saved: {boardStateData.SavedDate:g})";
                    StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);

                    MessageBox.Show($"Board state loaded successfully!\n\nSaved: {boardStateData.SavedDate:g}\nRack: {boardStateData.Rack}",
                        "Load Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading board: {ex.Message}", "Load Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error loading board.";
                StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private int CountUnrecognizedTiles(char[,] boardState, char[] rackState)
        {
            int count = 0;

            // Count board wildcards
            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    if (boardState[row, col] == '*')
                    {
                        count++;
                    }
                }
            }

            // Count rack wildcards (but only if they're not intentional wildcards)
            // We can't distinguish, so we just count them
            for (int i = 0; i < 7; i++)
            {
                if (rackState[i] == '*')
                {
                    count++;
                }
            }

            return count;
        }
    }
}
