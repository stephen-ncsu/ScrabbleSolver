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
            // Triple Word Score (Red)
            {"0,0", "TW"}, {"0,7", "TW"}, {"0,14", "TW"},
            {"7,0", "TW"}, {"7,14", "TW"},
            {"14,0", "TW"}, {"14,7", "TW"}, {"14,14", "TW"},
            
            // Double Word Score (Pink)
            {"1,1", "DW"}, {"2,2", "DW"}, {"3,3", "DW"}, {"4,4", "DW"},
            {"1,13", "DW"}, {"2,12", "DW"}, {"3,11", "DW"}, {"4,10", "DW"},
            {"13,1", "DW"}, {"12,2", "DW"}, {"11,3", "DW"}, {"10,4", "DW"},
            {"13,13", "DW"}, {"12,12", "DW"}, {"11,11", "DW"}, {"10,10", "DW"},
            
            // Triple Letter Score (Blue)
            {"1,5", "TL"}, {"1,9", "TL"},
            {"5,1", "TL"}, {"5,5", "TL"}, {"5,9", "TL"}, {"5,13", "TL"},
            {"9,1", "TL"}, {"9,5", "TL"}, {"9,9", "TL"}, {"9,13", "TL"},
            {"13,5", "TL"}, {"13,9", "TL"},
            
            // Double Letter Score (Light Blue)
            {"0,3", "DL"}, {"0,11", "DL"},
            {"2,6", "DL"}, {"2,8", "DL"},
            {"3,0", "DL"}, {"3,7", "DL"}, {"3,14", "DL"},
            {"6,2", "DL"}, {"6,6", "DL"}, {"6,8", "DL"}, {"6,12", "DL"},
            {"7,3", "DL"}, {"7,11", "DL"},
            {"8,2", "DL"}, {"8,6", "DL"}, {"8,8", "DL"}, {"8,12", "DL"},
            {"11,0", "DL"}, {"11,7", "DL"}, {"11,14", "DL"},
            {"12,6", "DL"}, {"12,8", "DL"},
            {"14,3", "DL"}, {"14,11", "DL"},
            
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
            
            // Change style based on whether it has content
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
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

                        // Check if this position should be highlighted
                        if (highlightedPositions != null && highlightedPositions.Contains((row, col)))
                        {
                            textBox.Style = (Style)FindResource("HighlightedTileTextBox");
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
