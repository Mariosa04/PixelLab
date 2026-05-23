using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using PixelLab.Core;

namespace PixelLab.Forms
{
    public partial class MainForm : Form
    {
        private PictureBox workspacePictureBox;
        private Panel controlPanel;
        private ComboBox cmbColorSpace;
        private CheckBox chkC1, chkC2, chkC3, chkC4;
        private TrackBar tbC1, tbC2, tbC3, tbC4;
        private Button btnApply;
        private Image originalImage;

        private Button btnSave;
        private Button btnReset;
        private Label lblInfo;
        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        public MainForm()
        {
            InitializeComponent();
            InitializeWorkspace();
        }

        private void InitializeWorkspace()
        {
            // Setup Control Panel
            controlPanel = new Panel { Dock = DockStyle.Right, Width = 250, BackColor = Color.LightGray };
            this.Controls.Add(controlPanel);

            cmbColorSpace = new ComboBox { Top = 10, Left = 10, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbColorSpace.Items.AddRange(new string[] { "RGB", "CMYK", "HSV", "YUV", "Lab", "YCbCr" });
            cmbColorSpace.SelectedIndex = 0;
            cmbColorSpace.SelectedIndexChanged += (s, e) => UpdateLabels();
            controlPanel.Controls.Add(cmbColorSpace);

            chkC1 = new CheckBox { Top = 50, Left = 10, Text = "Channel 1", Checked = true };
            chkC2 = new CheckBox { Top = 120, Left = 10, Text = "Channel 2", Checked = true };
            chkC3 = new CheckBox { Top = 190, Left = 10, Text = "Channel 3", Checked = true };
            chkC4 = new CheckBox { Top = 260, Left = 10, Text = "Channel 4", Checked = true };
            controlPanel.Controls.Add(chkC1); controlPanel.Controls.Add(chkC2); controlPanel.Controls.Add(chkC3); controlPanel.Controls.Add(chkC4);

            tbC1 = new TrackBar { Top = 70, Left = 10, Width = 200, Minimum = -255, Maximum = 255, Value = 0 };
            tbC2 = new TrackBar { Top = 140, Left = 10, Width = 200, Minimum = -255, Maximum = 255, Value = 0 };
            tbC3 = new TrackBar { Top = 210, Left = 10, Width = 200, Minimum = -255, Maximum = 255, Value = 0 };
            tbC4 = new TrackBar { Top = 280, Left = 10, Width = 200, Minimum = -255, Maximum = 255, Value = 0 };
            controlPanel.Controls.Add(tbC1); controlPanel.Controls.Add(tbC2); controlPanel.Controls.Add(tbC3); controlPanel.Controls.Add(tbC4);

            btnApply = new Button { Top = 350, Left = 10, Text = "Apply Transformation", Width = 200 };
            btnApply.Click += BtnApply_Click;
            controlPanel.Controls.Add(btnApply);

            btnSave = new Button
            {
                Top = 390,
                Left = 10,
                Width = 200,
                Text = "Save Image"
            };

            btnSave.Click += BtnSave_Click;

            controlPanel.Controls.Add(btnSave);

            btnReset = new Button
            {
                Top = 430,
                Left = 10,
                Width = 200,
                Text = "Reset Image"
            };

            btnReset.Click += BtnReset_Click;

            controlPanel.Controls.Add(btnReset);

            lblInfo = new Label
            {
                Top = 480,
                Left = 10,
                Width = 220,
                Height = 100,
                Text = "Image Info",
                BorderStyle = BorderStyle.FixedSingle
            };

            controlPanel.Controls.Add(lblInfo);

            // Setup PictureBox to display the image
            this.workspacePictureBox = new PictureBox();
            this.workspacePictureBox.Dock = DockStyle.Fill;
            this.workspacePictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.workspacePictureBox.BackColor = Color.DarkGray;
            this.Controls.Add(this.workspacePictureBox);
            this.workspacePictureBox.BringToFront();

            // Enable Drag and Drop on the Form
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;

            UpdateLabels();
        }

        private void UpdateLabels()
        {
            chkC4.Visible = false;
            tbC4.Visible = false;

            if (cmbColorSpace.SelectedItem.ToString() == "RGB")
            {
                chkC1.Text = "Red"; chkC2.Text = "Green"; chkC3.Text = "Blue";
                tbC1.Minimum = -255; tbC1.Maximum = 255;
                tbC2.Minimum = -255; tbC2.Maximum = 255;
                tbC3.Minimum = -255; tbC3.Maximum = 255;
            }
            else if (cmbColorSpace.SelectedItem.ToString() == "CMYK")
            {
                chkC1.Text = "Cyan"; chkC2.Text = "Magenta"; chkC3.Text = "Yellow"; chkC4.Text = "Key (Black)";
                chkC4.Visible = true; tbC4.Visible = true;
                tbC1.Minimum = -255; tbC1.Maximum = 255;
                tbC2.Minimum = -255; tbC2.Maximum = 255;
                tbC3.Minimum = -255; tbC3.Maximum = 255;
                tbC4.Minimum = -255; tbC4.Maximum = 255;
            }
            else if (cmbColorSpace.SelectedItem.ToString() == "HSV")
            {
                chkC1.Text = "Hue"; chkC2.Text = "Saturation"; chkC3.Text = "Value (Lightness)";
                tbC1.Minimum = -180; tbC1.Maximum = 180;
                tbC2.Minimum = -100; tbC2.Maximum = 100;
                tbC3.Minimum = -100; tbC3.Maximum = 100;
            }
            else if (cmbColorSpace.SelectedItem.ToString() == "YUV")
            {
                chkC1.Text = "Y (Luma)"; chkC2.Text = "U (Chroma blue)"; chkC3.Text = "V (Chroma red)";
                tbC1.Minimum = -255; tbC1.Maximum = 255;
                tbC2.Minimum = -255; tbC2.Maximum = 255;
                tbC3.Minimum = -255; tbC3.Maximum = 255;
            }
            else if (cmbColorSpace.SelectedItem.ToString() == "YCbCr")
            {
                chkC1.Text = "Y (Luma)"; chkC2.Text = "Cb (Blue difference)"; chkC3.Text = "Cr (Red difference)";
                tbC1.Minimum = -255; tbC1.Maximum = 255;
                tbC2.Minimum = -255; tbC2.Maximum = 255;
                tbC3.Minimum = -255; tbC3.Maximum = 255;
            }
            else if (cmbColorSpace.SelectedItem.ToString() == "Lab")
            {
                chkC1.Text = "L* (Lightness)"; chkC2.Text = "a* (Green-Red)"; chkC3.Text = "b* (Blue-Yellow)";
                tbC1.Minimum = -255; tbC1.Maximum = 255;
                tbC2.Minimum = -255; tbC2.Maximum = 255;
                tbC3.Minimum = -255; tbC3.Maximum = 255;
            }
            tbC1.Value = 0; tbC2.Value = 0; tbC3.Value = 0; tbC4.Value = 0;
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            if (originalImage == null) return;
            string space = cmbColorSpace.SelectedItem.ToString();
            Bitmap result = ColorSpaceConverter.ProcessImage((Bitmap)originalImage, space,
                chkC1.Checked, chkC2.Checked, chkC3.Checked, chkC4.Checked,
                tbC1.Value, tbC2.Value, tbC3.Value, tbC4.Value);

            if (workspacePictureBox.Image != null && workspacePictureBox.Image != originalImage)
                workspacePictureBox.Image.Dispose();

            workspacePictureBox.Image = result;
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                try
                {
                    if (originalImage != null) originalImage.Dispose();
                    originalImage = Image.FromFile(files[0]);

                    if (workspacePictureBox.Image != null && workspacePictureBox.Image != originalImage)
                        workspacePictureBox.Image.Dispose();

                    workspacePictureBox.Image = (Image)originalImage.Clone();
                    FileInfo file = new FileInfo(files[0]);

                    lblInfo.Text =
                        "Width: " + originalImage.Width +
                        "\nHeight: " + originalImage.Height +
                        "\nFormat: " + originalImage.PixelFormat +
                        "\nSize: " + (file.Length / 1024) + " KB";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (workspacePictureBox.Image == null)
                return;

            SaveFileDialog save = new SaveFileDialog();

            save.Filter =
             "JPEG Image|*.jpg|PNG Image|*.png|Bitmap Image|*.bmp";

            if (save.ShowDialog() == DialogResult.OK)
            {
                workspacePictureBox.Image.Save(save.FileName);

                MessageBox.Show("Image Saved Successfully");
            }
        }
        private void BtnReset_Click(object sender, EventArgs e)
        {
            if (originalImage == null)
                return;

            workspacePictureBox.Image =
                (Image)originalImage.Clone();

            tbC1.Value = 0;
            tbC2.Value = 0;
            tbC3.Value = 0;
            tbC4.Value = 0;
        }

    }
}