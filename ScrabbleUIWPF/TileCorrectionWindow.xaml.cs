using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using WpfWindow = System.Windows.Window;

namespace ScrabbleUIWPF
{
    public partial class TileCorrectionWindow : WpfWindow
    {
        private class TileError
        {
            public bool IsBoard { get; set; }
            public int Row { get; set; }
            public int Col { get; set; }
            public int RackIndex { get; set; }
            public string? CorrectedValue { get; set; }
            public Mat? TileImage { get; set; }
        }

        private readonly char[,] _boardState;
        private readonly char[] _rackState;
        private readonly List<TileError> _errors = new List<TileError>();
        private readonly Dictionary<(int row, int col), Mat>? _boardTileImages;
        private readonly Dictionary<int, Mat>? _rackTileImages;
        private int _currentErrorIndex = 0;

        public TileCorrectionWindow(
            char[,] boardState,
            char[] rackState,
            Dictionary<(int row, int col), Mat>? boardTileImages = null,
            Dictionary<int, Mat>? rackTileImages = null)
        {
            InitializeComponent();

            _boardState = boardState;
            _rackState = rackState;
            _boardTileImages = boardTileImages;
            _rackTileImages = rackTileImages;

            // Build list of errors
            foreach (var item in boardTileImages)
            {
                _errors.Add(new TileError
                {
                    IsBoard = true,
                    Row = item.Key.row,
                    Col = item.Key.col,
                    CorrectedValue = null,
                    TileImage = _boardTileImages?.ContainsKey((item.Key.row, item.Key.col)) == true 
                        ? _boardTileImages[(item.Key.row, item.Key.col)] 
                        : null
                });
            }

            foreach (var index in rackTileImages)
            {
                _errors.Add(new TileError
                {
                    IsBoard = false,
                    RackIndex = index.Key,
                    CorrectedValue = null,
                    TileImage = _rackTileImages?.ContainsKey(index.Key) == true 
                        ? index.Value
                        : null
                });
            }

            if (_errors.Count == 0)
            {
                // No errors to correct
                DialogResult = true;
                Close();
                return;
            }

            LoadCurrentError();
            CorrectionTextBox.Focus();
        }

        private void LoadCurrentError()
        {
            if (_currentErrorIndex >= _errors.Count)
            {
                ShowFinishState();
                return;
            }

            var error = _errors[_currentErrorIndex];

            // Update progress
            ProgressLabel.Text = $"Correcting tile {_currentErrorIndex + 1} of {_errors.Count}";

            // Update location label
            if (error.IsBoard)
            {
                TileLocationLabel.Text = $"Board Position: Row {error.Row + 1}, Column {error.Col + 1}";
            }
            else
            {
                TileLocationLabel.Text = $"Rack Slot: Position {error.RackIndex + 1}";
            }

            // Display tile image
            if (error.TileImage != null)
            {
                var bitmapSource = MatToBitmapSource(error.TileImage);
                if (bitmapSource != null)
                {
                    TileImagePreview.Source = bitmapSource;
                }
                else
                {
                    TileImagePreview.Source = null;
                }
            }
            else
            {
                TileImagePreview.Source = null;
            }

            // Set current value
            CorrectionTextBox.Text = error.CorrectedValue ?? "";
            WildcardCheckBox.IsChecked = error.CorrectedValue == "*";

            // Update buttons
            PreviousButton.IsEnabled = _currentErrorIndex > 0;
            UpdateNextButton();

            // Update corrections list
            UpdateCorrectionsList();

            CorrectionTextBox.Focus();
            CorrectionTextBox.SelectAll();
        }

        private void ShowFinishState()
        {
            ProgressLabel.Text = "All tiles corrected!";
            TileLocationLabel.Text = "Review your corrections below";
            CorrectionTextBox.IsEnabled = false;
            WildcardCheckBox.IsEnabled = false;

            PreviousButton.IsEnabled = _errors.Count > 0;
            NextButton.Visibility = Visibility.Collapsed;
            SkipButton.Visibility = Visibility.Collapsed;
            FinishButton.Visibility = Visibility.Visible;

            UpdateCorrectionsList();
        }

