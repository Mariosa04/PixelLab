using System;
using System.IO;
using System.Threading;
using NAudio.Wave;
using PixelLab.Models;

namespace PixelLab.Core
{
    public class AudioDecompressionEngine
    {
        private const string Magic = "PIXLAB_AUDIO_COMPRESSED_V2";

        public AudioCompressionResult Decompress(
            string compressedPath,
            string outputWavPath,
            IProgress<AudioProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            FileInfo compressedInfo = new FileInfo(compressedPath);
            DateTime start = DateTime.Now;

            try
            {
                using (FileStream fileStream = new FileStream(compressedPath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    string magic = reader.ReadString();

                    if (magic != Magic)
                        throw new InvalidDataException("Invalid PixelLab compressed audio file.");

                    int version = reader.ReadInt32();
                    AudioAlgorithmType algorithm = (AudioAlgorithmType)reader.ReadInt32();

                    int sampleRate = reader.ReadInt32();
                    int channels = reader.ReadInt32();
                    int bitsPerSample = reader.ReadInt32();

                    int quantizationLevels = reader.ReadInt32();
                    int settingsBitsPerSample = reader.ReadInt32();
                    int deltaStep = reader.ReadInt32();
                    bool normalize = reader.ReadBoolean();

                    string originalFileName = reader.ReadString();
                    long originalSizeBytes = reader.ReadInt64();
                    long totalSamples = reader.ReadInt64();

                    WaveFormat waveFormat = new WaveFormat(sampleRate, bitsPerSample, channels);

                    using (WaveFileWriter waveWriter = new WaveFileWriter(outputWavPath, waveFormat))
                    {
                        switch (algorithm)
                        {
                           
                            case AudioAlgorithmType.NonlinearQuantization:
                                DecompressNonlinearQuantization(reader, waveWriter, fileStream, totalSamples, quantizationLevels, progress, cancellationToken);
                                break;

                            case AudioAlgorithmType.Dpcm:
                                DecompressDpcm(reader, waveWriter, fileStream, totalSamples, channels, quantizationLevels, progress, cancellationToken);
                                break;

                            case AudioAlgorithmType.PredictiveDifferentialCoding:
                                DecompressPredictiveDifferentialCoding(reader, waveWriter, fileStream, totalSamples, channels, quantizationLevels, progress, cancellationToken);
                                break;

                            case AudioAlgorithmType.DeltaModulation:
                                DecompressDeltaModulation(reader, waveWriter, fileStream, totalSamples, channels, deltaStep, progress, cancellationToken);
                                break;

                            case AudioAlgorithmType.AdaptiveDeltaModulation:
                                DecompressAdaptiveDeltaModulation(reader, waveWriter, fileStream, totalSamples, channels, deltaStep, progress, cancellationToken);
                                break;

                            default:
                                throw new NotSupportedException("Unsupported compressed audio algorithm.");
                        }
                    }

                    return new AudioCompressionResult
                    {
                        OriginalFilePath = compressedPath,
                        DecompressedFilePath = outputWavPath,
                        OriginalSizeBytes = compressedInfo.Length,
                        CompressedSizeBytes = new FileInfo(outputWavPath).Length,
                        ElapsedTime = DateTime.Now - start,
                        Settings = new AudioCompressionSettings
                        {
                            AlgorithmType = algorithm,
                            TargetSampleRate = sampleRate,
                            QuantizationLevels = quantizationLevels,
                            BitsPerSample = settingsBitsPerSample,
                            DeltaStep = deltaStep,
                            NormalizeBeforeCompression = normalize
                        },
                        Success = true,
                        Message = "Decompression completed successfully."
                    };
                }
            }
            catch (OperationCanceledException)
            {
                if (File.Exists(outputWavPath))
                    File.Delete(outputWavPath);

                return new AudioCompressionResult
                {
                    OriginalFilePath = compressedPath,
                    DecompressedFilePath = outputWavPath,
                    OriginalSizeBytes = compressedInfo.Length,
                    CompressedSizeBytes = 0,
                    ElapsedTime = DateTime.Now - start,
                    Success = false,
                    Message = "Decompression canceled by user."
                };
            }
            catch (Exception ex)
            {
                return new AudioCompressionResult
                {
                    OriginalFilePath = compressedPath,
                    DecompressedFilePath = outputWavPath,
                    OriginalSizeBytes = compressedInfo.Length,
                    CompressedSizeBytes = File.Exists(outputWavPath) ? new FileInfo(outputWavPath).Length : 0,
                    ElapsedTime = DateTime.Now - start,
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        private void DecompressNonlinearQuantization(
            BinaryReader reader,
            WaveFileWriter waveWriter,
            FileStream fileStream,
            long totalSamples,
            int levels,
            IProgress<AudioProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            for (long i = 0; i < totalSamples; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                byte code = reader.ReadByte();
                short sample = DecodeNonlinear(code, levels);
                WritePcm16(waveWriter, sample);

                if (i % 4096 == 0)
                    ReportProgress(progress, fileStream, "Decompressing Nonlinear Quantization...");
            }
        }

        private void DecompressDpcm(
            BinaryReader reader,
            WaveFileWriter waveWriter,
            FileStream fileStream,
            long totalSamples,
            int channels,
            int levels,
            IProgress<AudioProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            short[] previousByChannel = new short[channels];

            for (long i = 0; i < totalSamples; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int channelIndex = (int)(i % channels);
                short reconstructed;

                if (i < channels)
                {
                    reconstructed = reader.ReadInt16();
                    previousByChannel[channelIndex] = reconstructed;
                }
                else
                {
                    byte code = reader.ReadByte();
                    int difference = DequantizeSignedValue(code, -32768, 32767, levels);
                    reconstructed = ClampToShort(previousByChannel[channelIndex] + difference);
                    previousByChannel[channelIndex] = reconstructed;
                }

                WritePcm16(waveWriter, reconstructed);

                if (i % 4096 == 0)
                    ReportProgress(progress, fileStream, "Decompressing DPCM...");
            }
        }

        private void DecompressPredictiveDifferentialCoding(
            BinaryReader reader,
            WaveFileWriter waveWriter,
            FileStream fileStream,
            long totalSamples,
            int channels,
            int levels,
            IProgress<AudioProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            short[] previous1 = new short[channels];
            short[] previous2 = new short[channels];
            int[] countByChannel = new int[channels];

            for (long i = 0; i < totalSamples; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int channelIndex = (int)(i % channels);
                short reconstructed;

                if (countByChannel[channelIndex] < 2)
                {
                    reconstructed = reader.ReadInt16();

                    previous2[channelIndex] = previous1[channelIndex];
                    previous1[channelIndex] = reconstructed;
                    countByChannel[channelIndex]++;
                }
                else
                {
                    int predicted = (2 * previous1[channelIndex]) - previous2[channelIndex];
                    predicted = ClampToShort(predicted);

                    byte code = reader.ReadByte();
                    int error = DequantizeSignedValue(code, -32768, 32767, levels);

                    reconstructed = ClampToShort(predicted + error);

                    previous2[channelIndex] = previous1[channelIndex];
                    previous1[channelIndex] = reconstructed;
                }

                WritePcm16(waveWriter, reconstructed);

                if (i % 4096 == 0)
                    ReportProgress(progress, fileStream, "Decompressing Predictive Differential Coding...");
            }
        }

        private void DecompressDeltaModulation(
            BinaryReader reader,
            WaveFileWriter waveWriter,
            FileStream fileStream,
            long totalSamples,
            int channels,
            int deltaStep,
            IProgress<AudioProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            short[] previousByChannel = new short[channels];
            BitReader bitReader = new BitReader(reader);

            for (long i = 0; i < totalSamples; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int channelIndex = (int)(i % channels);
                short reconstructed;

                if (i < channels)
                {
                    reconstructed = reader.ReadInt16();
                    previousByChannel[channelIndex] = reconstructed;
                }
                else
                {
                    bool bit = bitReader.ReadBit();

                    int value = previousByChannel[channelIndex];

                    if (bit)
                        value += deltaStep;
                    else
                        value -= deltaStep;

                    reconstructed = ClampToShort(value);
                    previousByChannel[channelIndex] = reconstructed;
                }

                WritePcm16(waveWriter, reconstructed);

                if (i % 4096 == 0)
                    ReportProgress(progress, fileStream, "Decompressing Delta Modulation...");
            }
        }

        private void DecompressAdaptiveDeltaModulation(
            BinaryReader reader,
            WaveFileWriter waveWriter,
            FileStream fileStream,
            long totalSamples,
            int channels,
            int initialDeltaStep,
            IProgress<AudioProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            short[] previousByChannel = new short[channels];
            int[] stepByChannel = new int[channels];
            bool[] lastBitByChannel = new bool[channels];
            bool[] hasLastBitByChannel = new bool[channels];

            for (int i = 0; i < channels; i++)
                stepByChannel[i] = initialDeltaStep;

            BitReader bitReader = new BitReader(reader);

            for (long i = 0; i < totalSamples; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int channelIndex = (int)(i % channels);
                short reconstructed;

                if (i < channels)
                {
                    reconstructed = reader.ReadInt16();
                    previousByChannel[channelIndex] = reconstructed;
                }
                else
                {
                    bool bit = bitReader.ReadBit();

                    int step = stepByChannel[channelIndex];
                    int value = previousByChannel[channelIndex];

                    if (bit)
                        value += step;
                    else
                        value -= step;

                    reconstructed = ClampToShort(value);
                    previousByChannel[channelIndex] = reconstructed;

                    if (hasLastBitByChannel[channelIndex] && lastBitByChannel[channelIndex] == bit)
                        stepByChannel[channelIndex] = Math.Min(stepByChannel[channelIndex] + Math.Max(1, initialDeltaStep / 8), 10000);
                    else
                        stepByChannel[channelIndex] = Math.Max(1, stepByChannel[channelIndex] - Math.Max(1, initialDeltaStep / 16));

                    lastBitByChannel[channelIndex] = bit;
                    hasLastBitByChannel[channelIndex] = true;
                }

                WritePcm16(waveWriter, reconstructed);

                if (i % 4096 == 0)
                    ReportProgress(progress, fileStream, "Decompressing Adaptive Delta Modulation...");
            }
        }

        private void InverseDct(double[] input, double[] output, int size)
        {
            double factor = Math.PI / size;

            for (int n = 0; n < size; n++)
            {
                double sum = 0;

                for (int k = 0; k < size; k++)
                {
                    double scale = k == 0 ? Math.Sqrt(1.0 / size) : Math.Sqrt(2.0 / size);
                    sum += scale * input[k] * Math.Cos(factor * (n + 0.5) * k);
                }

                output[n] = sum;
            }
        }

        private short DecodeNonlinear(byte code, int levels)
        {
            if (levels < 2)
                levels = 2;

            if (levels > 256)
                levels = 256;

            double mu = levels - 1;
            double compressed = (code / (double)(levels - 1)) * 2.0 - 1.0;

            double sign = compressed < 0 ? -1.0 : 1.0;
            double magnitude = Math.Abs(compressed);

            double expanded =
                sign * (1.0 / mu) * (Math.Pow(1.0 + mu, magnitude) - 1.0);

            return ClampToShort((int)(expanded * 32767.0));
        }

        private int DequantizeSignedValue(byte code, int min, int max, int levels)
        {
            if (levels < 2)
                levels = 2;

            if (levels > 256)
                levels = 256;

            double normalized = code / (double)(levels - 1);
            return (int)Math.Round(min + normalized * (max - min));
        }

        private short ClampToShort(int value)
        {
            if (value > short.MaxValue)
                return short.MaxValue;

            if (value < short.MinValue)
                return short.MinValue;

            return (short)value;
        }

        private void WritePcm16(WaveFileWriter writer, short sample)
        {
            byte[] bytes = BitConverter.GetBytes(sample);
            writer.Write(bytes, 0, bytes.Length);
        }

        private void ReportProgress(
            IProgress<AudioProgressInfo> progress,
            FileStream fileStream,
            string status)
        {
            if (progress == null)
                return;

            int percentage = 0;

            if (fileStream.Length > 0)
            {
                percentage = (int)((fileStream.Position * 100.0) / fileStream.Length);

                if (percentage < 0)
                    percentage = 0;

                if (percentage > 100)
                    percentage = 100;
            }

            progress.Report(new AudioProgressInfo
            {
                ProgressPercentage = percentage,
                OriginalBytesProcessed = fileStream.Position,
                CompressedBytesProduced = 0,
                CompressionRatio = 0,
                ProcessingSpeedKbPerSecond = 0,
                StatusMessage = status
            });
        }

        private class BitReader
        {
            private readonly BinaryReader reader;
            private byte currentByte;
            private int bitIndex;

            public BitReader(BinaryReader reader)
            {
                this.reader = reader;
                currentByte = 0;
                bitIndex = 8;
            }

            public bool ReadBit()
            {
                if (bitIndex >= 8)
                {
                    currentByte = reader.ReadByte();
                    bitIndex = 0;
                }

                bool bit = (currentByte & (1 << bitIndex)) != 0;
                bitIndex++;

                return bit;
            }
        }
    }
}