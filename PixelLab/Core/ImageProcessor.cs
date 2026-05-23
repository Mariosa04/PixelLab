using Emgu.CV;
using Emgu.CV.Structure;
using PixelLab.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using GdiPixelFormat = System.Drawing.Imaging.PixelFormat;
namespace PixelLab.Core
    {
        public class ImageProcessor
        {
        public static unsafe List<PointData> ExtractPoints(Bitmap bmp)
        {
            var points = new List<PointData>();

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, GdiPixelFormat.Format24bppRgb);

            byte* ptr = (byte*)data.Scan0;
            int stride = data.Stride;

            for (int y = 0; y < bmp.Height; y += 2)
            {
                byte* row = ptr + y * stride;

                for (int x = 0; x < bmp.Width; x += 2)
                {
                    int i = x * 3;

                    byte b = row[i];
                    byte g = row[i + 1];
                    byte r = row[i + 2];

                    ColorValues v = new ColorValues
                    {
                        R = r,
                        G = g,
                        B = b
                    };

                    ColorSpaceConverter.ApplyHSV(r, g, b, v);
                    ColorSpaceConverter.ApplyYUV(r, g, b, v);
                    ColorSpaceConverter.ApplyLab(r, g, b, v);
                    ColorSpaceConverter.ApplyYCbCr(r, g, b, v);
                    ColorSpaceConverter.ApplyCMYK(r, g, b, v);

                    Vector3 p = new Vector3(v.R - 128, v.G - 128, v.B - 128);

                    points.Add(new PointData
                    {
                        Pos = p,
                        Value = v
                    });
                }
            }

            bmp.UnlockBits(data);

            return points;
        }
    }
    }
