using RagnaRuneString;
using RagnaRuneString.Version1;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace RagnaRuneStringVisualizer
{
    [SupportedOSPlatform("windows")]
    public class ImageGenerator(string runeString) : IDisposable
    {
        private RuneStringData? runeStringData = RuneStringSerializer.DeserializeV1(runeString);
        private bool disposedValue;

        public void RenderToStream(Stream stream, ImageFormat imageFormat)
        {
            // Set up the canvas dimensions
            int width = 400;
            int height = 200;

            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);

            // Set background color
            graphics.Clear(Color.LightBlue);

            // Draw some text
            using (var font = new Font("Arial", 24, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.DarkBlue))
            {
                graphics.DrawString("Hi Jo! :)", font, brush, new PointF(10, 50));
            }

            // Draw shapes, lines, etc. as desired
            graphics.DrawRectangle(Pens.DarkRed, 10, 100, 380, 80);

            // Save the image to the provided stream in PNG format
            bitmap.Save(stream, imageFormat);
        }

        public void RenderToFile(string filePath, ImageFormat imageFormat)
        {
            using var fileStream = File.OpenWrite(filePath);
            RenderToStream(fileStream, imageFormat);
            fileStream.Flush();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                runeStringData = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
