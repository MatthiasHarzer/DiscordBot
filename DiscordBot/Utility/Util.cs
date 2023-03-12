using DiscordColor = Discord.Color;
using Color = System.Drawing.Color;

namespace DiscordBot.Utility;


public static class Util
{
    private static readonly Random Random = new();
    private static List<Color> _colorPalette = new ();

    private static List<Color> ColorPallet
    {
        get
        {
            if (_colorPalette.Count <= 0)
            {
                _colorPalette = GenerateColorPallet();
            }

            return _colorPalette;
        }
    }

    /// <summary>
    /// Generate a nice color pallet. See <a href="http://devmag.org.za/2012/07/29/how-to-choose-colours-procedurally-algorithms/">How to Choose Colours Procedurally (Algorithms)</a>
    /// </summary>
    /// <returns></returns>
    private static List<Color> GenerateColorPallet()
    {
        List<Color> colors = new List<Color>();

        double[] saturations = { 1.0f, 0.7f };
        double[] luminances = { 0.45f, 0.7f };

        int v = Random.Next(2);
        double saturation = saturations[v];
        double luminance = luminances[v];

        double goldenRatioConjugate = 0.618033988749895f;
        double currentHue = Random.NextDouble();

        int colorCount = 50;

        for (int i = 0; i < colorCount; i++)
        {
            HSL hslColor = new HSL
            {
                Hue = currentHue,
                Saturation = saturation,
                Brightness = luminance
            };

            colors.Add(hslColor.Color);

            currentHue += goldenRatioConjugate;
            currentHue %= 1.0f;
        }

        return colors;
    }

    public static string RandomString(int length = 20)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Next(s.Length)]).ToArray());
    }

    private static Color RandomMix(Color color1, Color color2, Color color3)
    {
        double[] greys = { 0.1, 0.5, 0.9 };

        double grey = greys[Random.Next(greys.Length)];

        int randomIndex = Random.Next(3);

        double mixRatio1 =
            (randomIndex == 0) ? Random.NextDouble() * grey : Random.NextDouble();

        double mixRatio2 =
            (randomIndex == 1) ? Random.NextDouble() * grey : Random.NextDouble();

        double mixRatio3 =
            (randomIndex == 2) ? Random.NextDouble() * grey : Random.NextDouble();

        double sum = mixRatio1 + mixRatio2 + mixRatio3;

        mixRatio1 /= sum;
        mixRatio2 /= sum;
        mixRatio3 /= sum;

        return Color.FromArgb(
            255,
            (byte)(mixRatio1 * color1.R + mixRatio2 * color2.R + mixRatio3 * color3.R),
            (byte)(mixRatio1 * color1.G + mixRatio2 * color2.G + mixRatio3 * color3.G),
            (byte)(mixRatio1 * color1.B + mixRatio2 * color2.B + mixRatio3 * color3.B));
    }

    /// <summary>
    /// Returns a random color based on the initial generated color pallet
    /// </summary>
    /// <returns>The newly generated color</returns>
    public static DiscordColor RandomColor()
    {
        Color[] colors = new Color[3];

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = ColorPallet[Random.Next(ColorPallet.Count)];
        }

        //* Mix the colors for even more randomnes
        Color color = RandomMix(colors[0], colors[1], colors[2]);
        return new DiscordColor(color.R, color.G, color.B);
    }
    
    public static List<T> Shuffle<T>(this IEnumerable<T> list)
    {
        var rng = new Random();
        return list.OrderBy(_ => rng.Next()).ToList();
    }


}