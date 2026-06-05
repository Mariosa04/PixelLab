namespace PixelLab.Models
{
    public class AudioProgressInfo
    {
        public int ProgressPercentage { get; set; }

        public long OriginalBytesProcessed { get; set; }

        public long CompressedBytesProduced { get; set; }

        public double CompressionRatio { get; set; }

        public double ProcessingSpeedKbPerSecond { get; set; }

        public string StatusMessage { get; set; }
    }
}