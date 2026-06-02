using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelLab.Models
{
    public class AudioInfo
    {
        public string FileName { get; set; }
        public string Format { get; set; }
        public long FileSizeBytes { get; set; }
        public TimeSpan Duration { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public int BitsPerSample { get; set; }
        public string EncodingType { get; set; }

        public override string ToString() =>
            $"File Name: {FileName}\n" +
            $"Format: {Format}\n" +
            $"File Size: {FileSizeBytes / (1024.0 * 1024.0):F2} MB\n" +
            $"Duration: {Duration:mm\\:ss\\.ff}\n" +
            $"Sample Rate: {SampleRate} Hz\n" +
            $"Channels: {Channels} ({(Channels == 1 ? "Mono" : "Stereo")})\n" +
            $"Bit Depth: {BitsPerSample} bits\n" +
            $"Encoding: {EncodingType}\n";
    }
}
