using IronSoftware.Drawing;
using RagnaRuneString;
using RagnaRuneString.Version1;
using RagnaRuneStringVisualizer.Properties;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using static IronSoftware.Drawing.AnyBitmap;
using static RagnaRuneStringVisualizer.DrawingConstants;
using PointF = System.Drawing.PointF;
using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;

namespace RagnaRuneStringVisualizer
{
    [SupportedOSPlatform("windows")]
    public class ImageGenerator : IDisposable
    {
        private RuneStringData? runeStringData;
        private bool disposedValue;

        public ImageGenerator(string runeString)
        {
            runeStringData = RuneStringSerializer.DeserializeV1(runeString);
            if (runeStringData.Value.runes.Count > 50)
            {
                throw new ArgumentException("Images can be generated only for maximum of 50 runes.");
            }
            if (runeStringData.Value.runes.Count == 0)
            {
                throw new ArgumentException("There are no runes to draw");
            }
            BPMChange globalBpm = GetGlobalBPM();
            BPMChange localBpm = GetLocalBPM(globalBpm.bpm);
            if (GetTotalBeats(globalBpm, localBpm) > 5)
            {
                throw new ArgumentException("Images can be generated only for maximum of 5 beats.");
            }
        }

        public void RenderToStream(Stream stream, ImageFormat imageFormat)
        {
            ObjectDisposedException.ThrowIf(runeStringData == null || disposedValue, this);

            // Figure out how many beats we'll need to draw and when we start in global beat time
            BPMChange globalBpm = GetGlobalBPM();
            BPMChange localBpm = GetLocalBPM(globalBpm.bpm);
            int totalBeats = GetTotalBeats(globalBpm, localBpm);
            double startTime = GetStartTime(globalBpm, localBpm);

            int imageHeight = (int)Math.Ceiling(ImageHeightPerBeat * (totalBeats + 2 * ImageVerticalPaddingInBeats));

            using Bitmap bitmap = new AnyBitmap(ImageWidth, imageHeight);
            using var graphics = Graphics.FromImage(bitmap);

            DrawBackground(graphics, imageHeight);
            DrawColumnShadows(graphics, imageHeight);
            DrawGridLines(graphics, totalBeats, localBpm);
            DrawNotes(graphics, startTime, imageHeight, globalBpm, localBpm);

            // Save the image to the provided stream in PNG format
            FromBitmap(bitmap).ExportStream(stream, imageFormat);
        }

        private static void DrawBackground(Graphics graphics, int imageHeight)
        {
            using var backgroundTextureBrush = new TextureBrush(Resources.waterTexture, WrapMode.Tile);
            graphics.FillRectangle(backgroundTextureBrush, 0, 0, ImageWidth, imageHeight);
        }

        private static void DrawColumnShadows(Graphics graphics, int imageHeight)
        {
            using var columnShadowBrush = new SolidBrush(ColorTranslator.FromHtml(ColumnShadowColor));
            using var columnShadowPen = new Pen(columnShadowBrush, ColumnShadowWidth);

            for (int i = 0; i < 4; ++i)
            {
                var x = ColumnShadowGap * (4 * i + 1) + ColumnShadowWidth / 2;
                graphics.DrawLine(columnShadowPen, new PointF(x, 0), new PointF(x, imageHeight));
            }
        }

        private static void DrawGridLines(Graphics graphics, int totalBeats, BPMChange localBpm)
        {
            using var majorGridlineBrush = new SolidBrush(ColorTranslator.FromHtml(MajorGridlineColor));
            using var majorGridlinePen = new Pen(majorGridlineBrush, MajorGridlineThickness);
            using var minorGridlineBrush = new SolidBrush(ColorTranslator.FromHtml(MinorGridlineColor));
            using var minorGridlinePen = new Pen(minorGridlineBrush, MinorGridlineThickness);

            float currentY = ImageVerticalPaddingInBeats * ImageHeightPerBeat;
            int counter = 0;
            while (counter <= totalBeats * GridDivision)
            {
                var pen = (counter % GridDivision == 0) switch
                {
                    true => majorGridlinePen,
                    false => minorGridlinePen,
                };
                graphics.DrawLine(pen, new PointF(0, currentY), new PointF(ImageWidth, currentY));

                currentY += GridlineOffset;
                counter++;
            }
        }

