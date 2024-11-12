namespace RagnaRuneStringVisualizer
{
    internal class DrawingConstants
    {
        public const int GridDivision = 4;
        public const int ImageWidth = 400;
        public const int ImageHeightPerBeat = 200;
        public const float ImageVerticalPaddingInBeats = 1.0f / GridDivision;

        // Column shadows
        public const float ColumnShadowWidth = ImageWidth * 3 / (3 * 4 + 5); // Columns are 3 times wider than gaps between them, with gaps on the left and right as well.
        public const float ColumnShadowGap = ImageWidth / (3 * 4 + 5);
        public const string ColumnShadowColor = "#0A000000";

        // Gridlines
        public const string MajorGridlineColor = "#333333";
        public const string MinorGridlineColor = "#555555";
        public const float MajorGridlineThickness = 1.5f; // Doesn't seem to be different from 1?
        public const float MinorGridlineThickness = 1;
        public const float GridlineOffset = (float)ImageHeightPerBeat / GridDivision;

        // Runes
        public const float RuneWidth = 3 * ColumnShadowGap;
        public const float RuneHeight = 3 * ColumnShadowGap;
        public const int RuneImageWidth = 1024;
        public const int RuneImageHeight = 1024;
        public const int RuneDivisionResolution = 64 * 6;

    }
}
