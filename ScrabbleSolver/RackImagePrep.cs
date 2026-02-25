using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace ScrabbleSolver
{
    public class RackImagePrep
    {
        char [] _rack = new char[7];
        OCR _ocr = new OCR();

        public char[] Run(string fileName)
        {
            _rack = new char[7]; // Reset board state for each run
            using (Mat inputImage = LoadAndValidateImage(fileName))
            {
                if (inputImage == null)
                {
                    throw new Exception("Failed to load image. Please check the file path and try again.");
                }

                using (Mat rack = ExtractAndProcessBoard(inputImage))
                {
                    ProcessRackCells(rack);
                }
            }

            _ocr.ShowErrorMontage();

            return _rack;
        }

        private void ProcessRackCells(Mat rack)
        {
            int cellSize = 170; // 720 / 15
            int cellPadding = 9;


            VisualGridCalibration(rack, cellSize, cellPadding);

            for (int column = 0; column < 7; column++)
            {
                ProcessSingleCell(rack, column, cellSize);
            }
        }

        private void ProcessSingleCell(Mat processedBoard, int col, int cellSize)
        {
            Rect cellRect = new Rect(Math.Min(col * cellSize, processedBoard.Width - cellSize), 0, cellSize, processedBoard.Height);

            using (Mat cell = new Mat(processedBoard, cellRect))
            {
                if (IsTilePresent(cell))
                {
                    string detected = RunOCR(cell, col);
                    _rack[col] = string.IsNullOrEmpty(detected) ? '*' : detected[0];
                }
                else
                {
                    _rack[col] = ' '; // Empty square
                }
            }
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

        string RunOCR(Mat cell, int column)
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
                            VisualizeDebugInfo(final, stats, nLabels, text, column);
                        }

                        return string.IsNullOrEmpty(text) ? "*" : text.Substring(0, 1);
                    }
                }
            }
        }

        private void VisualizeDebugInfo(Mat thresh, Mat stats, int nLabels, string text, int column)
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


                Cv2.PutText(visualDebug, "C " + column, new OpenCvSharp.Point(10, 40), HersheyFonts.HersheySimplex, 0.3, Scalar.Purple, 1);


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


        private Mat PrepareImageForOcr(Mat cleanTile)
        {
            Mat final = new Mat();
            Cv2.CopyMakeBorder(cleanTile, final, 40, 40, 40, 40, BorderTypes.Constant, Scalar.White);

            return final;
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
            Rect roi = new Rect(25, 50, 90, 100);
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

        void VisualGridCalibration(Mat boardMat, int cellSize, int margin)
        {
            Mat calibrationView = boardMat.Clone();
            int cols = 7;

            Cv2.Line(calibrationView, new OpenCvSharp.Point(0, 65), new OpenCvSharp.Point(cols * cellSize, 65), Scalar.Red, 1);
            Cv2.Line(calibrationView, new OpenCvSharp.Point(0, 145), new OpenCvSharp.Point(cols * cellSize, 145), Scalar.Red, 1);

            for (int x = 0; x <= cols; x++)
            {
                Cv2.Line(calibrationView, new OpenCvSharp.Point(x * cellSize, 0), new OpenCvSharp.Point(x * cellSize, cellSize), Scalar.Red, 1);
            }

            Cv2.ImShow("Calibration - Check if Red Grid matches Tiles", calibrationView);
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

        private Mat ExtractAndProcessBoard(Mat inputImage)
        {
            var boardRect = new Rect(44, 2305, 1182, 163);
            Mat rackOnly = new Mat(inputImage, boardRect);
            return rackOnly;
        }
    }
}