        private void DrawNotes(Graphics graphics, double startTime, int imageHeight, BPMChange globalBpm, BPMChange localBpm)
        {
            var scaleFactor = globalBpm.bpm / localBpm.bpm;
            var localStartTime = (startTime - localBpm.startTime) * scaleFactor;
            foreach (var note in runeStringData!.Value.runes)
            {
                var localNoteTime = (note.time - localBpm.startTime) * scaleFactor;
                var x = ColumnShadowGap * (4 * note.lineIndex + 1);
                var y = (float)((localNoteTime - localStartTime) * ImageHeightPerBeat) + RuneHeight / 2;
                graphics.DrawImage(
                    RuneForBeat(localNoteTime),
                    new RectangleF(x, imageHeight - y, RuneWidth, RuneHeight),
                    new Rectangle(0, 0, RuneImageWidth, RuneImageHeight),
                    GraphicsUnit.Pixel
                );
            }
        }

        private static Image RuneForBeat(double beat)
        {
            int fracBeat = (int)Math.Round((beat - (int)beat) * RuneDivisionResolution) % RuneDivisionResolution; // closest approximation of numerator with given denominator
            return FromBitmap(fracBeat switch
            {
                0 => Resources.rune1,
                RuneDivisionResolution * 1 / 4 => Resources.rune14,
                RuneDivisionResolution * 1 / 3 => Resources.rune13,
                RuneDivisionResolution * 5 / 6 => Resources.rune13,
                RuneDivisionResolution * 1 / 2 => Resources.rune12,
                RuneDivisionResolution * 2 / 3 => Resources.rune23,
                RuneDivisionResolution * 1 / 6 => Resources.rune23,
                RuneDivisionResolution * 3 / 4 => Resources.rune34,
                _ => Resources.runeX
            });
        }

        private int GetTotalBeats(BPMChange globalBpm, BPMChange localBpm)
        {
            Rune firstRune = runeStringData!.Value.runes.First();
            Rune lastRune = runeStringData.Value.runes.Last();

            double scaleFactor = globalBpm.bpm / localBpm.bpm;
            double localFirstRuneTime = (firstRune.time - localBpm.startTime) * scaleFactor;
            double localLastRuneTime = (lastRune.time - localBpm.startTime) * scaleFactor;
            int firstBeatStart = (int)localFirstRuneTime;
            int lastBeatEnd = (int)Math.Ceiling(Math.Round(localLastRuneTime * RuneDivisionResolution) / RuneDivisionResolution - 0.001); // To avoid issues with image extending to the next beat sometimes

            return lastBeatEnd - firstBeatStart;
        }

        private double GetStartTime(BPMChange globalBpm, BPMChange localBpm)
        {
            Rune firstRune = runeStringData!.Value.runes.First();

            double scaleFactor = globalBpm.bpm / localBpm.bpm;
            double localRuneTime = (firstRune.time - localBpm.startTime) * scaleFactor;
            double startTimeInLocalTime = (int)localRuneTime - ImageVerticalPaddingInBeats; // We leave some empty space at the start, so that runes are not cut-off.

            return startTimeInLocalTime / scaleFactor + localBpm.startTime;
        }

        private BPMChange GetGlobalBPM()
        {
            BPMChange globalBpm = runeStringData!.Value.bpmChanges.Where(b => b.startTime <= 0.0001).FirstOrDefault();
            if (globalBpm.bpm == 0) globalBpm.bpm = 120;
            return globalBpm;
        }

        private BPMChange GetLocalBPM(double globalBpm)
        {
            Rune firstRune = runeStringData!.Value.runes.First();
            BPMChange localBpm = runeStringData!.Value.bpmChanges.Where(b => firstRune.time - b.startTime >= 0.0001).LastOrDefault();
            if (localBpm.bpm == 0) localBpm.bpm = globalBpm;
            return localBpm;
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
