using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScrabbleUIWPF
{
    public partial class ScrabbleBoardControl : UserControl
    {
        private TextBox[,] _tileTextBoxes = new TextBox[15, 15];
        private static readonly Dictionary<string, string> _specialSquares = new Dictionary<string, string>
        {
            // Triple Word Score (Red) - Updated from scoringBoard array
            {"0,3", "TW"}, {"0,11", "TW"},
            {"3,0", "TW"}, {"3,14", "TW"},
            {"11,0", "TW"}, {"11,14", "TW"},
            {"14,3", "TW"}, {"14,11", "TW"},

            // Double Word Score (Pink) - Updated from scoringBoard array
            {"1,1", "DW"}, {"1,13", "DW"},
            {"3,7", "DW"}, {"7,3", "DW"}, {"7,11", "DW"}, {"11,7", "DW"},
            {"13,1", "DW"}, {"13,13", "DW"},

            // Triple Letter Score (Blue) - Updated from scoringBoard array
            {"0,0", "TL"}, {"0,14", "TL"},
            {"1,6", "TL"}, {"1,8", "TL"},
            {"4,5", "TL"}, {"4,9", "TL"},
            {"5,4", "TL"}, {"5,10", "TL"},
            {"6,1", "TL"}, {"6,13", "TL"},
            {"8,1", "TL"}, {"8,13", "TL"},
            {"9,4", "TL"}, {"9,10", "TL"},
            {"10,5", "TL"}, {"10,9", "TL"},
            {"13,6", "TL"}, {"13,8", "TL"},
            {"14,0", "TL"}, {"14,14", "TL"},

            // Double Letter Score (Light Blue) - Updated from scoringBoard array
            {"0,7", "DL"}, {"2,4", "DL"}, {"2,10", "DL"},
            {"3,3", "DL"}, {"3,11", "DL"},
            {"4,2", "DL"}, {"4,12", "DL"},
            {"5,7", "DL"}, {"7,0", "DL"}, {"7,5", "DL"}, {"7,9", "DL"}, {"7,14", "DL"},
            {"9,7", "DL"}, {"10,2", "DL"}, {"10,12", "DL"},
            {"11,3", "DL"}, {"11,11", "DL"},
            {"12,4", "DL"}, {"12,10", "DL"},
            {"14,7", "DL"},

            // Center (Star - treated as Double Word)
            {"7,7", "★"}
        };

        public ScrabbleBoardControl()
        {
            InitializeComponent();
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            // Clear existing grid
            BoardGrid.Children.Clear();
            BoardGrid.RowDefinitions.Clear();
            BoardGrid.ColumnDefinitions.Clear();

            // Create 16 rows and 16 columns (1 extra for headers)
            for (int i = 0; i < 16; i++)
            {
                BoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
                BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
            }

            // Add column headers (1-15)
            for (int col = 0; col < 15; col++)
            {
                var header = new TextBlock
                {
                    Text = (col + 1).ToString(),
                    Style = (Style)FindResource("HeaderLabel")
                };
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, col + 1);
                BoardGrid.Children.Add(header);
            }

            // Add row headers (1-15)
            for (int row = 0; row < 15; row++)
            {
                var header = new TextBlock
                {
                    Text = (row + 1).ToString(),
                    Style = (Style)FindResource("HeaderLabel")
                };
                Grid.SetRow(header, row + 1);
                Grid.SetColumn(header, 0);
                BoardGrid.Children.Add(header);
            }

            // Create board squares
            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    var border = CreateSquare(row, col);
                    Grid.SetRow(border, row + 1);
                    Grid.SetColumn(border, col + 1);
                    BoardGrid.Children.Add(border);
                }
            }
        }

        private Border CreateSquare(int row, int col)
        {
            var border = new Border();
            string key = $"{row},{col}";

            // Set style based on square type
            if (_specialSquares.ContainsKey(key))
            {
                string squareType = _specialSquares[key];
                switch (squareType)
                {
                    case "TW":
                        border.Style = (Style)FindResource("TripleWordSquare");
                        break;
                    case "DW":
                        border.Style = (Style)FindResource("DoubleWordSquare");
                        break;
                    case "TL":
                        border.Style = (Style)FindResource("TripleLetterSquare");
                        break;
                    case "DL":
                        border.Style = (Style)FindResource("DoubleLetterSquare");
                        break;
                    case "★":
                        border.Style = (Style)FindResource("CenterSquare");
                        break;
                }

                // Add label for empty special squares
                var grid = new Grid();
                
                // Background label
                var label = new TextBlock
                {
                    Text = squareType,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };
                
                // Tile TextBox
                var textBox = new TextBox
                {
                    Style = (Style)FindResource("EmptyTileTextBox"),
                    Tag = $"{row},{col}"
                };
                textBox.TextChanged += TileTextBox_TextChanged;
                
                _tileTextBoxes[row, col] = textBox;
                
                grid.Children.Add(label);
                grid.Children.Add(textBox);
                border.Child = grid;
            }
            else
            {
                border.Style = (Style)FindResource("NormalSquare");
                
                var textBox = new TextBox
                {
                    Style = (Style)FindResource("EmptyTileTextBox"),
                    Tag = $"{row},{col}"
                };
                textBox.TextChanged += TileTextBox_TextChanged;
                
                _tileTextBoxes[row, col] = textBox;
                border.Child = textBox;
            }

            return border;
        }

        private void TileTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;

            // Support wildcard characters
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (textBox.Text == "?" || textBox.Text == "_")
                {
                    textBox.Text = "*";
                }

                textBox.Style = (Style)FindResource("TileTextBox");
            }
            else
            {
                textBox.Style = (Style)FindResource("EmptyTileTextBox");
            }
        }

        public void SetBoardState(char[,] boardState)
        {
            SetBoardState(boardState, null);
        }

        public void SetBoardState(char[,] boardState, List<(int row, int col)>? highlightedPositions)
        {
            if (boardState.GetLength(0) != 15 || boardState.GetLength(1) != 15)
                return;

            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    char tile = boardState[row, col];
                    var textBox = _tileTextBoxes[row, col];

                    if (tile != ' ' && tile != '\0')
                    {
                        textBox.Text = tile.ToString();

                        // Check if this position should be highlighted as unrecognized
                        if (tile == '*' || (highlightedPositions != null && highlightedPositions.Contains((row, col))))
                        {
                            textBox.Style = (Style)FindResource("UnrecognizedTileTextBox");
                        }
                        else
                        {
                            textBox.Style = (Style)FindResource("TileTextBox");
                        }
                    }
                    else
                    {
                        textBox.Text = string.Empty;
                        textBox.Style = (Style)FindResource("EmptyTileTextBox");
                    }
                }
            }
        }

        public char[,] GetBoardState()
        {
            char[,] boardState = new char[15, 15];

            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    string text = _tileTextBoxes[row, col].Text;
                    boardState[row, col] = string.IsNullOrWhiteSpace(text) ? ' ' : text[0];
                }
            }

            return boardState;
        }

        public void ClearBoard()
        {
            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    _tileTextBoxes[row, col].Text = string.Empty;
                }
            }
        }
    }
}
