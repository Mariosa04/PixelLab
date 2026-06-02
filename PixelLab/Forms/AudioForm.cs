using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;

namespace PixelLab.Forms
{
    public partial class AudioForm : Form
    {


        private Panel controlPanel;
        private Panel audioWorkspaceDisplay;
        private Label lblAudioInfo; // الـ Label المطلوب لعرض الخصائص
        private Button btnPlay;
        private Button btnPause;
        private ComboBox cmbCompression;
        private Button btnCompress;

        private string currentAudioPath = null;
        private AudioFileReader audioFileReader = null; // From page 4/5 [cite: 20, 34]
        private WaveOutEvent outputDevice = null;       // From page 4 [cite: 24]

        public AudioForm()
        {
            InitializeComponent();
            InitializeAudioWorkspace();
        }


        private void InitializeAudioWorkspace()
        {
            // 1. لوحة التحكم الجانبية (يمين الواجهة)
            controlPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 260,
                BackColor = Color.LightGray,
                AutoScroll = true
            };
            this.Controls.Add(controlPanel);

            // 2. عنوان التشغيل والمعاينة
            Label lblPlaybackTitle = new Label { Top = 15, Left = 10, Width = 240, Text = "معاينة الملف الصوتي:", Font = new Font("Arial", 9, FontStyle.Bold) };
            controlPanel.Controls.Add(lblPlaybackTitle);

            btnPlay = new Button { Top = 40, Left = 10, Width = 110, Height = 35, Text = "▶ تشغيل" };
            btnPlay.Click += btnPlay_Click;
            controlPanel.Controls.Add(btnPlay);

            btnPause = new Button { Top = 40, Left = 130, Width = 110, Height = 35, Text = "⏸ إيقاف مؤقت" };
            btnPause.Click += btnPause_Click;
            controlPanel.Controls.Add(btnPause);

            // 3. الـ Label المطلوب لعرض خصائص الملف الصوتي تلقائياً
            Label lblInfoTitle = new Label { Top = 95, Left = 10, Width = 240, Text = "خصائص الملف الصوتي:", Font = new Font("Arial", 9, FontStyle.Bold) };
            controlPanel.Controls.Add(lblInfoTitle);

            lblAudioInfo = new Label
            {
                Top = 120,
                Left = 10,
                Width = 240,
                Height = 180,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Padding = new Padding(5),
                RightToLeft = RightToLeft.Yes // لدعم القراءة باللغة العربية بشكل صحيح
            };
            controlPanel.Controls.Add(lblAudioInfo);

            // 4. خيارات ضغط الملفات الصوتية (المطلوب 3)
            Label lblCompressTitle = new Label { Top = 315, Left = 10, Width = 240, Text = "خيارات ضغط الصوت:", Font = new Font("Arial", 9, FontStyle.Bold) };
            controlPanel.Controls.Add(lblCompressTitle);

            cmbCompression = new ComboBox { Top = 340, Left = 10, Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCompression.Items.AddRange(new string[] { "Normal (بدون ضغط)", "RLE Audio Method", "DCT Audio Method" });
            cmbCompression.SelectedIndex = 0;
            controlPanel.Controls.Add(cmbCompression);

            btnCompress = new Button { Top = 380, Left = 10, Width = 230, Height = 35, Text = "تنفيذ عملية الضغط", BackColor = Color.LightBlue };
            btnCompress.Click += BtnCompress_Click;
            controlPanel.Controls.Add(btnCompress);

            // 5. مساحة العمل المركزية (للسحب والإفالت وعرض الملف)
            audioWorkspaceDisplay = new Panel();
            audioWorkspaceDisplay.Dock = DockStyle.Fill;
            audioWorkspaceDisplay.BackColor = Color.DarkGray;
            this.Controls.Add(audioWorkspaceDisplay);
            audioWorkspaceDisplay.BringToFront();

            // إضافة نص توضيحي داخل مساحة العمل
            Label lblDragDropHint = new Label
            {
                Text = "قم بسحب وإفلات الملف الصوتي هنا",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Arial", 14, FontStyle.Bold)
            };
            audioWorkspaceDisplay.Controls.Add(lblDragDropHint);

            // 6. تفعيل خاصية السحب والإفالت (Drag and Drop) على الواجهة
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;

            this.Size = new Size(800, 500);
            this.Text = "تطبيق ضغط وتحليل الملفات الصوتية";
        }

        private void BtnCompress_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
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
                string targetFile = files[0];
                string extension = Path.GetExtension(targetFile).ToLower();

                // Simple check to process audio formats [cite: 49]
                if (extension == ".wav" || extension == ".mp3" || extension == ".aif" || extension == ".ogg")
                {
                    try
                    {
                        // Clean up old instances before loading a new track
                        CleanUpAudio();

                        currentAudioPath = targetFile;

                        // Automatically update interface UI info properties
                        UpdateAudioInfo(currentAudioPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading audio file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Unsupported file type format. Please upload an audio file.", "Format Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void UpdateAudioInfo(string audioPath)
        {
            FileInfo fileInfo = new FileInfo(audioPath);

            // Using AudioFileReader to securely dissect audio metadata [cite: 34]
            using (var reader = new AudioFileReader(audioPath))
            {
                Models.AudioInfo info = new Models.AudioInfo
                {
                    FileName = fileInfo.Name,
                    Format = fileInfo.Extension.Replace(".", "").ToUpper(),
                    FileSizeBytes = fileInfo.Length,
                    Duration = reader.TotalTime,                      // Page 5 [cite: 38]
                    SampleRate = reader.WaveFormat.SampleRate,        // Page 5 [cite: 39]
                    Channels = reader.WaveFormat.Channels,            // Page 5 [cite: 41]
                    BitsPerSample = reader.WaveFormat.BitsPerSample,  // Page 5 question [cite: 45]
                    EncodingType = reader.WaveFormat.Encoding.ToString()
                };

                // Render metrics to your UI label string placeholder
                lblAudioInfo.Text = info.ToString();
            }
        }

        // Action binding for a Preview/Play GUI Button [cite: 23]
        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentAudioPath)) return;

            try
            {
                if (outputDevice == null)
                {
                    outputDevice = new WaveOutEvent(); // Page 4 [cite: 24]
                    audioFileReader = new AudioFileReader(currentAudioPath);
                    outputDevice.Init(audioFileReader); // Page 4 [cite: 26]
                }

                if (outputDevice.PlaybackState != PlaybackState.Playing)
                {
                    outputDevice.Play(); // Page 4 [cite: 27]
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Playback Error: " + ex.Message);
            }
        }

        // Action binding for a Pause GUI Button
        private void btnPause_Click(object sender, EventArgs e)
        {
            if (outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing)
            {
                outputDevice.Pause();
            }
        }

        private void CleanUpAudio()
        {
            if (outputDevice != null)
            {
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
            }
            if (audioFileReader != null)
            {
                audioFileReader.Dispose();
                audioFileReader = null;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CleanUpAudio();
            base.OnFormClosing(e);
        }
    }
}
