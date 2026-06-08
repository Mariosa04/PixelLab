using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using NAudio.Wave;
using PixelLab.Core;
using PixelLab.Models;
using PixelLab.Forms;
namespace PixelLab.Forms
{
    public partial class AudioForm : Form
    {
        private Panel controlPanel;
        private Panel audioWorkspaceDisplay;

        private Label lblAudioInfo;
        private Label lblStatus;
        private Label lblAudioTime;

        private Button btnPlay;
        private Button btnPause;
        private Button btnCompress;
        private Button btnDecompress;
        private Button btnCancel;
        private Button btnReset;
        private Button btnSaveReport;

        private ComboBox cmbCompression;
        private NumericUpDown numSampleRate;
        private NumericUpDown numQuantizationLevels;
        private NumericUpDown numDeltaStep;

        private ProgressBar progressBar;
        private TrackBar trackAudioPosition;
        private System.Windows.Forms.Timer playbackTimer;
        private PictureBox pictureWaveform;

        private Chart chartCompressionRatio;
        private Chart chartProcessingSpeed;
        private int chartPointIndex = 0;

        private string currentAudioPath = null;
        private string lastCompressedPath = null;

        private AudioFileReader audioFileReader = null;
        private WaveOutEvent outputDevice = null;

        private bool isUserDraggingTrackBar = false;

        private CancellationTokenSource cancellationTokenSource = null;
        private AudioCompressionResult lastResult = null;

        public AudioForm()
        {
            InitializeComponent();
            InitializeAudioWorkspace();
        }

        private void InitializeAudioWorkspace()
        {
            controlPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 300,
                BackColor = Color.LightGray,
                AutoScroll = true
            };
            this.Controls.Add(controlPanel);

