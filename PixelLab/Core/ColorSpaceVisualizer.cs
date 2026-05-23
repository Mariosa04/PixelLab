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


        public static List<PointData> DrawRgbCube(Bitmap imageBitmap)
        {
            var points = ImageProcessor.ExtractPoints(imageBitmap);

            DrawCubeWireframe();

            GL.Begin(PrimitiveType.Points);

            foreach (var p in points)
            {
                GL.Color3(p.Value.R, p.Value.G, p.Value.B);
                GL.Vertex3(p.Pos);
            }

            GL.End();

            return points;
        }

        public static List<PointData> DrawHsvCylinder(Bitmap imageBitmap)
        {
            var points = ImageProcessor.ExtractPoints(imageBitmap);

            GL.Begin(PrimitiveType.Points);

            foreach (var v in points)
            {
                float hue = v.Value.H;
                float sat = v.Value.S / 100f;
                float val = v.Value.V / 100f;

                float radius = sat * (val * 128);
                float angle = hue * (float)Math.PI / 180f;

                float x = radius * (float)Math.Cos(angle);
                float y = radius * (float)Math.Sin(angle);
                float z = val * 255;

                Vector3 p = new Vector3(x, y, z - 128);

                GL.Color3(v.Value.R, v.Value.G, v.Value.B);
                GL.Vertex3(p);
            }

            GL.End();
            return points;
        }

        public static List<PointData> DrawCmykCloud(Bitmap imageBitmap)
        {
            var points = ImageProcessor.ExtractPoints(imageBitmap);

            GL.Begin(PrimitiveType.Points);

            foreach (var v in points)
            {
                float r = v.Value.R / 255f;
                float g = v.Value.G / 255f;
                float b = v.Value.B / 255f;

                float k = 1 - Math.Max(r, Math.Max(g, b));
                float c = 1 - r - k;
                float m = 1 - g - k;
                float y = 1 - b - k;

                Vector3 p = new Vector3(
                    c * 255 - 128,
                    m * 255 - 128,
                    y * 255 - 128
                );

                GL.Color3(v.Value.R, v.Value.G, v.Value.B);
                GL.Vertex3(p);
            }

            GL.End();
            return points;
        }

        public static List<PointData> DrawYuvPlane(Bitmap imageBitmap)
        {
            var points = ImageProcessor.ExtractPoints(imageBitmap);

            GL.Begin(PrimitiveType.Points);

            foreach (var v in points)
            {
                Vector3 p = new Vector3(
                    v.Value.Y_yuv - 128,
                    v.Value.U,
                    v.Value.Vu
                );

                GL.Color3(v.Value.R, v.Value.G, v.Value.B);
                GL.Vertex3(p);
            }

            GL.End();
            return points;
        }

        public static List<PointData> DrawLabSpace(Bitmap imageBitmap)
        {
            var points = ImageProcessor.ExtractPoints(imageBitmap);

            GL.Begin(PrimitiveType.Points);

            foreach (var v in points)
            {
                Vector3 p = new Vector3(
                    v.Value.L_lab - 128,
                    v.Value.A_lab,
                    v.Value.B_lab
                );

                GL.Color3(v.Value.R, v.Value.G, v.Value.B);
                GL.Vertex3(p);
            }

            GL.End();
            return points;
        }

        public static List<PointData> DrawYCbCrSpace(Bitmap imageBitmap)
        {
            var points = ImageProcessor.ExtractPoints(imageBitmap);

            GL.Begin(PrimitiveType.Points);

            foreach (var v in points)
            {
                Vector3 p = new Vector3(
                    v.Value.Y_ycbcr - 128,
                    v.Value.Cb,
                    v.Value.Cr
                );

                GL.Color3(v.Value.R, v.Value.G, v.Value.B);
                GL.Vertex3(p);
            }

            GL.End();
            return points;
        }
    }
}