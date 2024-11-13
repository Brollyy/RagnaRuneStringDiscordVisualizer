using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

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

        var extension = Path.GetExtension(filePath);
        ImageFormatConverter converter = new();
        ImageFormat imageFormat = converter.ConvertFromString(string.Concat(extension[1].ToString(), extension.AsSpan(2))) switch
        {
            null => throw new ArgumentException($"Couldn't recognize image format for {extension}"),
            ImageFormat i => i,
            _ => throw new ArgumentException($"Couldn't recognize image format for {extension}")
        };

        RagnaRuneStringVisualizer.ImageGenerator imageGenerator = new(runestring);
        imageGenerator.RenderToFile(filePath, imageFormat);
    }
}