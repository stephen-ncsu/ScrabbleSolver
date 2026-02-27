using System.Windows;
using System.Windows.Controls;

namespace ScrabbleUIWPF
{
    public partial class RackControl : UserControl
    {
        private TextBox[] _tileTextBoxes = new TextBox[7];

        public RackControl()
        {
            InitializeComponent();
            InitializeRack();
        }

        private void InitializeRack()
        {
            TilesPanel.Children.Clear();

            for (int i = 0; i < 7; i++)
            {
                var textBox = new TextBox
                {
                    Style = (Style)FindResource("TileTextBox"),
                    Tag = i
                };

                textBox.TextChanged += TileTextBox_TextChanged;
                _tileTextBoxes[i] = textBox;
                TilesPanel.Children.Add(textBox);
            }
        }

        private void TileTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // Support wildcard characters
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (textBox.Text == "?" || textBox.Text == "_")
                {
                    textBox.Text = "*";
                }
            }
        }

        public void SetRack(char[] tiles, List<int>? unrecognizedIndices = null)
        {
            if (tiles == null || tiles.Length != 7)
                return;

            for (int i = 0; i < 7; i++)
            {
                var textBox = _tileTextBoxes[i];

                if (tiles[i] != ' ' && tiles[i] != '\0')
                {
                    textBox.Text = tiles[i].ToString().ToUpper();
                }
                else
                {
                    textBox.Text = string.Empty;
                }

                // Highlight unrecognized tiles
                if (unrecognizedIndices != null && unrecognizedIndices.Contains(i))
                {
                    textBox.Style = (Style)FindResource("UnrecognizedTileTextBox");
                }
                else
                {
                    textBox.Style = (Style)FindResource("TileTextBox");
                }
            }
        }

        public char[] GetRack()
        {
            char[] rack = new char[7];

            for (int i = 0; i < 7; i++)
            {
                string? text = _tileTextBoxes[i].Text;
                rack[i] = string.IsNullOrWhiteSpace(text) ? ' ' : char.ToUpper(text[0]);
            }

            return rack;
        }

        public string GetRackString()
        {
            return new string(GetRack()).Trim();
        }

        public void ClearRack()
        {
            for (int i = 0; i < 7; i++)
            {
                _tileTextBoxes[i].Text = string.Empty;
                _tileTextBoxes[i].Style = (Style)FindResource("TileTextBox");
            }
        }
    }
}
