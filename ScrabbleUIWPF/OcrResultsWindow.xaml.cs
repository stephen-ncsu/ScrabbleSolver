using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScrabbleUIWPF
{
    public partial class OcrResultsWindow : Window
    {
        private readonly char[,] _boardState;
        private readonly char[] _rackState;
        private readonly List<(int row, int col)> _unrecognizedBoardTiles;
        private readonly List<int> _unrecognizedRackTiles;
        private readonly string _originalImagePath;

        public bool RescanRequested { get; private set; }

        public OcrResultsWindow(
            string originalImagePath,
            char[,] boardState,
            char[] rackState,
            List<(int row, int col)> unrecognizedBoardTiles,
            List<int> unrecognizedRackTiles)
        {
            InitializeComponent();

            _originalImagePath = originalImagePath;
            _boardState = boardState;
            _rackState = rackState;
            _unrecognizedBoardTiles = unrecognizedBoardTiles ?? new List<(int row, int col)>();
            _unrecognizedRackTiles = unrecognizedRackTiles ?? new List<int>();
            RescanRequested = false;

            LoadOcrResults();
        }

        private void LoadOcrResults()
        {
            // Load and display the original image
            if (!string.IsNullOrEmpty(_originalImagePath) && File.Exists(_originalImagePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_originalImagePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    OriginalImage.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            // Update status labels
            int recognizedBoardTiles = CountRecognizedBoardTiles();
            int totalBoardTiles = CountTotalBoardTiles();

            BoardStatusLabel.Text = $"{recognizedBoardTiles} of {totalBoardTiles} tiles recognized";
            BoardStatusLabel.Foreground = _unrecognizedBoardTiles.Count > 0 
                ? new SolidColorBrush(Colors.Orange) 
                : new SolidColorBrush(Colors.Green);

            int recognizedRackTiles = 7 - _unrecognizedRackTiles.Count;
            RackStatusLabel.Text = $"{recognizedRackTiles} of 7 tiles recognized";
            RackStatusLabel.Foreground = _unrecognizedRackTiles.Count > 0 
                ? new SolidColorBrush(Colors.Red) 
                : new SolidColorBrush(Colors.Green);

            // Update instructions
            if (_unrecognizedBoardTiles.Count > 0 || _unrecognizedRackTiles.Count > 0)
            {
                InstructionsLabel.Text = "⚠️ Some tiles were not recognized. Please review and correct them on the main board.";
                InstructionsLabel.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                InstructionsLabel.Text = "✓ All tiles recognized successfully!";
                InstructionsLabel.Foreground = new SolidColorBrush(Colors.Green);
            }
        }

        private int CountRecognizedBoardTiles()
        {
            int count = 0;
            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    if (_boardState[row, col] != ' ' && _boardState[row, col] != '\0' && _boardState[row, col] != '?')
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private int CountTotalBoardTiles()
        {
            int count = 0;
            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    if (_boardState[row, col] != ' ' && _boardState[row, col] != '\0')
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private void RescanButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Do you want to select a different image?",
                "Re-scan Image",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                RescanRequested = true;
                DialogResult = false;
                Close();
            }
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (_unrecognizedBoardTiles.Count > 0 || _unrecognizedRackTiles.Count > 0)
            {
                var result = MessageBox.Show(
                    "Some tiles were not recognized. You can correct them on the main board. Continue?",
                    "Unrecognized Tiles",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;
            }

            DialogResult = true;
            Close();
        }

        public char[,] GetBoardState() => _boardState;
        public char[] GetRackState() => _rackState;
        public List<(int row, int col)> GetUnrecognizedBoardTiles() => _unrecognizedBoardTiles;
        public List<int> GetUnrecognizedRackTiles() => _unrecognizedRackTiles;
    }
}
