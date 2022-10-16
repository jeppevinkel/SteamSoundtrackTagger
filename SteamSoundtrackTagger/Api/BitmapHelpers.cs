using System.IO;
using System.Windows.Media.Imaging;

namespace SteamSoundtrackTagger.Api;

public static class BitmapHelpers
{
    public static void SaveJpgImage(this BitmapImage source, string filePath)
    {
        SaveImage(source, filePath, new JpegBitmapEncoder());
    }

    public static void SavePngImage(this BitmapImage source, string filePath)
    {
        SaveImage(source, filePath, new PngBitmapEncoder());
    }
    
    private static void SaveImage(BitmapImage source, string filePath, BitmapEncoder encoder)
    {
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new FileStream(filePath, FileMode.Create);
        encoder.Save(stream);
    }
}