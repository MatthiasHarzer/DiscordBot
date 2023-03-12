using System.Drawing;

namespace DiscordBot.Utility;

// ReSharper disable once InconsistentNaming
public class HSL
{
    public double Hue { get; init; }
    public double Saturation { get; init; }
    public double Brightness { get; init; }

    /// <summary>
    /// <para>Convert from the current HSL to RGB</para>
    /// <para>http://en.wikipedia.org/wiki/HSV_color_space#Conversion_from_HSL_to_RGB</para>
    /// </summary>
    public Color Color
    {
        get
        {
            double[] t = new double[] { 0, 0, 0 };

            try
            {
                double tH = Hue;
                double tS = Saturation;
                double tL = Brightness;

                if (tS.Equals(0))
                {
                    t[0] = t[1] = t[2] = tL;
                }
                else
                {
                    double q, p;

                    q = tL < 0.5 ? tL * (1 + tS) : tL + tS - (tL * tS);
                    p = 2 * tL - q;

                    t[0] = tH + (1.0 / 3.0);
                    t[1] = tH;
                    t[2] = tH - (1.0 / 3.0);

                    for (byte i = 0; i < 3; i++)
                    {
                        t[i] = t[i] < 0 ? t[i] + 1.0 : t[i] > 1 ? t[i] - 1.0 : t[i];

                        if (t[i] * 6.0 < 1.0)
                            t[i] = p + ((q - p) * 6 * t[i]);
                        else if (t[i] * 2.0 < 1.0)
                            t[i] = q;
                        else if (t[i] * 3.0 < 2.0)
                            t[i] = p + ((q - p) * 6 * ((2.0 / 3.0) - t[i]));
                        else
                            t[i] = p;
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return Color.FromArgb((int)(t[0] * 255), (int)(t[1] * 255), (int)(t[2] * 255));
        }
    }
}
