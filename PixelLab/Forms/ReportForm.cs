using System;
using System.Drawing;
using System.Windows.Forms;
using PixelLab.Models;

namespace PixelLab.Forms
{
    public class ReportForm : Form
    {
        public ReportForm(AudioCompressionResult result)
        {
            Text = "Compression Report";

            Width = 800;
            Height = 500;

            StartPosition =
                FormStartPosition.CenterParent;

            TextBox txtReport =
                new TextBox();

            txtReport.Multiline = true;
            txtReport.ReadOnly = true;
            txtReport.ScrollBars =
                ScrollBars.Vertical;

            txtReport.Dock = DockStyle.Fill;

            txtReport.Font =
                new Font("Consolas", 11);

            txtReport.Text =
                "========== COMPRESSION REPORT ==========\r\n\r\n" +

                "Original File:\r\n" +
                result.OriginalFilePath +

                "\r\n\r\nCompressed File:\r\n" +
                result.CompressedFilePath +

                "\r\n\r\nDecompressed File:\r\n" +
                result.DecompressedFilePath +

                "\r\n\r\nOriginal Size:\r\n" +
                (result.OriginalSizeBytes / 1024.0).ToString("F2") +
                " KB" +

                "\r\n\r\nCompressed Size:\r\n" +
                (result.CompressedSizeBytes / 1024.0).ToString("F2") +
                " KB" +

                "\r\n\r\nSaving Percentage:\r\n" +
                result.SavingPercentage.ToString("F2") +
                "%" +

                "\r\n\r\nProcessing Time:\r\n" +
                result.ElapsedTime.TotalSeconds.ToString("F2") +
                " Seconds" +

                "\r\n\r\nAlgorithm:\r\n" +
                result.Settings.AlgorithmType +

                "\r\n\r\nSuccess:\r\n" +
                result.Success +

                "\r\n\r\nMessage:\r\n" +
                result.Message;

            Controls.Add(txtReport);
        }
    }
}