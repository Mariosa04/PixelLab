using OpenTK;
using OpenTK.Graphics.OpenGL;
using PixelLab.Core;
using PixelLab.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PixelLab.Forms
{
    public class ColorSpace3DForm : Form
    {
        private GLControl gl;
        private float angle = 0;
        private Bitmap imageBitmap;
        private string mode;

        private float zoom = 1f;
        private float rotX = 0;
        private float rotY = 0;
        private bool dragging = false;
        private Point lastMouse;

        private Label lblInfo;

        private List<PointData> points = new List<PointData>();

        public event Action<ColorValues>ColorSelected;

        public ColorSpace3DForm(Bitmap bmp, string colorMode)
        {
            imageBitmap = bmp;
            mode = colorMode;

            Width = 800;
            Height = 600;

            gl = new GLControl();
            gl.Dock = DockStyle.Fill;
            Controls.Add(gl);

            gl.Load += Gl_Load;
            gl.Paint += Gl_Paint;
            gl.Resize += Gl_Resize;

            // rotation
            gl.MouseDown += (s, e) =>
            {
                dragging = true;
                lastMouse = e.Location;
            };

            gl.MouseUp += (s, e) =>
            {
                dragging = false;
            };

            gl.MouseMove += (s, e) =>
            {
                if (!dragging) return;

                rotY += (e.X - lastMouse.X) * 0.5f;
                rotX += (e.Y - lastMouse.Y) * 0.5f;

                lastMouse = e.Location;
                gl.Invalidate();
            };

            // zoom
            gl.MouseWheel += (s, e) =>
            {
                zoom += e.Delta > 0 ? 0.1f : -0.1f;
                zoom = Math.Max(0.2f, Math.Min(5f, zoom));
                gl.Invalidate();
            };

            // picking
            gl.MouseClick += OnPick;

            lblInfo = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                AutoSize = false
            };
            Controls.Add(lblInfo);
        }

        private void Gl_Load(object sender, EventArgs e)
        {
            GL.ClearColor(Color.Black);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.PointSmooth);
            GL.PointSize(3f);
        }

        private void Gl_Resize(object sender, EventArgs e)
        {
            GL.Viewport(0, 0, gl.Width, gl.Height);

            Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                gl.Width / (float)gl.Height,
                1,
                1000
            );

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref proj);
        }

        private void Gl_Paint(object sender, PaintEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 modelview =
                Matrix4.CreateScale(zoom) *
                Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rotX)) *
                Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotY)) *
                Matrix4.LookAt(new Vector3(300, 300, 300), Vector3.Zero, Vector3.UnitY);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);

            GL.Rotate(angle, 1, 1, 0);

            ColorSpaceVisualizer.DrawAxes();
            DrawScene();
            if (points != null &&
    points.Count > 0)
            {
                ShowColorValues(
                    points[0].Value);
            }

            gl.SwapBuffers();
        }

        private void DrawScene()
        {
            Text = "Color Space: " + mode;

            switch (mode)
            {
                case "RGB":
                    points = ColorSpaceVisualizer.DrawRgbCube(imageBitmap);
                    break;

                case "HSV":
                    points = ColorSpaceVisualizer.DrawHsvCylinder(imageBitmap);
                    break;

                case "CMYK":
                    points = ColorSpaceVisualizer.DrawCmykCloud(imageBitmap);
                    break;

                case "YUV":
                    points = ColorSpaceVisualizer.DrawYuvPlane(imageBitmap);
                    break;

                case "Lab":
                    points = ColorSpaceVisualizer.DrawLabSpace(imageBitmap);
                    break;

                case "YCbCr":
                    points = ColorSpaceVisualizer.DrawYCbCrSpace(imageBitmap);
                    break;
            }


        }

        private void OnPick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            int x = e.X;
            int y = gl.Height - e.Y;

            float[] depth = new float[1];
            GL.ReadPixels(x, y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, depth);

            Vector3 world = UnProject(x, y, depth[0]);

            PointData best = null;
            float min = float.MaxValue;

            foreach (var p in points)
            {
                float d = Vector3.Distance(p.Pos, world);
                if (d < min)
                {
                    min = d;
                    best = p;
                }
            }

            if (best != null)
            {
                ShowColorValues(best.Value);

                ColorSelected?.Invoke(
                    best.Value);
            }
        }

        private Vector3 UnProject(float x, float y, float z)
        {
            Matrix4 modelView, projection;

            GL.GetFloat(GetPName.ModelviewMatrix, out modelView);
            GL.GetFloat(GetPName.ProjectionMatrix, out projection);

            Vector4 vec = new Vector4
            {
                X = (2.0f * x) / gl.Width - 1,
                Y = 1 - (2.0f * y) / gl.Height,
                Z = 2.0f * z - 1,
                W = 1
            };

            Matrix4 inv = Matrix4.Invert(modelView * projection);

            Vector4 result = Vector4.Transform(vec, inv);
            result /= result.W;

            return new Vector3(result.X, result.Y, result.Z);
        }

        private void ShowColorValues(ColorValues v)
        {
            string text =
                $"RGB: ({v.R}, {v.G}, {v.B})\n" +
                $"HSV: ({v.H:0}, {v.S:0}%, {v.V:0}%)\n" +
                $"CMYK: ({v.C_val:0.00}, {v.M:0.00}, {v.Y_val:0.00}, {v.K:0.00})\n" +
                $"YUV: ({v.Y_yuv:0}, {v.U:0}, {v.Vu:0})\n" +
                $"Lab: ({v.L_lab:0}, {v.A_lab:0}, {v.B_lab:0})\n" +
                $"YCbCr: ({v.Y_ycbcr:0}, {v.Cb:0}, {v.Cr:0})";

            lblInfo.Text = text;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ColorSpace3DForm
            // 
            this.ClientSize = new System.Drawing.Size(278, 244);
            this.Name = "ColorSpace3DForm";
            this.Load += new System.EventHandler(this.ColorSpace3DForm_Load);
            this.ResumeLayout(false);

        }

        private void ColorSpace3DForm_Load(object sender, EventArgs e)
        {

        }

        public void UpdateImage(Bitmap bmp, string colorMode)
        {
            if (bmp == null)
                return;

            imageBitmap?.Dispose();
            imageBitmap = new Bitmap(bmp);
            mode = colorMode;

            points.Clear();
            gl.MakeCurrent();
            gl.Invalidate();
        }

        ///

    }
}