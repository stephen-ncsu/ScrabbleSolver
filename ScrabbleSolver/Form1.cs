using IronOcr;
using IronSoftware.Drawing;
using OpenCvSharp;
using SixLabors.ImageSharp.ColorSpaces;
using System.Drawing.Printing;
using System.Text;

namespace ScrabbleSolver
{
    public partial class Form1 : Form
    {
        char[,] _boardState = new char[15, 15];
        List<Move> _moves = new List<Move>();

        int _errorCount = 0;


        string _fileName = @"D:\Users\steph\Downloads\scrabble_board.png";

        public Form1()
        {
            IronOcr.License.LicenseKey = "IRONSUITE.UOPITT1.GMAIL.COM.4621-25E37A5567-BYUH7GI3BXY25WGR-NMHDJVUDHSD4-PNXR7RRD5ZKN-DBQOJRG2YGPC-FQGWGZDNDFUW-AKSBDAQ35NZM-SP7NSC-TZFRWH2KXV2QUA-DEPLOYMENT.TRIAL-HI4GH5.TRIAL.EXPIRES.13.MAR.2026";
            InitializeComponent();
            threshold1TextBox.Text = "239";
            threshold2TextBox.Text = "300";
        }

        private void OnButtonClick(object sender, EventArgs e)
        {
            var imagePrep = new ImagePrep();

            Cv2.DestroyAllWindows();
            _errorCount = 0;
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
            var solver = new Solver(_boardState);
            _moves = solver.Solve();

            var scorer = new Scorer();
            scorer.GetScoreForMove(_moves.FirstOrDefault());

            DisplayScrabbleBoard(_moves.FirstOrDefault().GetBoardState(), movesTextBox);
        }

        private void showMove_Click(object sender, EventArgs e)
        {
            try
            {
                DisplayScrabbleBoard(_moves[Convert.ToInt32(moveId.Text)].GetBoardState(), movesTextBox);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Invalid move index. Please enter a valid number corresponding to a move.");
            }
        }
    }
}
