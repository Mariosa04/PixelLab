using OpenTK;
using OpenTK.Graphics.OpenGL;
using PixelLab.Models;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace PixelLab.Core
{
    public static class ColorSpaceVisualizer
    {
        public static void DrawAxes()
        {
            GL.Begin(PrimitiveType.Lines);

            GL.Color3(Color.Red);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(300, 0, 0);

            GL.Color3(Color.Green);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 300, 0);

            GL.Color3(Color.Blue);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 0, 300);

            GL.End();
        }

        private static void DrawCubeWireframe()
        {
            GL.Color3(Color.White);
            GL.Begin(PrimitiveType.Lines);

            float s = 128;

            GL.Vertex3(-s, -s, -s); GL.Vertex3(s, -s, -s);
            GL.Vertex3(s, -s, -s); GL.Vertex3(s, s, -s);
            GL.Vertex3(s, s, -s); GL.Vertex3(-s, s, -s);
            GL.Vertex3(-s, s, -s); GL.Vertex3(-s, -s, -s);

            GL.Vertex3(-s, -s, s); GL.Vertex3(s, -s, s);
            GL.Vertex3(s, -s, s); GL.Vertex3(s, s, s);
            GL.Vertex3(s, s, s); GL.Vertex3(-s, s, s);
            GL.Vertex3(-s, s, s); GL.Vertex3(-s, -s, s);

            GL.Vertex3(-s, -s, -s); GL.Vertex3(-s, -s, s);
            GL.Vertex3(s, -s, -s); GL.Vertex3(s, -s, s);
            GL.Vertex3(s, s, -s); GL.Vertex3(s, s, s);
            GL.Vertex3(-s, s, -s); GL.Vertex3(-s, s, s);

            GL.End();
        }
        public static ColorValues Convert(Color c)
        {
            ColorValues v = new ColorValues();

            v.R = c.R;
            v.G = c.G;
            v.B = c.B;

            ColorSpaceConverter.ApplyCMYK(c, v);
            ColorSpaceConverter.ApplyHSV(c, v);
            ColorSpaceConverter.ApplyYUV(c, v);
            ColorSpaceConverter.ApplyLab(c, v);
            ColorSpaceConverter.ApplyYCbCr(c, v);

            return v;
        }

        public static List<PointData> DrawRgbCube(Bitmap imageBitmap)
        {
            var points = new List<PointData>();

            DrawCubeWireframe();

            GL.Begin(PrimitiveType.Points);

            for (int y = 0; y < imageBitmap.Height; y += 2)
                for (int x = 0; x < imageBitmap.Width; x += 2)
                {
                    ColorValues v = Convert(imageBitmap.GetPixel(x, y));

                    Vector3 p = new Vector3(v.R - 128, v.G - 128, v.B - 128);

                    points.Add(new PointData
                    {
                        Pos = p,
                        Value = v
                    });

                    GL.Color3(v.R, v.G, v.B);
                    GL.Vertex3(p);
                }

            GL.End();
            return points;
        }

        public static List<PointData> DrawHsvCylinder(Bitmap imageBitmap)
        {
            var points = new List<PointData>();
            GL.Begin(PrimitiveType.Points);

            for (int y = 0; y < imageBitmap.Height; y += 2)
                for (int x = 0; x < imageBitmap.Width; x += 2)
                {
                    ColorValues v = Convert(imageBitmap.GetPixel(x, y));

                    float hue = v.H;
                    float sat = v.S / 100f; 
                    float val = v.V / 100f;   

                    float radius = sat * (val * 128);
                    float angle = hue * (float)Math.PI / 180f;

                    float X = radius * (float)Math.Cos(angle);
                    float Y = radius * (float)Math.Sin(angle);
                    float Z = val * 255;

                    var p = new PointData
                    {
                        Pos = new Vector3(X, Y, Z - 128),
                        Value = v
                    };

                    points.Add(p);
                    GL.Color3(v.R, v.G, v.B);
                    GL.Vertex3(p.Pos);
                }

            GL.End();
            return points;
        }

        public static List<PointData> DrawCmykCloud(Bitmap imageBitmap)
        {
            var points = new List<PointData>();

            GL.Begin(PrimitiveType.Points);

            for (int y = 0; y < imageBitmap.Height; y += 2)
                for (int x = 0; x < imageBitmap.Width; x += 2)
                {
                    ColorValues v = Convert(imageBitmap.GetPixel(x, y));

                    float r = v.R / 255f;
                    float g = v.G / 255f;
                    float b = v.B / 255f;

                    float K = 1 - Math.Max(r, Math.Max(g, b));
                    float C = (1 - r - K);
                    float M = (1 - g - K);
                    float Y = (1 - b - K);

                    float X = C * 255;
                    float Ypos = M * 255;
                    float Z = Y * 255;

                    var p = new PointData
                    {
                        Pos = new Vector3(X - 128, Ypos - 128, Z - 128),
                        Value = v
                    };

                    points.Add(p);

                    GL.Color3(v.R, v.G, v.B);
                    GL.Vertex3(p.Pos);
                }

            GL.End();
            return points;
        }

        public static List<PointData> DrawYuvPlane(Bitmap imageBitmap)
        {
            var points = new List<PointData>();

            GL.Begin(PrimitiveType.Points);

            for (int y = 0; y < imageBitmap.Height; y += 2)
                for (int x = 0; x < imageBitmap.Width; x += 2)
                {
                    ColorValues v = Convert(imageBitmap.GetPixel(x, y));

                    float Yl = v.Y_yuv;
                    float U = v.U;
                    float V = v.Vu;

                    var p = new PointData
                    {
                        Pos = new Vector3(Yl - 128, U, V),
                        Value = v
                    };

                    points.Add(p);

                    GL.Color3(v.R, v.G, v.B);
                    GL.Vertex3(p.Pos);
                }

            GL.End();
            return points;
        }

        public static List<PointData> DrawLabSpace(Bitmap imageBitmap)
        {
            var points = new List<PointData>();

            GL.Begin(PrimitiveType.Points);

            for (int y = 0; y < imageBitmap.Height; y += 2)
                for (int x = 0; x < imageBitmap.Width; x += 2)
                {
                    ColorValues v = Convert(imageBitmap.GetPixel(x, y));

                    float L = v.L_lab;
                    float a = v.A_lab;
                    float b = v.B_lab;

                    var p = new PointData
                    {
                        Pos = new Vector3(L - 128, a, b),
                        Value = v
                    };

                    points.Add(p);

                    GL.Color3(v.R, v.G, v.B);
                    GL.Vertex3(p.Pos);
                }

            GL.End();
            return points;
        }

        public static List<PointData> DrawYCbCrSpace(Bitmap imageBitmap)
        {
            var points = new List<PointData>();

            GL.Begin(PrimitiveType.Points);

            for (int y = 0; y < imageBitmap.Height; y += 2)
                for (int x = 0; x < imageBitmap.Width; x += 2)
                {
                    ColorValues v = Convert(imageBitmap.GetPixel(x, y));

                    float Yl = v.Y_ycbcr;
                    float Cb = v.Cb;
                    float Cr = v.Cr;

                    var p = new PointData
                    {
                        Pos = new Vector3(Yl - 128, Cb, Cr),
                        Value = v
                    };

                    points.Add(p);

                    GL.Color3(v.R, v.G, v.B);
                    GL.Vertex3(p.Pos);
                }

            GL.End();
            return points;
        }
    }
}