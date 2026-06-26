namespace Planner.BlazorApp.Services;

using System;

public static class ColourHelper {
    public static string ColourFromString(string input, double saturation, double lightness) {
        if (string.IsNullOrEmpty(input))
            input = "default";

        var hash = ComputeFnv1a(input);
        int hue = (int)(hash % 360);

        return HslToHex(hue, saturation, lightness);
    }

    private static uint ComputeFnv1a(string input) {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        unchecked {
            var hash = offsetBasis;
            foreach (var ch in input) {
                hash ^= ch;
                hash *= prime;
            }

            return hash;
        }
    }

    private static string HslToHex(double h, double s, double l) {
        h /= 360.0;
        double r = 0, g = 0, b = 0;

        if (s == 0) {
            r = g = b = l; // achromatic
        } else {
            Func<double, double, double, double> hueToRgb = (p, q, t) => {
                if (t < 0) t += 1;
                if (t > 1) t -= 1;
                if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
                if (t < 1.0 / 2.0) return q;
                if (t < 2.0 / 3.0) return p + (2.0 / 3.0 - t) * 6 * (q - p);
                return p;
            };

            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;
            r = hueToRgb(p, q, h + 1.0 / 3.0);
            g = hueToRgb(p, q, h);
            b = hueToRgb(p, q, h - 1.0 / 3.0);
        }

        return $"#{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(b * 255):X2}";
    }
}
