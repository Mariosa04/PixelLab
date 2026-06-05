using System;

namespace PixelLab.Models
{
    public class AudioCompressionResult
    {
        public string OriginalFilePath { get; set; }

        public string CompressedFilePath { get; set; }

        public string DecompressedFilePath { get; set; }

        public long OriginalSizeBytes { get; set; }

        public long CompressedSizeBytes { get; set; }

        public double SavingPercentage { get; set; }

        public TimeSpan ElapsedTime { get; set; }

        public AudioCompressionSettings Settings { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }
    }
}