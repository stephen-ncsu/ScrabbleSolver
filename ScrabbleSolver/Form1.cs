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
                Cv2.Resize(boardOnly, resizedBoard, new OpenCvSharp.Size(750, 750));
                return CropToTightBoard(resizedBoard);
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
                    Cv2.Resize(tightBoard, finalBoard, new OpenCvSharp.Size(720, 720));
                    return finalBoard;
                }
            }
        }

        private void ProcessBoardCells(Mat processedBoard)
        {
            const int cellSize = 48; // 720 / 15

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
                    string detected = RunOCR(cell);
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

        string RunOCR(Mat cell, bool debug = false)
        {
            if (debug)
            {
                Cv2.ImShow("Debug - Original Cell " + Guid.NewGuid(), cell);
                Cv2.WaitKey(1);
            }

            using (Mat thresh = PreprocessCell(cell))
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            {
                // 2. ISOLATE THE BOTTOM-LEFT CHARACTER
                int nLabels = Cv2.ConnectedComponentsWithStats(thresh, labels, stats, centroids);

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
                            VisualizeDebugInfo(cell, thresh, stats, nLabels, text);
                        }

                        return string.IsNullOrEmpty(text) ? "?" : text.Substring(0, 1);
                    }
                }
            }
        }

        private Mat PreprocessCell(Mat cell)
        {
            Rect roi = new Rect(5, 17, 28, 31);
            Mat thresh = new Mat();

            using (Mat croppedTop = new Mat(cell, roi))
            using (Mat gray = new Mat())
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2)))
            {
                Cv2.CvtColor(croppedTop, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(gray, thresh, 120, 255, ThresholdTypes.BinaryInv);
                Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);
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
            }
            return cleanTile;
        }

        private Mat PrepareImageForOcr(Mat cleanTile)
        {
            Mat final = new Mat();
            Cv2.CopyMakeBorder(cleanTile, final, 40, 40, 40, 40, BorderTypes.Constant, Scalar.White);
            Cv2.Resize(final, final, new OpenCvSharp.Size(400, 400));
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

        private void VisualizeDebugInfo(Mat cell, Mat thresh, Mat stats, int nLabels, string text)
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

        string RunOCR_new(Mat cell, bool debug = false)
        {
            using (Mat gray = new Mat())
            using (Mat thresh = new Mat())
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            {
                // 1. CROP & PRE-PROCESS
                // Using your specific ROI to isolate the letter area
                Rect roi = new Rect(5, 17, 28, 31);
                using (Mat croppedTop = new Mat(cell, roi))
                {
                    Cv2.CvtColor(croppedTop, gray, ColorConversionCodes.BGR2GRAY);
                }

                Cv2.Threshold(gray, thresh, 120, 255, ThresholdTypes.BinaryInv);

                // Clean noise (Opening: Erode then Dilate)
                using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2)))
                {
                    Cv2.MorphologyEx(thresh, thresh, MorphTypes.Open, kernel);
                }

                // 2. ISOLATE BLOBS
                int nLabels = Cv2.ConnectedComponentsWithStats(thresh, labels, stats, centroids);
                int bestLabel = -1;
                double maxArea = 0;

                // Target Zone (Bottom-Left logic)
                int targetMaxX = (int)(thresh.Width * 0.70);
                int targetMinY = (int)(thresh.Height * 0.30);

                for (int i = 1; i < nLabels; i++)
                {
                    double blobCenterX = centroids.At<double>(i, 0);
                    double blobCenterY = centroids.At<double>(i, 1);
                    int area = stats.At<int>(i, 4);

                    if (blobCenterX < targetMaxX && blobCenterY > targetMinY)
                    {
                        if (area > maxArea)
                        {
                            maxArea = area;
                            bestLabel = i;
                        }
                    }
                }

                if (bestLabel == -1) return "?";

                // 3. FORCE "I" BY ASPECT RATIO
                int w = stats.At<int>(bestLabel, 2);
                int h = stats.At<int>(bestLabel, 3);
                double aspectRatio = (double)w / h;
                if (aspectRatio < 0.4 && h > (thresh.Height * 0.5)) return "I";

                // 4. CREATE CLEAN MASK & FILL HOLES (The "O" Fix)
                Mat letterMask = new Mat();
                Cv2.Compare(labels, new Scalar(bestLabel), letterMask, CmpType.EQ);

                // Flood fill holes to make 'O', 'D', 'Q' solid
                using (Mat floodFilled = letterMask.Clone())
                {
                    Cv2.FloodFill(floodFilled, new OpenCvSharp.Point(0, 0), new Scalar(255));
                    Cv2.BitwiseNot(floodFilled, floodFilled); // Isolate the holes
                    Cv2.BitwiseOr(letterMask, floodFilled, letterMask); // Merge holes into letter
                }

                // 5. INVERT & PAD (Prepare for OCR)
                Mat final = new Mat();
                Cv2.BitwiseNot(letterMask, final); // Convert to Black Letter on White Background

                // Add 40px white border and resize
                Cv2.CopyMakeBorder(final, final, 40, 40, 40, 40, BorderTypes.Constant, Scalar.White);
                Cv2.Resize(final, final, new OpenCvSharp.Size(400, 400));

                // 6. EXECUTE OCR
                var ocr = new IronTesseract();
                ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.SingleChar;
                ocr.Configuration.WhiteListCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

                using (var input = new IronSoftware.Drawing.AnyBitmap(final.ToMemoryStream()))
                {
                    Cv2.ImShow(Guid.NewGuid().ToString(), final);
                    var result = ocr.Read(input);
                    string text = result?.Text?.Trim().ToUpper() ?? "";

                    if (string.IsNullOrEmpty(text))
                    {
                        errorCount++;
                        if (debug)
                        {
                            // Your existing debug visual stats logic here...
                            // Using visualDebug and drawing the rectangles
                        }
                    }

                    return string.IsNullOrEmpty(text) ? "?" : text.Substring(0, 1);
                }
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
