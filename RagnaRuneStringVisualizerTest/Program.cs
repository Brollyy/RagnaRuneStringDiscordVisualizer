using System.Runtime.Versioning;
using static IronSoftware.Drawing.AnyBitmap;

internal class Program
{
    [SupportedOSPlatform("windows")]
    private static void Main(string[] args)
    {
        Console.Write("Runestring: ");
        string? runestring = Console.ReadLine();
        if (runestring == null)
        {
            Console.Error.WriteLine("No runestring provided!");
            return;
        }
        Console.Write("Path to the file: ");
        string? filePath = Console.ReadLine();
        if (filePath == null)
        {
            Console.Error.WriteLine("No file path provided!");
            return;
        }

        ImageFormat imageFormat = GetImageFormat(filePath);
        RagnaRuneStringVisualizer.ImageGenerator imageGenerator = new(runestring);
        imageGenerator.RenderToFile(filePath, imageFormat);
    }

    private static ImageFormat GetImageFormat(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            throw new FileNotFoundException("Please provide filename.");
        }

        if (filename.ToLower().EndsWith("png"))
        {
            return ImageFormat.Png;
        }
        else if (filename.ToLower().EndsWith("jpg") || filename.ToLower().EndsWith("jpeg"))
        {
            return ImageFormat.Jpeg;
        }
        else if (filename.ToLower().EndsWith("webp"))
        {
            return ImageFormat.Jpeg;
        }
        else if (filename.ToLower().EndsWith("gif"))
        {
            return ImageFormat.Gif;
        }
        else if (filename.ToLower().EndsWith("tif") || filename.ToLower().EndsWith("tiff"))
        {
            return ImageFormat.Tiff;
        }
        else
        {
            return ImageFormat.Bmp;
        }
    }
}