using IronOcr;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScrabbleSolver
{
    public class OCR
    {
        private int _errorCount;
        public List<Mat> ErrorImages
        {
            get;
            set;
        } = new List<Mat>();

        public string ExecuteOcr(Mat final, string defaultCharacter)
        {
            // Try the standard approach first
            string result = ExecuteStandardOcr(final, "3");

            // If it failed and the image looks like O or Q, try alternative approaches
            if (string.IsNullOrEmpty(result) && CouldBeCircularLetter(final))
            {
                result = ExecuteCircularLetterOcr(final);
            }

            if (string.IsNullOrEmpty(result) && CouldBeCircularLetter(final))
            {
                result = AnalyzeShapeForOQ(final);
            }

            if (string.IsNullOrEmpty(result))
            {
                result = ExecuteStandardOcr(final, "0");
            }


            return result.Length == 1 ? result.Substring(0, 1) : defaultCharacter;
        }

        private string AnalyzeShapeForOQ(Mat image)
        {
            using (Mat gray = new Mat())
            using (Mat inverted = new Mat())
            {
                if (image.Channels() == 3)
                    Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                else
                    image.CopyTo(gray);

                Cv2.BitwiseNot(gray, inverted);
                // Find contours
                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(inverted, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                if (contours.Length > 0)
                {
                    var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();



                    // Check circularity
                    double area = Cv2.ContourArea(largestContour);
                    double perimeter = Cv2.ArcLength(largestContour, true);
                    double circularity = 4 * Math.PI * area / (perimeter * perimeter);

                    // Check for tail (Q has a tail, O doesn't)
                    var boundingRect = Cv2.BoundingRect(largestContour);
                    bool hasTail = CheckForTail(gray, boundingRect);

                    if ((circularity > 0.88 && hasTail == false) || circularity > 0.69 && hasTail == true) // Fairly circular
                    {
                        return hasTail ? "Q" : "O";
                    }
                    else
                    {
                        //Cv2.ImShow("Shape Analysis Debug", image);
                        //Cv2.WaitKey(1);
                    }
                }
            }

            return null;
        }

        private bool CheckForTail(Mat gray, Rect boundingRect)
        {
            // Look for pixels in the bottom-right area that could be a Q tail
            int tailRegionX = boundingRect.X + (int)(boundingRect.Width * 0.55);
            int tailRegionY = boundingRect.Y + (int)(boundingRect.Height * 0.7);
            int tailRegionSizeX = (int)(boundingRect.Width * 0.45);
            int tailRegionSizeY = (int)(boundingRect.Height * 0.3);

            Rect tailRegion = new Rect(
                Math.Max(0, tailRegionX),
                Math.Max(0, tailRegionY),
                tailRegionSizeX,
                tailRegionSizeY
            );
            //Rect tailRegion = new Rect(gray.Width / 2, gray.Height /2, gray.Width / 2, gray.Height / 2);

            //Mat visualDebug = new Mat();
            //Cv2.CvtColor(gray, visualDebug, ColorConversionCodes.GRAY2BGR);

            //Cv2.Rectangle(visualDebug, boundingRect, Scalar.Green, 1);
            //Cv2.Rectangle(visualDebug, tailRegion, Scalar.Red, 1);
            //Cv2.ImShow("Tail check", visualDebug);
            //Cv2.WaitKey(1);

            if (tailRegion.Width > 0 && tailRegion.Height > 0)
            {
                using (Mat tailArea = new Mat(gray, tailRegion))
                {
                    Scalar meanValue = Cv2.Mean(tailArea);
                    return meanValue[0] > 55 && boundingRect.Height > 42; // Dark pixels indicate a potential tail
                }
            }

            return false;
        }

        private string ExecuteStandardOcr(Mat final, string mode)
        {
            var ocr = new IronTesseract();
            ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.SingleChar;
            ocr.Configuration.WhiteListCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            ocr.Configuration.TesseractVariables["tessedit_char_blacklist"] = "0";
            ocr.Configuration.TesseractVariables["classify_bln_numeric_mode"] = "0";

            using (var input = new IronSoftware.Drawing.AnyBitmap(final.ToMemoryStream()))
            {
                var result = ocr.Read(input);
                return result?.Text?.Trim().ToUpper();
            }
        }

        private string ExecuteCircularLetterOcr(Mat final)
        {
            var ocr = new IronTesseract();
            ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.SingleChar;
            ocr.Configuration.WhiteListCharacters = "OQ"; // Only try O and Q

            using (var input = new IronSoftware.Drawing.AnyBitmap(final.ToMemoryStream()))
            {
                var result = ocr.Read(input);
                return result?.Text?.Trim().ToUpper();
            }
        }

        private bool CouldBeCircularLetter(Mat image)
        {
            // Simple heuristic: check if the image has roughly circular proportions
            using (Mat gray = new Mat())
            {
                if (image.Channels() == 3)
                    Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                else
                    image.CopyTo(gray);

                // Find contours
                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(gray, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);


                if (contours.Length > 0)
                {
                    var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                    var boundingRect = Cv2.BoundingRect(largestContour);
                    // Check if it's roughly square (circular letters tend to be more square)
                    double aspectRatio = (double)boundingRect.Width / boundingRect.Height;
                    return aspectRatio > 0.7 && aspectRatio < 1.3; // Roughly square
                }
            }

            return false;
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

            //Cv2.ImShow("Contours Debug - All contours (Green), Selected (Red) " + Guid.NewGuid().ToString(), contoursVisualization);
            //Cv2.WaitKey(1);
        }



        public void ShowErrorMontage()
        {
            if (ErrorImages.Count == 0) return;

            // Use the actual size of the first image
            Mat firstImage = ErrorImages[0];
            if (firstImage.Empty()) return;

            int imgWidth = firstImage.Width;
            int imgHeight = firstImage.Height;
            int count = ErrorImages.Count;

            Serilog.Log.Information($"Using image dimensions: {imgWidth}x{imgHeight}");

            // Calculate grid size
            int cols = (int)Math.Ceiling(Math.Sqrt(count));
            int rows = (int)Math.Ceiling((double)count / cols);

            using (Mat montage = new Mat(rows * imgHeight, cols * imgWidth, firstImage.Type(), Scalar.Black))
            {
                for (int i = 0; i < count; i++)
                {
                    int r = i / cols;
                    int c = i % cols;

                    Rect roi = new Rect(c * imgWidth, r * imgHeight, imgWidth, imgHeight);

                    if (!ErrorImages[i].Empty())
                    {
                        ErrorImages[i].CopyTo(new Mat(montage, roi));
                    }
                }

                Cv2.ImShow("All Errors", montage);
            }
        }

       
    }
}