            Label lblPlaybackTitle = new Label
            {
                Top = 15,
                Left = 10,
                Width = 270,
                Text = "معاينة الملف الصوتي:",
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            controlPanel.Controls.Add(lblPlaybackTitle);

            Button btnLoadAudio = new Button
            {
                Top = 890,
                Left = 10,
                Width = 270,
                Height = 30,
                Text = "📂 تحميل ملف صوتي"
            };

            btnLoadAudio.Click += BtnLoadAudio_Click;
            controlPanel.Controls.Add(btnLoadAudio);


            btnPlay = new Button
            {
                Top = 40,
                Left = 10,
                Width = 130,
                Height = 35,
                Text = "▶ تشغيل"
            };
            btnPlay.Click += btnPlay_Click;
            controlPanel.Controls.Add(btnPlay);

            btnPause = new Button
            {
                Top = 40,
                Left = 150,
                Width = 130,
                Height = 35,
                Text = "⏸ إيقاف مؤقت"
            };
            btnPause.Click += btnPause_Click;
            controlPanel.Controls.Add(btnPause);



            trackAudioPosition = new TrackBar
            {
                Top = 80,
                Left = 10,
                Width = 270,
                Minimum = 0,
                Maximum = 1000,
                Value = 0,
                TickStyle = TickStyle.None
            };
            trackAudioPosition.MouseDown += TrackAudioPosition_MouseDown;
            trackAudioPosition.MouseUp += TrackAudioPosition_MouseUp;
            controlPanel.Controls.Add(trackAudioPosition);

            lblAudioTime = new Label
            {
                Top = 105,
                Left = 10,
                Width = 270,
                Height = 20,
                Text = "00:00 / 00:00",
                TextAlign = ContentAlignment.MiddleCenter
            };
            controlPanel.Controls.Add(lblAudioTime);

            playbackTimer = new System.Windows.Forms.Timer();
            playbackTimer.Interval = 300;
            playbackTimer.Tick += PlaybackTimer_Tick;

            Label lblInfoTitle = new Label
            {
                Top = 135,
                Left = 10,
                Width = 270,
                Text = "خصائص الملف الصوتي:",
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            controlPanel.Controls.Add(lblInfoTitle);

            lblAudioInfo = new Label
            {
                Top = 160,
                Left = 10,
                Width = 270,
                Height = 170,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Padding = new Padding(5)
            };
            controlPanel.Controls.Add(lblAudioInfo);

            Label lblCompressTitle = new Label
            {
                Top = 345,
                Left = 10,
                Width = 270,
                Text = "خيارات ضغط الصوت:",
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            controlPanel.Controls.Add(lblCompressTitle);

            cmbCompression = new ComboBox
            {
                Top = 370,
                Left = 10,
                Width = 270,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCompression.Items.AddRange(new string[]
            {
                       "Nonlinear Quantization",
                      "DPCM",
                     "Predictive Differential Coding",
                       "Delta Modulation",
                     "Adaptive Delta Modulation"
            });
            cmbCompression.SelectedIndex = 0;
            controlPanel.Controls.Add(cmbCompression);

            Label lblSampleRate = new Label
            {
                Top = 405,
                Left = 10,
                Width = 270,
                Text = "Sample Rate:"
            };
            controlPanel.Controls.Add(lblSampleRate);

            numSampleRate = new NumericUpDown
            {
                Top = 427,
                Left = 10,
                Width = 270,
                Minimum = 8000,
                Maximum = 96000,
                Value = 44100,
                Increment = 1000
            };
            controlPanel.Controls.Add(numSampleRate);

            Label lblQuantization = new Label
            {
                Top = 460,
                Left = 10,
                Width = 270,
                Text = "Quantization Levels:"
            };
            controlPanel.Controls.Add(lblQuantization);

            numQuantizationLevels = new NumericUpDown
            {
                Top = 482,
                Left = 10,
                Width = 270,
                Minimum = 2,
                Maximum = 256,
                Value = 256
            };
            controlPanel.Controls.Add(numQuantizationLevels);

            Label lblDelta = new Label
            {
                Top = 515,
                Left = 10,
                Width = 270,
                Text = "Delta Step:"
            };
            controlPanel.Controls.Add(lblDelta);

            numDeltaStep = new NumericUpDown
            {
                Top = 537,
                Left = 10,
                Width = 270,
                Minimum = 1,
                Maximum = 10000,
                Value = 500
            };
            controlPanel.Controls.Add(numDeltaStep);

            progressBar = new ProgressBar
            {
                Top = 580,
                Left = 10,
                Width = 270,
                Height = 25
            };
            controlPanel.Controls.Add(progressBar);

            lblStatus = new Label
            {
                Top = 610,
                Left = 10,
                Width = 270,
                Height = 45,
                Text = "جاهز",
                ForeColor = Color.DarkBlue
            };
            controlPanel.Controls.Add(lblStatus);

            btnCompress = new Button
            {
                Top = 665,
                Left = 10,
                Width = 270,
                Height = 35,
                Text = "تنفيذ عملية الضغط",
                BackColor = Color.LightBlue
            };
            btnCompress.Click += BtnCompress_Click;
            controlPanel.Controls.Add(btnCompress);

            btnDecompress = new Button
            {
                Top = 710,
                Left = 10,
                Width = 270,
                Height = 35,
                Text = "فك ضغط الملف"
            };
            btnDecompress.Click += BtnDecompress_Click;
            controlPanel.Controls.Add(btnDecompress);

            btnCancel = new Button
            {
                Top = 755,
                Left = 10,
                Width = 270,
                Height = 35,
                Text = "إلغاء العملية",
                Enabled = false
            };
            btnCancel.Click += BtnCancel_Click;
            controlPanel.Controls.Add(btnCancel);

            btnReset = new Button
            {
                Top = 800,
                Left = 10,
                Width = 270,
                Height = 35,
                Text = "إعادة ضبط"
            };
            btnReset.Click += BtnReset_Click;
            controlPanel.Controls.Add(btnReset);

            btnSaveReport = new Button
            {
                Top = 845,
                Left = 10,
                Width = 270,
                Height = 35,
                Text = "حفظ التقرير",
                Enabled = false
            };
            btnSaveReport.Click += BtnSaveReport_Click;
            controlPanel.Controls.Add(btnSaveReport);

            audioWorkspaceDisplay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.DarkGray
            };

            this.Controls.Add(audioWorkspaceDisplay);
            audioWorkspaceDisplay.BringToFront();

            InitializeCharts();

            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;

            this.Size = new Size(950, 600);
            this.Text = "تطبيق ضغط وتحليل الملفات الصوتية";
        }

        private void InitializeCharts()
        {
            audioWorkspaceDisplay.Controls.Clear();

            Label lblDragDropHint = new Label
            {
                Text = "قم بسحب وإفلات الملف الصوتي هنا",
                Dock = DockStyle.Top,
                Height = 70,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Arial", 14, FontStyle.Bold)
            };
            audioWorkspaceDisplay.Controls.Add(lblDragDropHint);

            pictureWaveform = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            audioWorkspaceDisplay.Controls.Add(pictureWaveform);
            pictureWaveform.BringToFront();

            Panel chartsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.DarkGray
            };
            audioWorkspaceDisplay.Controls.Add(chartsPanel);
            chartsPanel.BringToFront();

            chartCompressionRatio = CreateChart("نسبة الضغط أثناء التنفيذ", "Step", "Compression Ratio");
            chartCompressionRatio.Dock = DockStyle.Top;
            chartCompressionRatio.Height = 210;
            chartsPanel.Controls.Add(chartCompressionRatio);

            chartProcessingSpeed = CreateChart("سرعة المعالجة", "Step", "KB/s");
            chartProcessingSpeed.Dock = DockStyle.Fill;
            chartsPanel.Controls.Add(chartProcessingSpeed);
        }

       

       


        private Chart CreateChart(string title, string xTitle, string yTitle)
        {
            Chart chart = new Chart();
            chart.BackColor = Color.WhiteSmoke;

            ChartArea area = new ChartArea("MainArea");
            area.AxisX.Title = xTitle;
            area.AxisY.Title = yTitle;
            area.AxisX.MajorGrid.LineColor = Color.LightGray;
            area.AxisY.MajorGrid.LineColor = Color.LightGray;

            chart.ChartAreas.Add(area);

            Series series = new Series("Data");
            series.ChartType = SeriesChartType.Line;
            series.BorderWidth = 2;
            series.ChartArea = "MainArea";

            chart.Series.Add(series);
            chart.Titles.Add(title);

            return chart;
        }

        private void DrawWaveform(string audioPath)
        {
            try
            {
                int width = pictureWaveform.Width;
                int height = pictureWaveform.Height;

                if (width <= 0)
                    width = 700;

                if (height <= 0)
                    height = 120;

                Bitmap bitmap = new Bitmap(width, height);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Black);

                    using (AudioFileReader reader = new AudioFileReader(audioPath))
                    {
                        float[] buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];

                        int samplesRead = reader.Read(buffer, 0, buffer.Length);

                        if (samplesRead <= 0)
                        {
                            pictureWaveform.Image = bitmap;
                            return;
                        }

                        using (Pen wavePen = new Pen(Color.Lime, 1))
                        using (Pen centerPen = new Pen(Color.DarkGray, 1))
                        {
                            int centerY = height / 2;
                            g.DrawLine(centerPen, 0, centerY, width, centerY);

                            for (int x = 0; x < width; x++)
                            {
                                int startIndex = (int)((x / (double)width) * samplesRead);
                                int endIndex = (int)(((x + 1) / (double)width) * samplesRead);

                                if (endIndex <= startIndex)
                                    endIndex = startIndex + 1;

                                if (endIndex > samplesRead)
                                    endIndex = samplesRead;

                                float min = 0;
                                float max = 0;

                                for (int i = startIndex; i < endIndex; i++)
                                {
                                    float sample = buffer[i];

                                    if (sample < min)
                                        min = sample;

                                    if (sample > max)
                                        max = sample;
                                }

                                int yMin = centerY - (int)(min * centerY);
                                int yMax = centerY - (int)(max * centerY);

                                g.DrawLine(wavePen, x, yMin, x, yMax);
                            }
                        }
                    }
                }

