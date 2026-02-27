using IronOcr;
using Microsoft.Win32;
using OpenCvSharp;
using ScrabbleSolver;
using System.Diagnostics;
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
                List<(int row, int col)>? unrecognizedBoardTiles = null;
                List<int>? unrecognizedRackTiles = null;

                await Task.Run(() =>
                {
                    // Process rack
                    var rackImagePrep = new RackImagePrep();
                    rackState = rackImagePrep.Run(_fileName);
                    unrecognizedRackTiles = rackImagePrep.GetUnrecognizedTiles();

                    // Process board
                    var boardImagePrep = new BoardImagePrep();
                    Cv2.DestroyAllWindows();
                    boardState = boardImagePrep.Run(_fileName);
                    unrecognizedBoardTiles = boardImagePrep.GetUnrecognizedTiles();
                });

                HideBusyIndicator();

                // Show OCR results window
                var ocrResultsWindow = new OcrResultsWindow(
                    _fileName,
                    boardState!,
                    rackState!,
                    unrecognizedBoardTiles!,
                    unrecognizedRackTiles!
                );

                var result = ocrResultsWindow.ShowDialog();

                if (result == true)
                {
                    // User clicked Continue
                    CurrentBoardControl.SetBoardState(boardState!, unrecognizedBoardTiles);
                    RackControl.SetRack(rackState!, unrecognizedRackTiles);

                    int totalUnrecognized = unrecognizedBoardTiles!.Count + unrecognizedRackTiles!.Count;
                    if (totalUnrecognized > 0)
                    {
                        StatusTextBlock.Text = $"⚠️ Image loaded. {totalUnrecognized} tile(s) not recognized - please correct.";
                        StatusTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    else
                    {
                        StatusTextBlock.Text = $"✓ Image processed successfully! Rack: {RackControl.GetRackString()}";
                        StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    }
                }
                else if (ocrResultsWindow.RescanRequested)
                {
                    // User wants to select a different image
                    BrowseButton_Click(sender, e);
                }
                else
                {
                    StatusTextBlock.Text = "OCR review cancelled.";
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
                    moveInfo.AppendLine($"Word: {string.Join("", changedPositions.Select(p => p.Item1))}");
                    moveInfo.AppendLine($"Changed Positions: {string.Join(", ", changedPositions.Select(pos => $"{pos.Item1} at ({pos.Item2 + 1}, {pos.Item3 + 1})"))}");

                    MoveInfoTextBox.Text = moveInfo.ToString();

                    // Highlight the new tiles on the move board
                    var highlightPositions = changedPositions.Select(p => (p.Item2, p.Item3)).ToList();
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
                moveInfo.AppendLine($"Word: {string.Join("", changedPositions.Select(p => p.Item1))}");
                moveInfo.AppendLine($"Changed Positions: {string.Join(", ", changedPositions.Select(pos => $"{pos.Item1} at ({pos.Item2 + 1}, {pos.Item3 + 1})"))}");

                MoveInfoTextBox.Text = moveInfo.ToString();

                // Highlight the new tiles on the move board
                var highlightPositions = changedPositions.Select(p => (p.Item2, p.Item3)).ToList();
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
    }
}
