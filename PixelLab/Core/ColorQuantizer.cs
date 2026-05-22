using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
namespace PixelLab.Core
{
    internal class ColorQuantizer
    {
        /// Reduces number of colors in image using uniform quantization
        /// Example: 256 → 4 colors per channel
        public static Bitmap Quantize(Bitmap source, int levels)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Bitmap result = new Bitmap(source.Width, source.Height);

            // convert levels to step size
            int step = 256 / levels;
            if (step == 0) step = 1;

            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Color p = source.GetPixel(x, y);

                    int r = QuantizeValue(p.R, step);
                    int g = QuantizeValue(p.G, step);
                    int b = QuantizeValue(p.B, step);

                    result.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }

            return result;
        }

        /// Quantize a single channel value
        private static int QuantizeValue(int value, int step)
        {
            int q = (value / step) * step;
            return Clamp(q, 0, 255);
        }

       
        /// Clamp helper
        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }


    }
}