                if (pictureWaveform.Image != null)
                    pictureWaveform.Image.Dispose();

                pictureWaveform.Image = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Waveform Error: " + ex.Message);
            }
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            if (audioFileReader == null)
                return;

            if (!isUserDraggingTrackBar)
            {
                double totalSeconds = audioFileReader.TotalTime.TotalSeconds;
                double currentSeconds = audioFileReader.CurrentTime.TotalSeconds;

                if (totalSeconds > 0)
                {
                    int value = (int)((currentSeconds / totalSeconds) * trackAudioPosition.Maximum);

                    if (value < trackAudioPosition.Minimum)
                        value = trackAudioPosition.Minimum;

                    if (value > trackAudioPosition.Maximum)
                        value = trackAudioPosition.Maximum;

                    trackAudioPosition.Value = value;
                }

                lblAudioTime.Text =
                    audioFileReader.CurrentTime.ToString(@"mm\:ss") +
                    " / " +
                    audioFileReader.TotalTime.ToString(@"mm\:ss");
            }
        }

        private void TrackAudioPosition_MouseDown(object sender, MouseEventArgs e)
        {
            isUserDraggingTrackBar = true;
        }

        private void TrackAudioPosition_MouseUp(object sender, MouseEventArgs e)
        {
            if (audioFileReader != null)
            {
                double percent = trackAudioPosition.Value / (double)trackAudioPosition.Maximum;
                double targetSeconds = audioFileReader.TotalTime.TotalSeconds * percent;

                audioFileReader.CurrentTime = TimeSpan.FromSeconds(targetSeconds);
            }

            isUserDraggingTrackBar = false;
        }

        private void ResetCharts()
        {
            chartPointIndex = 0;

            if (chartCompressionRatio != null)
                chartCompressionRatio.Series["Data"].Points.Clear();

            if (chartProcessingSpeed != null)
                chartProcessingSpeed.Series["Data"].Points.Clear();
        }

        private void AddChartPoint(AudioProgressInfo info)
        {
            if (info == null)
                return;

            chartPointIndex++;

            if (chartCompressionRatio != null)
                chartCompressionRatio.Series["Data"].Points.AddXY(chartPointIndex, info.CompressionRatio);

            if (chartProcessingSpeed != null)
                chartProcessingSpeed.Series["Data"].Points.AddXY(chartPointIndex, info.ProcessingSpeedKbPerSecond);
        }

        private async void BtnCompress_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentAudioPath))
            {
                MessageBox.Show("يرجى إدخال ملف صوتي أولاً.");
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "PixelLab Compressed Audio (*.plca)|*.plca";
            saveDialog.FileName = Path.GetFileNameWithoutExtension(currentAudioPath) + "_compressed.plca";

            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;

            lastCompressedPath = saveDialog.FileName;

            AudioCompressionSettings settings = BuildSettings();

            cancellationTokenSource = new CancellationTokenSource();

            SetBusyState(true);

            Progress<AudioProgressInfo> progress = new Progress<AudioProgressInfo>(UpdateProgress);

            AudioCompressionEngine engine = new AudioCompressionEngine();

            lastResult = await Task.Run(() =>
                engine.Compress(
                    currentAudioPath,
                    lastCompressedPath,
                    settings,
                    progress,
                    cancellationTokenSource.Token));

            SetBusyState(false);
            btnSaveReport.Enabled = lastResult != null;

            MessageBox.Show(lastResult.Message);
            ReportForm report =
                new ReportForm(lastResult);

            report.ShowDialog();

        }

        private async void BtnDecompress_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "PixelLab Compressed Audio (*.plca)|*.plca";

            if (openDialog.ShowDialog() != DialogResult.OK)
                return;

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Wave Audio (*.wav)|*.wav";
            saveDialog.FileName = Path.GetFileNameWithoutExtension(openDialog.FileName) + "_decompressed.wav";

            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;

            cancellationTokenSource = new CancellationTokenSource();

            SetBusyState(true);

            Progress<AudioProgressInfo> progress = new Progress<AudioProgressInfo>(UpdateProgress);

            AudioDecompressionEngine engine = new AudioDecompressionEngine();

            lastResult = await Task.Run(() =>
                engine.Decompress(
                    openDialog.FileName,
                    saveDialog.FileName,
                    progress,
                    cancellationTokenSource.Token));

            SetBusyState(false);
            btnSaveReport.Enabled = lastResult != null;

            MessageBox.Show(lastResult.Message);

            ReportForm report =
                    new ReportForm(lastResult);

            report.ShowDialog();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null)
                cancellationTokenSource.Cancel();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            CleanUpAudio();

            currentAudioPath = null;
            lastCompressedPath = null;
            lastResult = null;

            lblAudioInfo.Text = "";
            lblStatus.Text = "تمت إعادة الضبط";
            progressBar.Value = 0;

            cmbCompression.SelectedIndex = 0;
            numSampleRate.Value = 44100;
            numQuantizationLevels.Value = 256;
            numDeltaStep.Value = 500;

            ResetCharts();

            if (pictureWaveform != null && pictureWaveform.Image != null)
            {
                pictureWaveform.Image.Dispose();
                pictureWaveform.Image = null;
            }

            btnSaveReport.Enabled = false;
        }

        private void BtnSaveReport_Click(object sender, EventArgs e)
        {
            if (lastResult == null)
            {
                MessageBox.Show("لا يوجد تقرير لحفظه.");
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Text Report (*.txt)|*.txt";
            saveDialog.FileName = "AudioCompressionReport.txt";

            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;

            AudioCompressionReport report = new AudioCompressionReport
            {
                Result = lastResult
            };

            File.WriteAllText(saveDialog.FileName, report.BuildTextReport());

            MessageBox.Show("تم حفظ التقرير بنجاح.");
        }

        private AudioCompressionSettings BuildSettings()
        {
            AudioAlgorithmType type = AudioAlgorithmType.NonlinearQuantization;

            if (cmbCompression.SelectedIndex == 0)
                type = AudioAlgorithmType.NonlinearQuantization;
            else if (cmbCompression.SelectedIndex == 1)
                type = AudioAlgorithmType.Dpcm;
            else if (cmbCompression.SelectedIndex == 2)
                type = AudioAlgorithmType.PredictiveDifferentialCoding;
            else if (cmbCompression.SelectedIndex == 3)
                type = AudioAlgorithmType.DeltaModulation;
            else if (cmbCompression.SelectedIndex == 4)
                type = AudioAlgorithmType.AdaptiveDeltaModulation;
           

            return new AudioCompressionSettings
            {
                AlgorithmType = type,
                TargetSampleRate = (int)numSampleRate.Value,
                QuantizationLevels = (int)numQuantizationLevels.Value,
                BitsPerSample = 8,
                DeltaStep = (int)numDeltaStep.Value,
                NormalizeBeforeCompression = true
            };
        }

        private void UpdateProgress(AudioProgressInfo info)
        {
            if (info == null)
                return;

            int value = info.ProgressPercentage;

            if (value < 0)
                value = 0;

            if (value > 100)
                value = 100;

            progressBar.Value = value;

            lblStatus.Text =
                info.StatusMessage + Environment.NewLine +
                "Progress: " + value + "% | Ratio: " +
                info.CompressionRatio.ToString("F2") +
                " | Speed: " +
                info.ProcessingSpeedKbPerSecond.ToString("F2") +
                " KB/s";

            AddChartPoint(info);
        }

        private void SetBusyState(bool busy)
        {
            btnCompress.Enabled = !busy;
            btnDecompress.Enabled = !busy;
            btnCancel.Enabled = busy;
            btnReset.Enabled = !busy;
            btnPlay.Enabled = !busy;
            btnPause.Enabled = !busy;
            cmbCompression.Enabled = !busy;
            numSampleRate.Enabled = !busy;
            numQuantizationLevels.Enabled = !busy;
            numDeltaStep.Enabled = !busy;
            trackAudioPosition.Enabled = !busy;

            if (busy)
            {
                progressBar.Value = 0;
                lblStatus.Text = "جاري التنفيذ...";
                ResetCharts();
            }
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

            if (files == null || files.Length == 0)
                return;

            string targetFile = files[0];
            string extension = Path.GetExtension(targetFile).ToLower();

            if (extension == ".wav" || extension == ".mp3" || extension == ".aif" || extension == ".aiff")
            {
                try
                {
                    CleanUpAudio();
                    currentAudioPath = targetFile;
                    UpdateAudioInfo(currentAudioPath);
                    DrawWaveform(currentAudioPath);
                    lblStatus.Text = "تم تحميل الملف بنجاح";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading audio file: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Unsupported file type. Please upload WAV, MP3, AIF, or AIFF.");
            }
        }

        private void UpdateAudioInfo(string audioPath)
        {
            FileInfo fileInfo = new FileInfo(audioPath);

            using (AudioFileReader reader = new AudioFileReader(audioPath))
            {
                AudioInfo info = new AudioInfo
                {
                    FileName = fileInfo.Name,
                    Format = fileInfo.Extension.Replace(".", "").ToUpper(),
                    FileSizeBytes = fileInfo.Length,
                    Duration = reader.TotalTime,
                    SampleRate = reader.WaveFormat.SampleRate,
                    Channels = reader.WaveFormat.Channels,
                    BitsPerSample = reader.WaveFormat.BitsPerSample,
                    EncodingType = reader.WaveFormat.Encoding.ToString()
                };

                lblAudioInfo.Text = info.ToString();
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentAudioPath))
                return;

            try
            {
                if (outputDevice == null)
                {
                    outputDevice = new WaveOutEvent();
                    audioFileReader = new AudioFileReader(currentAudioPath);
                    outputDevice.Init(audioFileReader);
                }

                if (outputDevice.PlaybackState != PlaybackState.Playing)
                {
                    outputDevice.Play();

                    if (playbackTimer != null)
                        playbackTimer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Playback Error: " + ex.Message);
            }
        }

        private void BtnLoadAudio_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Audio Files|*.wav;*.mp3;*.aif;*.aiff",
                Title = "اختر ملف صوتي"
            };

            if (openDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                CleanUpAudio();

                currentAudioPath = openDialog.FileName;

                UpdateAudioInfo(currentAudioPath);
                DrawWaveform(currentAudioPath);

                lblStatus.Text = "تم تحميل الملف بنجاح";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading audio file: " + ex.Message);
            }
        }


        private void btnPause_Click(object sender, EventArgs e)
        {
            if (outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing)
            {
                outputDevice.Pause();

                if (playbackTimer != null)
                    playbackTimer.Stop();
            }
        }

        private void CleanUpAudio()
        {
            if (playbackTimer != null)
                playbackTimer.Stop();

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

            if (trackAudioPosition != null)
                trackAudioPosition.Value = 0;

            if (lblAudioTime != null)
                lblAudioTime.Text = "00:00 / 00:00";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CleanUpAudio();

            if (cancellationTokenSource != null)
                cancellationTokenSource.Cancel();

            if (pictureWaveform != null && pictureWaveform.Image != null)
            {
                pictureWaveform.Image.Dispose();
                pictureWaveform.Image = null;
            }

            base.OnFormClosing(e);
        }

        private void AudioForm_Load(object sender, EventArgs e)
        {

        }
    }
}