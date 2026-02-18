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
        char[,] boardState = new char[15, 15];

        const int CC_STAT_LEFT = 0;
        const int CC_STAT_TOP = 1;
        const int CC_STAT_WIDTH = 2;
        const int CC_STAT_HEIGHT = 3;
        const int CC_STAT_AREA = 4;
        int errorCount = 0;
        Scalar[] colors = new Scalar[5];
        List<Mat> errorImages = new List<Mat>();

        public Form1()
        {
            colors[0] = Scalar.Red;
            colors[1] = Scalar.Green;
            colors[2] = Scalar.Blue;
            colors[3] = Scalar.Yellow;
            colors[4] = Scalar.Purple;
            IronOcr.License.LicenseKey = "IRONSUITE.UOPITT1.GMAIL.COM.4621-25E37A5567-BYUH7GI3BXY25WGR-NMHDJVUDHSD4-PNXR7RRD5ZKN-DBQOJRG2YGPC-FQGWGZDNDFUW-AKSBDAQ35NZM-SP7NSC-TZFRWH2KXV2QUA-DEPLOYMENT.TRIAL-HI4GH5.TRIAL.EXPIRES.13.MAR.2026";
            InitializeComponent();
            threshold1TextBox.Text = "239";
            threshold2TextBox.Text = "300";
        }

        private void OnButtonClick(object sender, EventArgs e)
        {
            // Clear previous error images and close windows
            foreach (var img in errorImages) img.Dispose();
            errorImages.Clear();
            Cv2.DestroyAllWindows();

            errorCount = 0;
            string imagePath = @"D:\Users\steph\Downloads\scrabble_board.png";

            using (Mat inputImage = LoadAndValidateImage(imagePath))
            {
                if (inputImage == null) return;

                var boardContour = DetectBoardContour(inputImage);
                if (boardContour == null)
                {
                    MessageBox.Show("Could not detect the Scrabble board in the image.");
                    return;
                }

                using (Mat processedBoard = ExtractAndProcessBoard(inputImage, boardContour))
                {
                    ProcessBoardCells(processedBoard);
                }
            }

            DisplayScrabbleBoard(boardState, output);
            ShowErrorMontage();
        }

        private Mat LoadAndValidateImage(string imagePath)
        {
            Mat inputImage = Cv2.ImRead(imagePath, ImreadModes.Color);

            if (inputImage.Empty())
            {
                MessageBox.Show("Could not open or find the image! Check the path: " + imagePath);
                return null;
            }

            return inputImage;
        }

        private dynamic DetectBoardContour(Mat inputImage)
        {
            using (Mat grayImage = new Mat())
            using (Mat thresholded = new Mat())
            {
                // Convert to grayscale and threshold
                Cv2.CvtColor(inputImage, grayImage, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(grayImage, thresholded,
                    Convert.ToInt32(threshold1TextBox.Text),
                    Convert.ToInt32(threshold2TextBox.Text),
                    ThresholdTypes.BinaryInv);

                if (thresholded.Empty())
                    throw new Exception("Thresholding failed to produce an image.");

                return FindLargestBoardContour(thresholded, inputImage);
            }
        }

        private dynamic FindLargestBoardContour(Mat thresholded, Mat inputImage)
        {
            // Apply morphological operations to clean up noise
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5)))
            {
                Cv2.MorphologyEx(thresholded, thresholded, MorphTypes.Open, kernel);
            }

            // Find contours
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(thresholded, out contours, out hierarchy,
                RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            // Find the largest contour that meets our criteria
            return contours
                .Select(c => new { Contour = c, Rect = Cv2.BoundingRect(c) })
                .Where(x => x.Rect.Width > inputImage.Width * 0.5)
                .OrderByDescending(x => x.Rect.Width * x.Rect.Height)
                .FirstOrDefault();
        }

        private Mat ExtractAndProcessBoard(Mat inputImage, dynamic boardContour)
        {
            Rect boardRect = CalculateBoardRectangle(inputImage, boardContour.Rect);

            using (Mat boardOnly = new Mat(inputImage, boardRect))
            using (Mat resizedBoard = new Mat())
            {
                //Cv2.Resize(boardOnly, resizedBoard, new OpenCvSharp.Size(750, 750));
                return CropToTightBoard(boardOnly);
            }
        }

        private Rect CalculateBoardRectangle(Mat inputImage, Rect tileArea)
        {
            int boardWidth = inputImage.Width;
            int boardHeight = boardWidth; // Square board

            int centerY = tileArea.Y + (tileArea.Height / 2);
            int startY = Math.Max(0, centerY - (boardHeight / 2));

            // Ensure the board rectangle stays within image bounds
            if (startY + boardHeight > inputImage.Height)
            {
                boardHeight = inputImage.Height - startY;
            }

            return new Rect(0, startY, boardWidth, boardHeight);
        }

        private Mat CropToTightBoard(Mat resizedBoard)
        {
            using (Mat grayBoard = new Mat())
            using (Mat borderMask = new Mat())
            {
                Cv2.CvtColor(resizedBoard, grayBoard, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(grayBoard, borderMask, 240, 255, ThresholdTypes.BinaryInv);

                Rect boardArea = Cv2.BoundingRect(borderMask);

                using (Mat tightBoard = new Mat(resizedBoard, boardArea))
                {
                    Mat finalBoard = new Mat();
                    Cv2.Resize(tightBoard, finalBoard, new OpenCvSharp.Size(1215, 1215));
                    return finalBoard;
                }
            }
        }

        private void ProcessBoardCells(Mat processedBoard)
        {
            int cellSize = 81; // 720 / 15
            int cellPadding = 4;


            VisualGridCalibration(processedBoard, cellSize);

            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    ProcessSingleCell(processedBoard, row, col, cellSize);
                }
            }
        }

        private void ProcessSingleCell(Mat processedBoard, int row, int col, int cellSize)
        {
            Rect cellRect = new Rect(col * cellSize, row * cellSize, cellSize, cellSize);

            using (Mat cell = new Mat(processedBoard, cellRect))
            {
                if (IsTilePresent(cell))
                {
                    string detected = RunOCR(cell, row, col);
                    boardState[row, col] = string.IsNullOrEmpty(detected) ? '?' : detected[0];
                }
                else
                {
                    boardState[row, col] = ' '; // Empty square
                }
            }
        }

        void VisualGridCalibration(Mat boardMat, int cellSize)
        {
            Mat calibrationView = boardMat.Clone();
            int rows = 15;
            int cols = 15;

            //List<OpenCvSharp.Scalar> colors = new List<OpenCvSharp.Scalar> { Scalar.Red, Scalar.Green, Scalar.Blue, Scalar.Yellow, Scalar.Purple };
            //int colorCount = 0;
            //for (int i=45; i<50; i++)
            //{
            //    if (i % 2 == 0)
            //    {
            //        var leftPoint = new OpenCvSharp.Point(0, i);
            //        var rightPoint = new OpenCvSharp.Point(720, i);
            //        Cv2.Line(calibrationView, leftPoint, rightPoint, colors[colorCount], 1);
            //        colorCount++;
            //        if(colorCount > 4)
            //        {
            //            colorCount = 0;
            //        }
            //    }
            //}

            //for (int y = 0; y <= rows; y++)
            //{
            //    var bottomOfCell = y * cellSize + (y - 1) * cellPadding;
            //    var leftPoint = new OpenCvSharp.Point(0, bottomOfCell);
            //    var rightPoint = new OpenCvSharp.Point(cols * cellSize + ((cols - 1) * cellPadding), bottomOfCell);
            //    // Draw Horizontal Lines
            //    Cv2.Line(calibrationView, leftPoint, rightPoint, Scalar.Red, 1);


            //    var topOfNextRow = y * cellSize + (y) * cellPadding;
            //    var leftPointOfNextRow = new OpenCvSharp.Point(0, topOfNextRow);
            //    var rightPointOfNextRow = new OpenCvSharp.Point(cols * cellSize + ((cols - 1) * cellPadding), topOfNextRow);

            //    Cv2.Line(calibrationView, leftPointOfNextRow, rightPointOfNextRow, Scalar.Blue, 1);
            //}

            //for (int x = 0; x <= cols; x++)
            //{
            //    var leftOfCell = x * cellSize + (x - 1) * cellPadding;
            //    var topPoint = new OpenCvSharp.Point(leftOfCell, 0);
            //    var bottomPoint = new OpenCvSharp.Point(leftOfCell, rows * cellSize + ((rows - 1) * cellPadding));
            //    // Draw Vertical Lines
            //    Cv2.Line(calibrationView, topPoint, bottomPoint, Scalar.Green, 1);


            //    var leftOfNextCell = x * cellSize + (x) * cellPadding;
            //    var topOfNextRow = new OpenCvSharp.Point(leftOfNextCell, 0);
            //    var bottomOfNextRow = new OpenCvSharp.Point(leftOfNextCell, rows * cellSize + ((rows - 1) * cellPadding));

            //    Cv2.Line(calibrationView, topOfNextRow, bottomOfNextRow, Scalar.Purple, 1);
            //}

            //for (int x = 0; x <= cols; x++)
            //{
            //    // Draw Vertical Lines
            //    var topPoint = new OpenCvSharp.Point(x * cellSize, 0);
            //    var bottomPoint = new OpenCvSharp.Point(x * cellSize, rows * cellSize);
            //    Cv2.Line(calibrationView, topPoint, bottomPoint, Scalar.Red, 1);
            //}

            for (int y = 0; y <= rows; y++)
            {
                // Draw Horizontal Lines
                Cv2.Line(calibrationView, new OpenCvSharp.Point(0, y * cellSize), new OpenCvSharp.Point(cols * cellSize, y * cellSize), Scalar.Red, 1);
            }

            for (int x = 0; x <= cols; x++)
            {
                // Draw Vertical Lines
                Cv2.Line(calibrationView, new OpenCvSharp.Point(x * cellSize, 0), new OpenCvSharp.Point(x * cellSize, rows * cellSize), Scalar.Red, 1);
            }

            Cv2.Resize(calibrationView, calibrationView, new OpenCvSharp.Size(1200, 1200));
            Cv2.ImShow("Calibration - Check if Red Grid matches Tiles", calibrationView);
        }

        bool IsTilePresent(Mat cell)
        {
            using (Mat hsvCell = new Mat())
            {
                Cv2.CvtColor(cell, hsvCell, ColorConversionCodes.BGR2HSV);
                Scalar mean = Cv2.Mean(hsvCell);

                double hue = mean[0];        // The color type
                double saturation = mean[1]; // How "colorful" it is
                double value = mean[2];      // How "bright" it is

                // DEBUG: Uncomment to see values in your Output window
                // System.Diagnostics.Debug.WriteLine($"S:{saturation} V:{value}");

                // UPDATED THRESHOLD:
                // Empty squares in your image are very pale (Saturation < 20).
                // Tiles have a distinct color (Saturation > 40).
                // Blue tiles in that app usually have a 'Value' between 100 and 220.
                return (saturation > 35 && value < 230);
            }
        }

        string RunOCR(Mat cell, int row, int column)
        {            
            using (Mat thresh = PreprocessCell(cell))
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            {
                // 2. ISOLATE THE BOTTOM-LEFT CHARACTER
                int nLabels = Cv2.ConnectedComponentsWithStats(thresh, labels, stats, centroids);

                // First, check if this looks like an 'I' character
                string possibleI = CheckForICharacter(thresh, labels, stats, centroids, nLabels);
                if (!string.IsNullOrEmpty(possibleI))
                {
                    return possibleI;
                }

                int bestLabel = FindBestLetterBlob(thresh, nLabels, stats, centroids);

                // 3. Create a clean mask with ONLY the isolated letter
                using (Mat cleanTile = CreateLetterMask(thresh, labels, bestLabel))
                {
                    // INVERT IMAGE: Tesseract needs Black Text on White Background.
                    // cleanTile is White Text on Black Background.
                    // We must invert the center content before adding the white border.
                    Cv2.BitwiseNot(cleanTile, cleanTile);

                    using (Mat final = PrepareImageForOcr(cleanTile))
                    {
                        // 5. Execute OCR
                        string text = ExecuteOcr(final);


                        if (string.IsNullOrEmpty(text))
                        {
                            errorCount++;
                            VisualizeDebugInfo(final, stats, nLabels, text, row, column);
                        }

                        return string.IsNullOrEmpty(text) ? "?" : text.Substring(0, 1);
                    }
                }
            }
        }

        private Mat FillInteriorHoles(Mat mask)
        {
            Mat result = mask.Clone();

            // Create a temporary image that's 2 pixels larger in each dimension
            using (Mat temp = Mat.Zeros(mask.Rows + 2, mask.Cols + 2, MatType.CV_8UC1))
            {
                // Copy the original mask to the center of the temporary image
                using (Mat roi = new Mat(temp, new Rect(1, 1, mask.Cols, mask.Rows)))
                {
                    mask.CopyTo(roi);
                }

                // Flood fill from the border - this fills all the background connected to edges
                Cv2.FloodFill(temp, new OpenCvSharp.Point(0, 0), new Scalar(255));

                // Extract the result (excluding the 1-pixel border we added)
                using (Mat borderFilled = new Mat(temp, new Rect(1, 1, mask.Cols, mask.Rows)))
                {
                    // Invert the flood-filled result to get the holes
                    Mat holes = new Mat();
                    Cv2.BitwiseNot(borderFilled, holes);

                    // Combine the original mask with the filled holes
                    Cv2.BitwiseOr(mask, holes, result);

                    holes.Dispose();
                }
            }

            return result;
        }

        private string CheckForICharacter(Mat thresh, Mat labels, Mat stats, Mat centroids, int nLabels)
        {
            // Look for tall, narrow blobs that could be 'I'
            for (int i = 1; i < nLabels; i++)
            {
                int width = stats.At<int>(i, CC_STAT_WIDTH);
                int height = stats.At<int>(i, CC_STAT_HEIGHT);
                int area = stats.At<int>(i, CC_STAT_AREA);
                double blobCenterX = centroids.At<double>(i, 0);
                double blobCenterY = centroids.At<double>(i, 1);

                // Check if blob is in the target area
                int targetMinX = 0;
                int targetMaxX = (int)(thresh.Width * 0.70);
                int targetMinY = (int)(thresh.Height * 0.30);
                int targetMaxY = thresh.Height;

                if (blobCenterX >= targetMinX && blobCenterX <= targetMaxX &&
                    blobCenterY >= targetMinY && blobCenterY <= targetMaxY)
                {

                    double aspectRatio = (double)width / height;

                    // 'I' characteristics based on BLOB dimensions, not full image:
                    // - Very narrow (low aspect ratio)
                    // - Tall relative to the available vertical space in the cell
                    // - Reasonable area (not just noise)
                    if (aspectRatio < 0.5 &&                    // Very narrow
                        height > 10 &&                          // Minimum height in pixels
                        height > (thresh.Height * 0.4) &&       // At least 40% of cell height
                        area > 8 &&                             // Minimum area to avoid noise
                        width >= 2 &&                           // Must be at least 2 pixels wide
                        width <= 11)                             // But not too wide
                    {
                        return "I";
                    }
                }
            }
            return null;
        }

        private Mat PreprocessCell(Mat cell)
        {
            Rect roi = new Rect(8, 15, 55, 66);
            Mat thresh = new Mat();

            using (Mat croppedTop = new Mat(cell, roi))
            using (Mat gray = new Mat())
            {
                Cv2.CvtColor(croppedTop, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(gray, thresh, 120, 255, ThresholdTypes.BinaryInv);

                Cv2.BitwiseNot(thresh, thresh);

                // Use smaller, more conservative morphological operations
                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 2)))
                {
                    // Gentler closing that won't destroy thin vertical lines
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
                }
            }
            return thresh;
        }

        private int FindBestLetterBlob(Mat thresh, int nLabels, Mat stats, Mat centroids)
        {
            int bestLabel = -1;
            double maxArea = 0;

            int targetMinX = 0;
            int targetMaxX = (int)(thresh.Width * 0.70);
            int targetMinY = (int)(thresh.Height * 0.30);
            int targetMaxY = thresh.Height;

            for (int i = 1; i < nLabels; i++)
            {
                int area = stats.At<int>(i, CC_STAT_AREA);
                double blobCenterX = centroids.At<double>(i, 0);
                double blobCenterY = centroids.At<double>(i, 1);

                if (blobCenterX > targetMinX && blobCenterX < targetMaxX &&
                    blobCenterY > targetMinY && blobCenterY < targetMaxY)
                {
                    if (area > maxArea)
                    {
                        maxArea = area;
                        bestLabel = i;
                    }
                }
            }
            return bestLabel;
        }

        private Mat CreateLetterMask(Mat thresh, Mat labels, int bestLabel)
        {
            Mat cleanTile = Mat.Zeros(thresh.Size(), MatType.CV_8UC1);

            if (bestLabel != -1)
            {
                var indexer = labels.GetGenericIndexer<int>();
                var cleanIndexer = cleanTile.GetGenericIndexer<byte>();

                // First, add the main blob
                for (int y = 0; y < labels.Height; y++)
                {
                    for (int x = 0; x < labels.Width; x++)
                    {
                        if (indexer[y, x] == bestLabel)
                        {
                            cleanIndexer[y, x] = 255;
                        }
                    }
                }

                // Now fill any holes inside the letter using flood fill
                using (Mat filledMask = FillInteriorHoles(cleanTile))
                {
                    filledMask.CopyTo(cleanTile);
                }
            }

            return cleanTile;
        }

        private Mat PrepareImageForOcr(Mat cleanTile)
        {
            Mat final = new Mat();
            Cv2.CopyMakeBorder(cleanTile, final, 40, 40, 40, 40, BorderTypes.Constant, Scalar.White);
            //Cv2.Resize(final, final, new OpenCvSharp.Size(400, 400));
            
            return final;
        }

        private string ExecuteOcr(Mat final)
        {
            var ocr = new IronTesseract();
            ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.SingleChar;
            ocr.Configuration.WhiteListCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            using (var input = new IronSoftware.Drawing.AnyBitmap(final.ToMemoryStream()))
            {
                var result = ocr.Read(input);
                return result?.Text?.Trim().ToUpper();
            }
        }

        private void VisualizeDebugInfo(Mat thresh, Mat stats, int nLabels, string text, int row, int column)
        {
            var windowName = String.IsNullOrEmpty(text) ? errorCount.ToString() : text;

            using (Mat visualDebug = new Mat())
            using (Mat paddedThresh = new Mat())
            {
                int margin = 30;
                Cv2.CopyMakeBorder(thresh, paddedThresh, margin, margin, margin, margin, BorderTypes.Constant, Scalar.White);
                Cv2.CvtColor(paddedThresh, visualDebug, ColorConversionCodes.GRAY2BGR);

                for (int i = 1; i < nLabels; i++)
                {
                    int x = stats.At<int>(i, CC_STAT_LEFT) + margin;
                    int y = stats.At<int>(i, CC_STAT_TOP) + margin;
                    int w = stats.At<int>(i, CC_STAT_WIDTH);
                    int h = stats.At<int>(i, CC_STAT_HEIGHT);

                    Scalar color = colors[i % colors.Length];
                    Cv2.Rectangle(visualDebug, new Rect(x, y, w, h), color, 1);
                    Cv2.PutText(visualDebug, $"ID:{i}", new OpenCvSharp.Point(x, y - 5), HersheyFonts.HersheySimplex, 0.4, color, 1);
                }

                Cv2.PutText(visualDebug, "Row " + row + " Column " + column, new OpenCvSharp.Point(10, 40), HersheyFonts.HersheySimplex, 0.6, Scalar.Purple, 2);

                if (String.IsNullOrEmpty(text) == false)
                {
                    Cv2.PutText(visualDebug, "OCR Succeed", new OpenCvSharp.Point(10, 20), HersheyFonts.HersheySimplex, 0.6, Scalar.Green, 2);
                }
                else
                {
                    Cv2.PutText(visualDebug, "OCR Failed", new OpenCvSharp.Point(10, 20), HersheyFonts.HersheySimplex, 0.6, Scalar.Red, 2);
                }

                Cv2.Resize(visualDebug, visualDebug, new OpenCvSharp.Size(400, 400));
                errorImages.Add(visualDebug.Clone());
            }
        }

        private void ShowErrorMontage()
        {
            if (errorImages.Count == 0) return;

            int imgWidth = 400;
            int imgHeight = 400;
            int count = errorImages.Count;

            // Calculate grid size (approx square)
            int cols = (int)Math.Ceiling(Math.Sqrt(count));
            int rows = (int)Math.Ceiling((double)count / cols);

            using (Mat montage = new Mat(rows * imgHeight, cols * imgWidth, MatType.CV_8UC3, Scalar.Black))
            {
                for (int i = 0; i < count; i++)
                {
                    int r = i / cols;
                    int c = i % cols;

                    Rect roi = new Rect(c * imgWidth, r * imgHeight, imgWidth, imgHeight);
                    errorImages[i].CopyTo(new Mat(montage, roi));
                }

                Cv2.ImShow("All Errors", montage);
            }
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

            sb.AppendLine("Error Count : " + errorCount); // Bottom border

            // 3. Output to TextBox (Ensure TextBox Font is set to 'Courier New' or 'Consolas')
            outputTextBox.Text = sb.ToString();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
