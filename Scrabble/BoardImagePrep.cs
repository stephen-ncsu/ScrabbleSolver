using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrabbleSolver
{
    public class BoardImagePrep
    {
        private char[,] _boardState = new char[15, 15];

        OCR _ocr = new OCR();

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
                int width = stats.At<int>(i, Constants.CC_STAT_WIDTH);
                int height = stats.At<int>(i, Constants.CC_STAT_HEIGHT);
                int area = stats.At<int>(i, Constants.CC_STAT_AREA);
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
                int area = stats.At<int>(i, Constants.CC_STAT_AREA);
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

            return final;
        }

        private Mat LoadAndValidateImage(string imagePath)
        {
            Mat inputImage = Cv2.ImRead(imagePath, ImreadModes.Color);

            if (inputImage.Empty())
            {
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
                    239,
                    400,
                    ThresholdTypes.BinaryInv);

                if (thresholded.Empty())
                    throw new Exception("Thresholding failed to produce an image.");

                return FindLargestBoardContour(thresholded, inputImage);
            }
        }

        public char[,] Run(string fileName)
        {
            _boardState = new char[15, 15]; // Reset board state for each run
            using (Mat inputImage = LoadAndValidateImage(fileName))
            {
                if (inputImage == null)
                {
                    throw new Exception("Failed to load image. Please check the file path and try again.");
                }

                var boardContour = DetectBoardContour(inputImage);

                if (boardContour == null)
                {                    
                    throw new Exception(fileName + " - Board contour not found. Adjust thresholds or check image quality.");
                }

                using (Mat processedBoard = ExtractAndProcessBoard(inputImage, boardContour))
                {
                    ProcessBoardCells(processedBoard);
                }
            }

            _ocr.ShowErrorMontage();

            return _boardState;
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
            var selectedContour = contours
                .Select(c => new { Contour = c, Rect = Cv2.BoundingRect(c) })
                .Where(x => x.Rect.Width > inputImage.Width * 0.7)
                .OrderByDescending(x => x.Rect.Width * x.Rect.Height)
                .FirstOrDefault();

            // DEBUGGING: Visualize all contours and highlight the selected one
            VisualizeContours(inputImage, contours, selectedContour);

            // Also show contour statistics in console
            Console.WriteLine($"Found {contours.Length} total contours");
            var qualifyingContours = contours
                .Select((c, index) => new { Index = index, Contour = c, Rect = Cv2.BoundingRect(c) })
                .Where(x => x.Rect.Width > inputImage.Width * 0.7)
                .OrderByDescending(x => x.Rect.Width * x.Rect.Height);

            Console.WriteLine($"Qualifying contours (width > {inputImage.Width * 0.7}):");
            foreach (var contour in qualifyingContours)
            {
                Console.WriteLine($"  Contour {contour.Index}: {contour.Rect.Width}x{contour.Rect.Height} at ({contour.Rect.X},{contour.Rect.Y})");
            }

            return selectedContour;
        }

        private void VisualizeContours(Mat inputImage, OpenCvSharp.Point[][] contours, dynamic selectedContour = null)
        {
            Mat contoursVisualization = inputImage.Clone();

            // Draw all contours in green
            for (int i = 0; i < contours.Length; i++)
            {
                Cv2.DrawContours(contoursVisualization, contours, i, Scalar.Green, 2);

                // Get bounding rectangle for each contour
                Rect boundingRect = Cv2.BoundingRect(contours[i]);

                // Draw bounding rectangle in blue
                Cv2.Rectangle(contoursVisualization, boundingRect, Scalar.Blue, 2);

                // Add contour info text
                string info = $"C{i}: {boundingRect.Width}x{boundingRect.Height}";
                Cv2.PutText(contoursVisualization, info,
                    new OpenCvSharp.Point(boundingRect.X, boundingRect.Y - 10),
                    HersheyFonts.HersheySimplex, 0.5, Scalar.Blue, 1);
            }

            // Highlight the selected contour in red if provided
            if (selectedContour != null)
            {
                // Find the index of the selected contour
                for (int i = 0; i < contours.Length; i++)
                {
                    Rect currentRect = Cv2.BoundingRect(contours[i]);
                    if (currentRect.X == selectedContour.Rect.X &&
                        currentRect.Y == selectedContour.Rect.Y &&
                        currentRect.Width == selectedContour.Rect.Width &&
                        currentRect.Height == selectedContour.Rect.Height)
                    {
                        // Draw selected contour in red with thicker line
                        Cv2.DrawContours(contoursVisualization, contours, i, Scalar.Red, 4);
                        Cv2.Rectangle(contoursVisualization, selectedContour.Rect, Scalar.Red, 4);

                        // Add "SELECTED" label
                        Cv2.PutText(contoursVisualization, "SELECTED BOARD",
                            new OpenCvSharp.Point(selectedContour.Rect.X, selectedContour.Rect.Y - 30),
                            HersheyFonts.HersheySimplex, 0.8, Scalar.Red, 2);
                        break;
                    }
                }
            }

            // Resize for better viewing if the image is too large
            if (contoursVisualization.Width > 1200)
            {
                double scale = 1200.0 / contoursVisualization.Width;
                Cv2.Resize(contoursVisualization, contoursVisualization,
                    new OpenCvSharp.Size((int)(contoursVisualization.Width * scale),
                                        (int)(contoursVisualization.Height * scale)));
            }
        }

        private Mat ExtractAndProcessBoard(Mat inputImage, dynamic boardContour)
        {
            Rect boardRect = CalculateBoardRectangle(inputImage, boardContour.Rect);

            boardRect = new Rect(0, 897, 1280, 1280);


            using (Mat boardOnly = new Mat(inputImage, boardRect))
            using (Mat resizedBoard = new Mat())
            {
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
                    _boardState[row, col] = string.IsNullOrEmpty(detected) ? '?' : detected[0];
                }
                else
                {
                    _boardState[row, col] = ' '; // Empty square
                }
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
                        string text = _ocr.ExecuteOcr(final);


                        if (string.IsNullOrEmpty(text))
                        {
                            VisualizeDebugInfo(final, stats, nLabels, text, row, column);
                        }

                        return string.IsNullOrEmpty(text) ? "?" : text.Substring(0, 1);
                    }
                }
            }
        }

        private void VisualizeDebugInfo(Mat thresh, Mat stats, int nLabels, string text, int row, int column)
        {
            var windowName = String.IsNullOrEmpty(text) ? _ocr.ErrorImages.Count.ToString() : text;

            using (Mat visualDebug = new Mat())
            using (Mat paddedThresh = new Mat())
            {
                //int margin = 40;
                //Cv2.CopyMakeBorder(thresh, paddedThresh, margin, margin, margin, margin, BorderTypes.Constant, Scalar.White);
                Cv2.CvtColor(thresh, visualDebug, ColorConversionCodes.GRAY2BGR);

                for (int i = 1; i < nLabels; i++)
                {
                    int x = stats.At<int>(i, Constants.CC_STAT_LEFT) + 40; //+ margin;
                    int y = stats.At<int>(i, Constants.CC_STAT_TOP) + 40; //+ margin;
                    int w = stats.At<int>(i, Constants.CC_STAT_WIDTH);
                    int h = stats.At<int>(i, Constants.CC_STAT_HEIGHT);

                    Scalar color = Constants.Colors[i % Constants.Colors.Length];
                    Cv2.Rectangle(visualDebug, new Rect(x, y, w, h), color, 1);
                    Cv2.PutText(visualDebug, $"ID:{i} X {x} Y {y} W {w} H {h}", new OpenCvSharp.Point(x - 5, y - 5), HersheyFonts.HersheySimplex, 0.3, color, 1);
                }


                Cv2.PutText(visualDebug, "R " + row + " C " + column, new OpenCvSharp.Point(10, 40), HersheyFonts.HersheySimplex, 0.3, Scalar.Purple, 1);


                if (String.IsNullOrEmpty(text) == false)
                {
                    Cv2.PutText(visualDebug, "S", new OpenCvSharp.Point(10, 20), HersheyFonts.HersheySimplex, 0.3, Scalar.Green, 1);
                }
                else
                {
                    Cv2.PutText(visualDebug, "F", new OpenCvSharp.Point(10, 20), HersheyFonts.HersheySimplex, 0.3, Scalar.Red, 1);
                }

                _ocr.ErrorImages.Add(visualDebug.Clone());
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

            Cv2.Resize(calibrationView, calibrationView, new OpenCvSharp.Size(1200, 1200));
            //Cv2.ImShow("Calibration - Check if Red Grid matches Tiles", calibrationView);
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
    }
}
