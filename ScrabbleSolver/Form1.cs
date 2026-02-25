using IronOcr;
using IronSoftware.Drawing;
using OpenCvSharp;
using SixLabors.ImageSharp.ColorSpaces;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Text;

namespace ScrabbleSolver
{
    public partial class Form1 : Form
    {
        char[,] _boardState = new char[15, 15];
        List<Move> _moves = new List<Move>();

        int _errorCount = 0;


        string _fileName = @".\TestData\scrabble_board.png";

        public Form1()
        {
            IronOcr.License.LicenseKey = "IRONSUITE.UOPITT1.GMAIL.COM.4621-25E37A5567-BYUH7GI3BXY25WGR-NMHDJVUDHSD4-PNXR7RRD5ZKN-DBQOJRG2YGPC-FQGWGZDNDFUW-AKSBDAQ35NZM-SP7NSC-TZFRWH2KXV2QUA-DEPLOYMENT.TRIAL-HI4GH5.TRIAL.EXPIRES.13.MAR.2026";
            InitializeComponent();
            threshold1TextBox.Text = "239";
            threshold2TextBox.Text = "300";
        }

        private void OnButtonClick(object sender, EventArgs e)
        {
            var rackImagePrep= new RackImagePrep();
            var rackResult = rackImagePrep.Run(_fileName);
            rackTextBox.Text = new string(rackResult);
            var imagePrep = new BoardImagePrep();

            Cv2.DestroyAllWindows();
            //_errorCount = 0;
            _boardState = imagePrep.Run(_fileName);

            DisplayScrabbleBoard(_boardState, output);
        }

        public void DisplayScrabbleBoard(char[,] boardState, RichTextBox outputTextBox)
        {
            StringBuilder sb = new StringBuilder();

            // 1. Add Column Headers (01 to 15)
            sb.Append("    "); // Initial spacing for row labels
            for (int col = 0; col < 15; col++)
            {
                sb.Append($"{col + 1:D2} ");
            }
            sb.AppendLine();
            sb.AppendLine("   " + new string('-', 45)); // Top border

            // 2. Loop through rows
            for (int row = 0; row < 15; row++)
            {
                // Add Row Header (01 to 15)
                sb.Append($"{row + 1:D2} | ");

                for (int col = 0; col < 15; col++)
                {
                    char tile = boardState[row, col];

                    // If the square is empty, use a dot to keep the grid visible
                    if (tile == ' ' || tile == '\0')
                    {
                        sb.Append(".  ");
                    }
                    else
                    {
                        sb.Append($"{tile}  ");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("Error Count : " + _errorCount); // Bottom border

            // 3. Output to TextBox (Ensure TextBox Font is set to 'Courier New' or 'Consolas')
            outputTextBox.Text = sb.ToString();
        }

        private void BrowseClick(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _fileName = openFileDialog1.FileName;
            }
        }

        private void SolveClick(object sender, EventArgs e)
        {
            var solver = new Solver(_boardState, rackTextBox.Text);
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            _moves = solver.Solve();
            stopWatch.Stop();
            Serilog.Log.Logger.Information("Solver found {MoveCount} moves in {ElapsedMilliseconds} ms", _moves.Count, stopWatch.ElapsedMilliseconds);

            Move bestMove = null;
            var scorer = new Scorer();
            foreach (var move in _moves)
            {
                move.Score = scorer.GetScoreForMove(move);

                if (bestMove == null || move.Score > bestMove.Score)
                {
                    Serilog.Log.Information("New best move found with score {Score}", move.Score);
                    bestMove = move;
                }
            }

            DisplayScrabbleBoard(bestMove.GetBoardState(), movesTextBox);

            movesTextBox.Text += $"\nBest Move Score: {bestMove.Score}";
            movesTextBox.Text += "\nChanged Positions: " + string.Join(", ", bestMove.GetChangedPositions().Select(pos => $"{pos.Item1} at ({pos.Item2 + 1}, {pos.Item3 + 1})"));
        }

        private void showMove_Click(object sender, EventArgs e)
        {
            try
            {
                DisplayScrabbleBoard(_moves[Convert.ToInt32(moveId.Text)].GetBoardState(), movesTextBox);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid move index. Please enter a valid number corresponding to a move.");
            }
        }

        private void updateBoardButton_Click(object sender, EventArgs e)
        {
            try
            {
                var updatedText = output.Text;
                var lines = updatedText.Split(new[] { Environment.NewLine, "\r\n", "\r", "\n" }, StringSplitOptions.None);

                if (lines.Length < 19)
                {
                    MessageBox.Show("Invalid board format. Please ensure the board has the correct structure.");
                    return;
                }

                for (int row = 0; row < 15; row++)
                {
                    int lineIndex = row + 2;
                    if (lineIndex >= lines.Length)
                    {
                        MessageBox.Show($"Invalid board format at row {row + 1}.");
                        return;
                    }

                    string line = lines[lineIndex];

                    int dataStartIndex = line.IndexOf('|');
                    if (dataStartIndex == -1 || dataStartIndex + 2 >= line.Length)
                    {
                        MessageBox.Show($"Invalid board format at row {row + 1}. Could not find '|' separator.");
                        return;
                    }

                    string rowData = line.Substring(dataStartIndex + 2);

                    for (int col = 0; col < 15; col++)
                    {
                        int charIndex = col * 3;
                        if (charIndex < rowData.Length)
                        {
                            char tile = rowData[charIndex];

                            if (tile == '.')
                            {
                                _boardState[row, col] = ' ';
                            }
                            else
                            {
                                _boardState[row, col] = tile;
                            }
                        }
                        else
                        {
                            _boardState[row, col] = ' ';
                        }
                    }
                }

                MessageBox.Show("Board state updated successfully!");
                DisplayScrabbleBoard(_boardState, output);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating board state: {ex.Message}");
            }
        }
    }
}
