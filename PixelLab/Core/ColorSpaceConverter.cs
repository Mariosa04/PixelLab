using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure; 

namespace PixelLab.Core
{
    public static class ColorSpaceConverter
    {
        public static Bitmap ProcessImage(Bitmap source, string colorSpace, bool enableC1, bool enableC2, bool enableC3, bool enableC4, float mC1, float mC2, float mC3, float mC4)
        {
            if (colorSpace == "HSV")
            {
                // Convert Bitmap to EmguCV BGR Image then to Mat format
                Image<Bgr, byte> bgrImg = source.ToImage<Bgr, byte>();
                Mat rgbImage = bgrImg.Mat;

                // Convert RGB to HSV
                Mat hsvImage = new Mat();
                CvInvoke.CvtColor(rgbImage, hsvImage, ColorConversion.Bgr2Hsv);

                // Use Image<Hsv, byte> for easy pixel modifications
                Image<Hsv, byte> hsvImgData = hsvImage.ToImage<Hsv, byte>();

                for (int y = 0; y < hsvImgData.Height; y++)
                {
                    for (int x = 0; x < hsvImgData.Width; x++)
                    {
                        Hsv p = hsvImgData[y, x];

                        double h = enableC1 ? p.Hue + mC1 : 0;
                        double s = enableC2 ? p.Satuation + mC2 : 0;
                        double v = enableC3 ? p.Value + mC3 : 0;

                        // Clamp values to valid HSV ranges 
                        // Hue = 0-179, Saturation = 0-255, Value = 0-255 in EmguCV 8-bit
                        hsvImgData[y, x] = new Hsv(
                            ClampDouble(h, 0, 179),
                            ClampDouble(s, 0, 255),
                            ClampDouble(v, 0, 255)
                        );
                    }
                }

                // Convert back from HSV to BGR
                Mat resultMat = new Mat();
                CvInvoke.CvtColor(hsvImgData, resultMat, ColorConversion.Hsv2Bgr);

                return resultMat.ToImage<Bgr, byte>().ToBitmap();
            }
            else if (colorSpace == "YCbCr")
            {
                // Convert Bitmap to EmguCV BGR Image then to Mat format
                Image<Bgr, byte> bgrImg = source.ToImage<Bgr, byte>();
                Mat rgbImage = bgrImg.Mat;

                // Convert RGB to YCbCr
                Mat ycbcrImage = new Mat();
                CvInvoke.CvtColor(rgbImage, ycbcrImage, ColorConversion.Bgr2YCrCb);

                // Use Image<Ycc, byte> for easy pixel modifications
                Image<Ycc, byte> yccImgData = ycbcrImage.ToImage<Ycc, byte>();

                for (int y = 0; y < yccImgData.Height; y++)
                {
                    for (int x = 0; x < yccImgData.Width; x++)
                    {
                        Ycc p = yccImgData[y, x];

                        double Y = enableC1 ? p.Y + mC1 : 0;
                        double Cb = enableC2 ? p.Cb + mC2 : 128; // default neutral chroma is 128
                        double Cr = enableC3 ? p.Cr + mC3 : 128; // default neutral chroma is 128

                        // Clamp values
                        yccImgData[y, x] = new Ycc(
                            ClampDouble(Y, 0, 255),
                            ClampDouble(Cb, 0, 255),
                            ClampDouble(Cr, 0, 255)
                        );
                    }
                }

                // Convert back from YCbCr to BGR
                Mat resultMat = new Mat();
                CvInvoke.CvtColor(yccImgData, resultMat, ColorConversion.YCrCb2Bgr);

                return resultMat.ToImage<Bgr, byte>().ToBitmap();
            }

            else if (colorSpace == "YUV")
            {
                // Convert Bitmap to EmguCV BGR Image then to Mat format
                Image<Bgr, byte> bgrImg = source.ToImage<Bgr, byte>();
                Mat rgbImage = bgrImg.Mat;

                // Convert RGB to YUV
                Mat yuvImage = new Mat();
                CvInvoke.CvtColor(rgbImage, yuvImage, ColorConversion.Bgr2Yuv);

                // Emgu CV does not have Image<Yuv, byte> by default, so we use Image<Bgr, byte> as a 3-channel container
                // Channel 0 (Blue) = Y, Channel 1 (Green) = U, Channel 2 (Red) = V
                Image<Bgr, byte> yuvImgData = yuvImage.ToImage<Bgr, byte>();

                for (int y = 0; y < yuvImgData.Height; y++)
                {
                    for (int x = 0; x < yuvImgData.Width; x++)
                    {
                        Bgr p = yuvImgData[y, x];

                        double Y_val = enableC1 ? p.Blue + mC1 : 0;
                        double U_val = enableC2 ? p.Green + mC2 : 128; // default neutral chroma is 128
                        double V_val = enableC3 ? p.Red + mC3 : 128; // default neutral chroma is 128

                        // Clamp values
                        yuvImgData[y, x] = new Bgr(
                            ClampDouble(Y_val, 0, 255),
                            ClampDouble(U_val, 0, 255),
                            ClampDouble(V_val, 0, 255)
                        );
                    }
                }

                // Convert back from YUV to BGR
                Mat resultMat = new Mat();
                CvInvoke.CvtColor(yuvImgData, resultMat, ColorConversion.Yuv2Bgr);

                return resultMat.ToImage<Bgr, byte>().ToBitmap();
            }
            else if (colorSpace == "Lab")
            {
                // Convert Bitmap to EmguCV BGR Image then to Mat format
                Image<Bgr, byte> bgrImg = source.ToImage<Bgr, byte>();
                Mat rgbImage = bgrImg.Mat;

                // Convert RGB to Lab
                Mat labImage = new Mat();
                CvInvoke.CvtColor(rgbImage, labImage, ColorConversion.Bgr2Lab);

                // Use Image<Bgr, byte> as a 3-channel container
                Image<Bgr, byte> labImgData = labImage.ToImage<Bgr, byte>();

                for (int y = 0; y < labImgData.Height; y++)
                {
                    for (int x = 0; x < labImgData.Width; x++)
                    {
                        Bgr p = labImgData[y, x];

                        double L = enableC1 ? p.Blue + mC1 : 0;
                        double a = enableC2 ? p.Green + mC2 : 128; // default neutral for 8-bit
                        double b_val = enableC3 ? p.Red + mC3 : 128; // default neutral for 8-bit

                        // Clamp values
                        labImgData[y, x] = new Bgr(
                            ClampDouble(L, 0, 255),
                            ClampDouble(a, 0, 255),
                            ClampDouble(b_val, 0, 255)
                        );
                    }
                }

                // Convert back from Lab to BGR
                Mat resultMat = new Mat();
                CvInvoke.CvtColor(labImgData, resultMat, ColorConversion.Lab2Bgr);

                return resultMat.ToImage<Bgr, byte>().ToBitmap();
            }

            // Fallback for RGB and CMYK manually processing
            Bitmap result = new Bitmap(source.Width, source.Height);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Color p = source.GetPixel(x, y);

                    if (colorSpace == "CMYK")
                    {
                        // Calculate CMY values as requested
                        int c = 255 - p.R; // Cyan
                        int m = 255 - p.G; // Magenta
                        int y_val = 255 - p.B; // Yellow

                        // Extract Key (Black)
                        int k = Math.Min(c, Math.Min(m, y_val));

                        // Separate CMY from K
                        c -= k;
                        m -= k;
                        y_val -= k;

                        // Apply channel toggles
                        if (!enableC1) c = 0;
                        if (!enableC2) m = 0;
                        if (!enableC3) y_val = 0;
                        if (!enableC4) k = 0;

                        // Apply modifiers from trackbars
                        c += (int)mC1;
                        m += (int)mC2;
                        y_val += (int)mC3;
                        k += (int)mC4;

                        // Calculate final RGB
                        int r_new = 255 - Clamp(c + k, 0, 255);
                        int g_new = 255 - Clamp(m + k, 0, 255);
                        int b_new = 255 - Clamp(y_val + k, 0, 255);

                        Color cmykColor = Color.FromArgb(r_new, g_new, b_new);
                        result.SetPixel(x, y, cmykColor);
                        continue;
                    }

                    float c1 = 0, c2 = 0, c3 = 0;

                    if (colorSpace == "RGB")
                    {
                        c1 = p.R; c2 = p.G; c3 = p.B;
                    }

                    // Apply enable/disable
                    if (!enableC1) c1 = 0;
                    if (!enableC2) c2 = 0;
                    if (!enableC3) c3 = 0;

                    // Apply modifiers (simple addition)
                    c1 += mC1; c2 += mC2; c3 += mC3;

                    Color finalColor = p;
                    if (colorSpace == "RGB")
                    {
                        finalColor = Color.FromArgb(Clamp((int)c1, 0, 255), Clamp((int)c2, 0, 255), Clamp((int)c3, 0, 255));
                    }

                    result.SetPixel(x, y, finalColor);
                }
            }
            return result;
        }

        private static double ClampDouble(double val, double min, double max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static void RgbToYuv(int r, int g, int b, out float y, out float u, out float v)
        {
            y = 0.299f * r + 0.587f * g + 0.114f * b;
            u = -0.14713f * r - 0.28886f * g + 0.436f * b;
            v = 0.615f * r - 0.51499f * g - 0.10001f * b;
        }

        public static void YuvToRgb(float y, float u, float v, out int r, out int g, out int b)
        {
            r = (int)(y + 1.13983f * v);
            g = (int)(y - 0.39465f * u - 0.58060f * v);
            b = (int)(y + 2.03211f * u);
        }
    }
}
