using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Snake
{
    /// <summary>Sprites for one snake.</summary>
    public sealed record SnakeSprites(ImageSource Body, ImageSource Head, ImageSource DeadBody, ImageSource DeadHead);

    public static class Images
    {
        public static readonly ImageSource Empty = LoadImage("Empty.png");
        public static readonly ImageSource Food = LoadImage("Food.png");

        public static readonly SnakeSprites PlayerOne = new(
            LoadImage("Body.png"), LoadImage("Head.png"), LoadImage("DeadBody.png"), LoadImage("DeadHead.png"));

        /// <summary>Player two is player one's sprites in blue, made at startup instead of shipping more images.</summary>
        public static readonly SnakeSprites PlayerTwo = new(
            Recolor(PlayerOne.Body), Recolor(PlayerOne.Head), Recolor(PlayerOne.DeadBody), Recolor(PlayerOne.DeadHead));

        public static SnakeSprites ForPlayer(int index) => index == 0 ? PlayerOne : PlayerTwo;

        private static BitmapSource LoadImage(string filename)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri($"pack://application:,,,/Slike/{filename}", UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private static ImageSource Recolor(ImageSource source)
        {
            var converted = new FormatConvertedBitmap((BitmapSource)source, PixelFormats.Bgra32, null, 0);
            int stride = converted.PixelWidth * 4;
            byte[] pixels = new byte[stride * converted.PixelHeight];
            converted.CopyPixels(pixels, stride, 0);
            SpriteTint.SwapRedAndBlue(pixels);
            BitmapSource result = BitmapSource.Create(
                converted.PixelWidth, converted.PixelHeight, converted.DpiX, converted.DpiY,
                PixelFormats.Bgra32, null, pixels, stride);
            result.Freeze();
            return result;
        }
    }

    /// <summary>Pixel work kept apart from <see cref="Images"/>, which needs a running application to load.</summary>
    public static class SpriteTint
    {
        /// <summary>Swaps the red and blue channels of BGRA pixels: orange becomes blue, white and black stay.</summary>
        public static void SwapRedAndBlue(byte[] bgra)
        {
            for (int i = 0; i + 3 < bgra.Length; i += 4)
            {
                (bgra[i], bgra[i + 2]) = (bgra[i + 2], bgra[i]);
            }
        }
    }
}
