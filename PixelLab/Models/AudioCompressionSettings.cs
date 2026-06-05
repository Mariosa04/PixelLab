using System;

namespace PixelLab.Models
{
    public class AudioCompressionSettings
    {
        public AudioAlgorithmType AlgorithmType { get; set; }

        public int TargetSampleRate { get; set; }

        public int QuantizationLevels { get; set; }

        public int BitsPerSample { get; set; }

        public int DeltaStep { get; set; }

        public bool NormalizeBeforeCompression { get; set; }

        public AudioCompressionSettings()
        {
            AlgorithmType = AudioAlgorithmType.NonlinearQuantization;
            TargetSampleRate = 44100;
            QuantizationLevels = 256;
            BitsPerSample = 8;
            DeltaStep = 500;
            NormalizeBeforeCompression = true;
        }

        public override string ToString()
        {
            return
                "Algorithm: " + AlgorithmType + Environment.NewLine +
                "Target Sample Rate: " + TargetSampleRate + " Hz" + Environment.NewLine +
                "Quantization Levels: " + QuantizationLevels + Environment.NewLine +
                "Bits Per Sample: " + BitsPerSample + Environment.NewLine +
                "Delta Step: " + DeltaStep + Environment.NewLine +
                "Normalize: " + NormalizeBeforeCompression;
        }
    }
}