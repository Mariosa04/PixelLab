using System;
using System.Text;

namespace PixelLab.Models
{
    public class AudioCompressionReport
    {
        public AudioCompressionResult Result { get; set; }

        public string BuildTextReport()
        {
            if (Result == null)
                return "No compression result available.";

            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Audio Compression Report");
            builder.AppendLine("========================");
            builder.AppendLine();

            builder.AppendLine("Original File:");
            builder.AppendLine(Result.OriginalFilePath);
            builder.AppendLine();

            builder.AppendLine("Compressed File:");
            builder.AppendLine(Result.CompressedFilePath);
            builder.AppendLine();

            builder.AppendLine("Original Size: " + FormatBytes(Result.OriginalSizeBytes));
            builder.AppendLine("Compressed Size: " + FormatBytes(Result.CompressedSizeBytes));
            builder.AppendLine("Saving Percentage: " + Result.SavingPercentage.ToString("F2") + " %");
            builder.AppendLine("Elapsed Time: " + Result.ElapsedTime.TotalSeconds.ToString("F2") + " seconds");
            builder.AppendLine();

            builder.AppendLine("Compression Settings:");
            if (Result.Settings != null)
                builder.AppendLine(Result.Settings.ToString());

            builder.AppendLine();
            builder.AppendLine("Status: " + (Result.Success ? "Success" : "Failed"));
            builder.AppendLine("Message: " + Result.Message);

            return builder.ToString();
        }

        private string FormatBytes(long bytes)
        {
            double kb = bytes / 1024.0;
            double mb = kb / 1024.0;

            if (mb >= 1)
                return mb.ToString("F2") + " MB";

            return kb.ToString("F2") + " KB";
        }
    }
}