        private void UpdateNextButton()
        {
            var currentError = _errors[_currentErrorIndex];
            NextButton.IsEnabled = !string.IsNullOrWhiteSpace(currentError.CorrectedValue);
        }

        private void UpdateCorrectionsList()
        {
            var sb = new StringBuilder();
            
            int boardCorrections = 0;
            int rackCorrections = 0;

            foreach (var error in _errors)
            {
                if (!string.IsNullOrWhiteSpace(error.CorrectedValue))
                {
                    if (error.IsBoard)
                    {
                        sb.AppendLine($"Board ({error.Row + 1},{error.Col + 1}): {error.CorrectedValue}");
                        boardCorrections++;
                    }
                    else
                    {
                        sb.AppendLine($"Rack Slot {error.RackIndex + 1}: {error.CorrectedValue}");
                        rackCorrections++;
                    }
                }
            }

            if (sb.Length == 0)
            {
                CorrectionsListTextBlock.Text = "No corrections made yet.";
            }
            else
            {
                CorrectionsListTextBlock.Text = $"({boardCorrections} board, {rackCorrections} rack)\n" + sb.ToString();
            }
        }

        private void CorrectionTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_currentErrorIndex < _errors.Count)
            {
                var error = _errors[_currentErrorIndex];
                
                // Support wildcards
                if (CorrectionTextBox.Text == "?" || CorrectionTextBox.Text == "_")
                {
                    CorrectionTextBox.Text = "*";
                    WildcardCheckBox.IsChecked = true;
                }

                error.CorrectedValue = string.IsNullOrWhiteSpace(CorrectionTextBox.Text) 
                    ? null 
                    : CorrectionTextBox.Text.ToUpper();

                UpdateNextButton();
            }
        }

        private void CorrectionTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && NextButton.IsEnabled)
            {
                NextButton_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                SkipButton_Click(sender, e);
            }
        }

        private void WildcardCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (WildcardCheckBox.IsChecked == true)
            {
                CorrectionTextBox.Text = "*";
            }
            else if (CorrectionTextBox.Text == "*")
            {
                CorrectionTextBox.Text = "";
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentErrorIndex > 0)
            {
                _currentErrorIndex--;
                LoadCurrentError();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            _currentErrorIndex++;
            LoadCurrentError();
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear current correction and move to next
            if (_currentErrorIndex < _errors.Count)
            {
                _errors[_currentErrorIndex].CorrectedValue = null;
            }
            _currentErrorIndex++;
            LoadCurrentError();
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            // Verify all tiles have been corrected
            var uncorrectedCount = _errors.Count(e => string.IsNullOrWhiteSpace(e.CorrectedValue));
            
            if (uncorrectedCount > 0)
            {
                var result = MessageBox.Show(
                    $"{uncorrectedCount} tile(s) were skipped and will remain marked with '*'.\n\nDo you want to continue?",
                    "Uncorrected Tiles",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    _currentErrorIndex = _errors.FindIndex(e => string.IsNullOrWhiteSpace(e.CorrectedValue));
                    if (_currentErrorIndex < 0) _currentErrorIndex = 0;
                    LoadCurrentError();
                    return;
                }
            }

            // Apply corrections
            ApplyCorrections();

            DialogResult = true;
            Close();
        }

        private void ApplyCorrections()
        {
            foreach (var error in _errors)
            {
                if (!string.IsNullOrWhiteSpace(error.CorrectedValue))
                {
                    if (error.IsBoard)
                    {
                        _boardState[error.Row, error.Col] = error.CorrectedValue[0];
                    }
                    else
                    {
                        _rackState[error.RackIndex] = error.CorrectedValue[0];
                    }
                }
            }
        }

        public char[,] GetCorrectedBoardState() => _boardState;
        public char[] GetCorrectedRackState() => _rackState;

        private BitmapSource? MatToBitmapSource(Mat mat)
        {
            if (mat == null || mat.Empty())
                return null;

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    Cv2.ImEncode(".png", mat, out var buffer);
                    memoryStream.Write(buffer, 0, buffer.Length);
                    memoryStream.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
