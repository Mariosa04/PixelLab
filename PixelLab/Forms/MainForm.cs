using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using PixelLab.Core;

namespace PixelLab.Forms
{
    public partial class MainForm : Form
    {
        private Button btnOpenImage;
        private PictureBox workspacePictureBox;
        private Panel controlPanel;
        private ComboBox cmbColorSpace;
        private CheckBox chkC1, chkC2, chkC3, chkC4;
        private TrackBar tbC1, tbC2, tbC3, tbC4;
        private Image originalImage;
        private ComboBox cmbQuantization;
        private Label lblImageInfo;
        private Button btnReset;
        private Button btnSaveImage;
        private ComboBox cmbCompression;
        private Bitmap currentImage;

        public MainForm()
        {
            InitializeComponent();
            InitializeWorkspace();
        }

        private void InitializeWorkspace()
        {

            
            controlPanel = new Panel { Dock = DockStyle.Right, Width = 250, BackColor = Color.LightGray ,AutoScroll =true};
            this.Controls.Add(controlPanel);

            cmbColorSpace = new ComboBox { Top = 10, Left = 10, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbColorSpace.Items.AddRange(new string[] { "RGB", "CMYK", "HSV", "YUV", "Lab", "YCbCr" });
            cmbColorSpace.SelectedIndex = 0;
            cmbColorSpace.SelectedIndexChanged += (s, e) => UpdateLabels();
            cmbColorSpace.SelectedIndexChanged += ColorSpaceChanged;
            cmbColorSpace.DropDownClosed += UpdatePreview;
            controlPanel.Controls.Add(cmbColorSpace);

            chkC1 = new CheckBox { Top = 50, Left = 10, Text = "Channel 1", Checked = true };
            chkC2 = new CheckBox { Top = 120, Left = 10, Text = "Channel 2", Checked = true };
            chkC3 = new CheckBox { Top = 190, Left = 10, Text = "Channel 3", Checked = true };
            chkC4 = new CheckBox { Top = 260, Left = 10, Text = "Channel 4", Checked = true };
            chkC1.CheckedChanged += UpdatePreview;
            chkC2.CheckedChanged += UpdatePreview;
            chkC3.CheckedChanged += UpdatePreview;
            chkC4.CheckedChanged += UpdatePreview;
            controlPanel.Controls.Add(chkC1);
            controlPanel.Controls.Add(chkC2);
            controlPanel.Controls.Add(chkC3);
            controlPanel.Controls.Add(chkC4);

            tbC1 = new TrackBar { Top = 70, Left = 10, Width = 200, Minimum = -255, Maximum = 255, Value = 0 };
            tbC2 = new TrackBar { Top = 140, Left = 10, Width = 200, Minimum = -255, Maximum = 255, Value = 0 };
            tbC3 = new TrackBar { Top = 210, Left = 10, Width = 200, Minimum = -255, Maximum = 255, Value = 0 };
            tbC4 = new TrackBar { Top = 280, Left = 10, Width = 200, Minimum = -255, Maximum = 255, Value = 0 };
            tbC1.Scroll += UpdatePreview;
            tbC2.Scroll += UpdatePreview;
            tbC3.Scroll += UpdatePreview;
            tbC3.Scroll += UpdatePreview;
            controlPanel.Controls.Add(tbC1);
            controlPanel.Controls.Add(tbC2);
            controlPanel.Controls.Add(tbC3);
            controlPanel.Controls.Add(tbC4);


            Label lable = new Label { Top = 260, Left = 10, Width = 200, Text = "number of color" };
            controlPanel.Controls.Add(lable);
            cmbQuantization = new ComboBox {Top = 280,Left = 10,Width = 200,DropDownStyle = ComboBoxStyle.DropDownList};
            cmbQuantization.Items.AddRange(new object[]{"256","128","64","32","16","8","4"});
            cmbQuantization.SelectedIndex = 0;
            cmbQuantization.DropDownClosed += UpdatePreview;
            controlPanel.Controls.Add(cmbQuantization);
           

            // Setup PictureBox to display the image
            this.workspacePictureBox = new PictureBox();
            this.workspacePictureBox.Dock = DockStyle.Fill;
            this.workspacePictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.workspacePictureBox.BackColor = Color.DarkGray;
            this.Controls.Add(this.workspacePictureBox);
            this.workspacePictureBox.BringToFront();



            //
            Label lable1 = new Label { Top = 320, Left = 10, Width = 200, Text = "number of color" };
            controlPanel.Controls.Add(lable1);
            lblImageInfo = new Label{Top = 350,Left = 10,Width = 220,Height = 120,BorderStyle = BorderStyle.FixedSingle,AutoSize = false};
            controlPanel.Controls.Add(lblImageInfo);


            //
            btnOpenImage = new Button { Top = 510, Left = 10, Width = 200, Height = 35, Text = "Open Image" };
            btnOpenImage.Click += BtnOpenImage_Click;
            controlPanel.Controls.Add(btnOpenImage);
          
         

            // Enable Drag and Drop on the Form
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;
            this.Size = new Size(800, 900);
            UpdateLabels();
        }


        private void BtnOpenImage_Click(object sender,EventArgs e)
        {
            OpenFileDialog dialog =new OpenFileDialog();

            dialog.Title = "Choose Image";

            dialog.Filter ="Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff";

            dialog.Multiselect = false;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (originalImage != null)
                        originalImage.Dispose();

                    originalImage =
                        Image.FromFile(dialog.FileName);
                    UpdateImageInfo(dialog.FileName);
                    if (workspacePictureBox.Image != null)
                        workspacePictureBox.Image.Dispose();
                    currentImage =
                        (Bitmap)originalImage.Clone();

                    workspacePictureBox.Image =
                        (Bitmap)currentImage.Clone();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Error loading image:\n" + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

       
        //////////////////////////////////////////
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
          //  tbC1.Value = 0; tbC2.Value = 0; tbC3.Value = 0; tbC4.Value = 0;
        }
        private void ColorSpaceChanged(object sender, EventArgs e)
        {
            UpdateLabels();

            if (currentImage == null)
                return;

            workspacePictureBox.Image?.Dispose();

            workspacePictureBox.Image =
                (Bitmap)currentImage.Clone();
        }

        //////////////////////////////////////
        private void UpdatePreview(object sender, EventArgs e)
        {
            if (currentImage == null)
                return;

            string space =
                cmbColorSpace.SelectedItem.ToString();

            Bitmap baseImage =
                (Bitmap)currentImage.Clone();

            // Apply color modifications
            Bitmap result =
                ColorSpaceConverter.ProcessImage(
                    baseImage,
                    space,
                    chkC1.Checked,
                    chkC2.Checked,
                    chkC3.Checked,
                    chkC4.Checked,
                    tbC1.Value,
                    tbC2.Value,
                    tbC3.Value,
                    tbC4.Value
                );

            // Quantization
            int levels =
                int.Parse(
                    cmbQuantization.SelectedItem.ToString());

            result =
                ColorQuantizer.Quantize(
                    result,
                    levels);

            // Save state
            currentImage?.Dispose();
            currentImage =
                (Bitmap)result.Clone();

            // Display
            workspacePictureBox.Image?.Dispose();

            workspacePictureBox.Image =
                (Bitmap)result.Clone();
        }
        ///////////////////////////////////////
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }
        /////////////////////////////////////
        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                try
                {
                    if (originalImage != null) originalImage.Dispose();
                    originalImage = Image.FromFile(files[0]);
                    UpdateImageInfo(files[0]);
                    if (workspacePictureBox.Image != null && workspacePictureBox.Image != originalImage)
                        workspacePictureBox.Image.Dispose();

                    currentImage =(Bitmap)originalImage.Clone();

                    workspacePictureBox.Image =(Bitmap)currentImage.Clone();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        //
        private void UpdateImageInfo(string imagePath)
        {
            FileInfo file = new FileInfo(imagePath);


            using (Bitmap bmp = new Bitmap(imagePath))
            {
                

                Models.ImageInfo info =new Models.ImageInfo
                    {
                        FileName = file.Name,
                        Format = file.Extension.Replace(".", "").ToUpper(),
                        FileSizeBytes = file.Length,
                        Width = bmp.Width,
                        Height = bmp.Height,

                    };

                lblImageInfo.Text = info.ToString();
            }
        }
        ///
       

       
     
    }
}
