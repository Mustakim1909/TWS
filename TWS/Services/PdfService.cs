using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWS.Interfaces;
using TWS.Models;

namespace TWS.Services
{
    public class PdfService : IPdfService
    {
        public async Task<string> GeneratePdfFromView(VisualElement visualElement, Order order, string type)
        {
            if (visualElement == null)
                throw new ArgumentNullException(nameof(visualElement));

            // Create file path
            string filePath = string.Empty;
            if (type == "Label")
            {
                filePath = Path.Combine(FileSystem.CacheDirectory, $"{order.Shipping.FullName}.pdf");
            }
            else
            {
                filePath = Path.Combine(FileSystem.CacheDirectory, $"Invoice_{order.Id}.pdf");
            }

                try
                {
                    // 1️⃣ Capture the VisualElement as screenshot
                    var screenshot = await visualElement.CaptureAsync();
                    using var screenshotStream = await screenshot.OpenReadAsync();

                    // 2️⃣ Decode screenshot into SKBitmap
                    using var bitmap = SKBitmap.Decode(screenshotStream);

                    if (bitmap == null)
                        throw new Exception("Failed to decode screenshot to bitmap.");

                    // 3️⃣ Create PDF document
                    using var document = SKDocument.CreatePdf(filePath);

                    // 4️⃣ Begin PDF page using bitmap dimensions
                    using var canvas = document.BeginPage(bitmap.Width, bitmap.Height);

                    // 5️⃣ Draw bitmap onto PDF page
                    using var skImage = SKImage.FromBitmap(bitmap);
                    canvas.DrawImage(skImage, 0, 0);

                    // 6️⃣ Finish PDF
                    document.EndPage();
                    document.Close();

                    return filePath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PDF generation error: {ex.Message}");
                    throw;
                }
        }

        private SKBitmap CaptureViewAlternative(VisualElement visualElement)
        {
            try
            {
                // Platform-specific view capture
                // This is a simplified version - in practice you'd need platform-specific code

                // For demonstration, we'll create a simple bitmap with the view's dimensions
                int width = (int)visualElement.Width;
                int height = (int)visualElement.Height;

                if (width <= 0) width = 600; // Default width
                if (height <= 0) height = 800; // Default height

                var bitmap = new SKBitmap(width, height);
                using var canvas = new SKCanvas(bitmap);

                // Draw a white background
                canvas.Clear(SKColors.White);

                // Draw a representation of the label
                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 24,
                    IsAntialias = true
                };

                canvas.DrawText("Shipping Label", 50, 50, paint);
                paint.TextSize = 16;
                canvas.DrawText("This is a generated shipping label", 50, 80, paint);

                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing view: {ex.Message}");

                // Fallback: Create a simple bitmap
                var bitmap = new SKBitmap(600, 800);
                using var canvas = new SKCanvas(bitmap);
                canvas.Clear(SKColors.White);

                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 20,
                    IsAntialias = true
                };

                canvas.DrawText("Shipping Label Preview", 50, 50, paint);
                canvas.DrawText("Could not capture view directly", 50, 80, paint);

                return bitmap;
            }
        }
    }
}


