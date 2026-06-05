using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PixelLab.Models;

namespace PixelLab.Core
{
    public class AudioCompressionEngine
    {
        private const string Magic = "PIXLAB_AUDIO_COMPRESSED_V2";
        private const int FileVersion = 2;

        public AudioCompressionResult Compress(
            string inputPath,
            string outputPath,
            AudioCompressionSettings settings,
            IProgress<AudioProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(inputPath))
                throw new ArgumentException("Input path is empty.");

            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Audio file not found.", inputPath);

            if (settings == null)
                settings = new AudioCompressionSettings();

            FileInfo originalFileInfo = new FileInfo(inputPath);
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                using (AudioFileReader reader = new AudioFileReader(inputPath))
                {
                    ISampleProvider sampleProvider = reader;

                    int outputSampleRate = reader.WaveFormat.SampleRate;
                    int channels = reader.WaveFormat.Channels;

                    if (settings.TargetSampleRate > 0 &&
                        settings.TargetSampleRate != reader.WaveFormat.SampleRate)
                    {
                        sampleProvider = new WdlResamplingSampleProvider(reader, settings.TargetSampleRate);
                        outputSampleRate = settings.TargetSampleRate;
                    }

                    using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    using (BinaryWriter writer = new BinaryWriter(fileStream))
                    {
                        long sampleCountPosition = WriteHeader(
                            writer,
                            settings,
                            outputSampleRate,
                            channels,
                            originalFileInfo.Name,
                            originalFileInfo.Length);

                        long totalSamples = 0;

                        switch (settings.AlgorithmType)
                        {
                            
                            case AudioAlgorithmType.NonlinearQuantization:
                                totalSamples = CompressNonlinearQuantization(sampleProvider, writer, reader, originalFileInfo.Length, settings, progress, cancellationToken, stopwatch);
                                break;

                            case AudioAlgorithmType.Dpcm:
                                totalSamples = CompressDpcm(sampleProvider, writer, reader, originalFileInfo.Length, settings, channels, progress, cancellationToken, stopwatch);
                                break;

                            case AudioAlgorithmType.PredictiveDifferentialCoding:
                                totalSamples = CompressPredictiveDifferentialCoding(sampleProvider, writer, reader, originalFileInfo.Length, settings, channels, progress, cancellationToken, stopwatch);
                                break;

                            case AudioAlgorithmType.DeltaModulation:
                                totalSamples = CompressDeltaModulation(sampleProvider, writer, reader, originalFileInfo.Length, settings, channels, progress, cancellationToken, stopwatch);
                                break;

                            case AudioAlgorithmType.AdaptiveDeltaModulation:
                                totalSamples = CompressAdaptiveDeltaModulation(sampleProvider, writer, reader, originalFileInfo.Length, settings, channels, progress, cancellationToken, stopwatch);
                                break;

                            default:
                                throw new NotSupportedException("Selected audio algorithm is not supported.");
                        }

                        writer.Flush();

                        fileStream.Seek(sampleCountPosition, SeekOrigin.Begin);
                        writer.Write(totalSamples);
                        writer.Flush();
                    }
                }

                stopwatch.Stop();

                FileInfo compressedInfo = new FileInfo(outputPath);

                double savingPercentage = 0;
                if (originalFileInfo.Length > 0)
                    savingPercentage = (1.0 - ((double)compressedInfo.Length / originalFileInfo.Length)) * 100.0;

                return new AudioCompressionResult
                {
                    OriginalFilePath = inputPath,
                    CompressedFilePath = outputPath,
                    OriginalSizeBytes = originalFileInfo.Length,
                    CompressedSizeBytes = compressedInfo.Length,
                    SavingPercentage = savingPercentage,
                    ElapsedTime = stopwatch.Elapsed,
                    Settings = settings,
                    Success = true,
                    Message = "Compression completed successfully."
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();

                if (File.Exists(outputPath))
                    File.Delete(outputPath);

                return new AudioCompressionResult
                {
                    OriginalFilePath = inputPath,
                    CompressedFilePath = outputPath,
                    OriginalSizeBytes = originalFileInfo.Length,
                    CompressedSizeBytes = 0,
                    SavingPercentage = 0,
                    ElapsedTime = stopwatch.Elapsed,
                    Settings = settings,
                    Success = false,
                    Message = "Compression canceled by user."
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                return new AudioCompressionResult
                {
                    OriginalFilePath = inputPath,
                    CompressedFilePath = outputPath,
                    OriginalSizeBytes = originalFileInfo.Length,
                    CompressedSizeBytes = File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0,
                    SavingPercentage = 0,
                    ElapsedTime = stopwatch.Elapsed,
                    Settings = settings,
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        private long WriteHeader(
            BinaryWriter writer,
            AudioCompressionSettings settings,
            int sampleRate,
            int channels,
            string originalFileName,
            long originalSizeBytes)
        {
            writer.Write(Magic);
            writer.Write(FileVersion);
            writer.Write((int)settings.AlgorithmType);
            writer.Write(sampleRate);
            writer.Write(channels);
            writer.Write(16);
            writer.Write(settings.QuantizationLevels);
            writer.Write(settings.BitsPerSample);
            writer.Write(settings.DeltaStep);
            writer.Write(settings.NormalizeBeforeCompression);
            writer.Write(originalFileName ?? string.Empty);
            writer.Write(originalSizeBytes);

            long sampleCountPosition = writer.BaseStream.Position;
            writer.Write((long)0);

            return sampleCountPosition;
        }

        private long CompressNonlinearQuantization(
            ISampleProvider sampleProvider,
            BinaryWriter writer,
            AudioFileReader reader,
            long originalSizeBytes,
            AudioCompressionSettings settings,
            IProgress<AudioProgressInfo> progress,
            CancellationToken cancellationToken,
            Stopwatch stopwatch)
        {
            float[] buffer = new float[4096];
            int read;
            long totalSamples = 0;

            while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (int i = 0; i < read; i++)
                {
                    short pcm = FloatToPcm16(buffer[i]);
                    byte code = EncodeNonlinear(pcm, settings.QuantizationLevels);
                    writer.Write(code);
                    totalSamples++;
                }

                ReportProgress(progress, reader, originalSizeBytes, writer.BaseStream.Length, stopwatch, "Applying Nonlinear Quantization...");
            }

            return totalSamples;
        }

        private long CompressDpcm(
            ISampleProvider sampleProvider,
            BinaryWriter writer,
            AudioFileReader reader,
            long originalSizeBytes,
            AudioCompressionSettings settings,
            int channels,
            IProgress<AudioProgressInfo> progress,
            CancellationToken cancellationToken,
            Stopwatch stopwatch)
        {
            float[] buffer = new float[4096];

            short[] previousByChannel = new short[channels];
            bool[] hasPrevious = new bool[channels];

            int read;
            long totalSamples = 0;

            while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (int i = 0; i < read; i++)
                {
                    int channelIndex = (int)(totalSamples % channels);
                    short current = FloatToPcm16(buffer[i]);

                    if (!hasPrevious[channelIndex])
                    {
                        writer.Write(current);
                        previousByChannel[channelIndex] = current;
                        hasPrevious[channelIndex] = true;
                    }
                    else
                    {
                        int difference = current - previousByChannel[channelIndex];

                        byte code = QuantizeSignedValue(
                            difference,
                            -32768,
                            32767,
                            settings.QuantizationLevels);

                        writer.Write(code);

                        int decodedDifference = DequantizeSignedValue(
                            code,
                            -32768,
                            32767,
                            settings.QuantizationLevels);

                        int reconstructed = previousByChannel[channelIndex] + decodedDifference;
                        previousByChannel[channelIndex] = ClampToShort(reconstructed);
                    }

                    totalSamples++;
                }

                ReportProgress(progress, reader, originalSizeBytes, writer.BaseStream.Length, stopwatch, "Applying DPCM...");
            }

            return totalSamples;
        }

        private long CompressPredictiveDifferentialCoding(
            ISampleProvider sampleProvider,
            BinaryWriter writer,
            AudioFileReader reader,
            long originalSizeBytes,
            AudioCompressionSettings settings,
            int channels,
            IProgress<AudioProgressInfo> progress,
            CancellationToken cancellationToken,
            Stopwatch stopwatch)
        {
            float[] buffer = new float[4096];

            short[] previous1 = new short[channels];
            short[] previous2 = new short[channels];
            int[] countByChannel = new int[channels];

            int read;
            long totalSamples = 0;

            while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (int i = 0; i < read; i++)
                {
                    int channelIndex = (int)(totalSamples % channels);
                    short current = FloatToPcm16(buffer[i]);

                    if (countByChannel[channelIndex] < 2)
                    {
                        writer.Write(current);

                        previous2[channelIndex] = previous1[channelIndex];
                        previous1[channelIndex] = current;
                        countByChannel[channelIndex]++;
                    }
                    else
                    {
                        int predicted = (2 * previous1[channelIndex]) - previous2[channelIndex];
                        predicted = ClampToShort(predicted);

                        int error = current - predicted;

                        byte code = QuantizeSignedValue(
                            error,
                            -32768,
                            32767,
                            settings.QuantizationLevels);

                        writer.Write(code);

                        int decodedError = DequantizeSignedValue(
                            code,
                            -32768,
                            32767,
                            settings.QuantizationLevels);

                        short reconstructed = ClampToShort(predicted + decodedError);

                        previous2[channelIndex] = previous1[channelIndex];
                        previous1[channelIndex] = reconstructed;
                    }

                    totalSamples++;
                }

                ReportProgress(progress, reader, originalSizeBytes, writer.BaseStream.Length, stopwatch, "Applying Predictive Differential Coding...");
            }

            return totalSamples;
        }

        private long CompressDeltaModulation(
            ISampleProvider sampleProvider,
            BinaryWriter writer,
            AudioFileReader reader,
            long originalSizeBytes,
            AudioCompressionSettings settings,
            int channels,
            IProgress<AudioProgressInfo> progress,
            CancellationToken cancellationToken,
            Stopwatch stopwatch)
        {
            float[] buffer = new float[4096];

            short[] previousByChannel = new short[channels];
            bool[] hasPrevious = new bool[channels];

            BitWriter bitWriter = new BitWriter(writer);

            int read;
            long totalSamples = 0;

            while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (int i = 0; i < read; i++)
                {
                    int channelIndex = (int)(totalSamples % channels);
                    short current = FloatToPcm16(buffer[i]);

                    if (!hasPrevious[channelIndex])
                    {
                        writer.Write(current);
                        previousByChannel[channelIndex] = current;
                        hasPrevious[channelIndex] = true;
                    }
                    else
                    {
                        bool bit = current >= previousByChannel[channelIndex];
                        bitWriter.WriteBit(bit);

                        int reconstructed = previousByChannel[channelIndex];

                        if (bit)
                            reconstructed += settings.DeltaStep;
                        else
                            reconstructed -= settings.DeltaStep;

                        previousByChannel[channelIndex] = ClampToShort(reconstructed);
                    }

                    totalSamples++;
                }

                ReportProgress(progress, reader, originalSizeBytes, writer.BaseStream.Length, stopwatch, "Applying Delta Modulation...");
            }

            bitWriter.Flush();

            return totalSamples;
        }

        private long CompressAdaptiveDeltaModulation(
            ISampleProvider sampleProvider,
            BinaryWriter writer,
            AudioFileReader reader,
            long originalSizeBytes,
            AudioCompressionSettings settings,
            int channels,
            IProgress<AudioProgressInfo> progress,
            CancellationToken cancellationToken,
            Stopwatch stopwatch)
        {
            float[] buffer = new float[4096];

            short[] previousByChannel = new short[channels];
            bool[] hasPrevious = new bool[channels];
            int[] stepByChannel = new int[channels];
            bool[] lastBitByChannel = new bool[channels];
            bool[] hasLastBitByChannel = new bool[channels];

            for (int i = 0; i < channels; i++)
                stepByChannel[i] = settings.DeltaStep;

            BitWriter bitWriter = new BitWriter(writer);

            int read;
            long totalSamples = 0;

            while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (int i = 0; i < read; i++)
                {
                    int channelIndex = (int)(totalSamples % channels);
                    short current = FloatToPcm16(buffer[i]);

                    if (!hasPrevious[channelIndex])
                    {
                        writer.Write(current);
                        previousByChannel[channelIndex] = current;
                        hasPrevious[channelIndex] = true;
                    }
                    else
                    {
                        bool bit = current >= previousByChannel[channelIndex];
                        bitWriter.WriteBit(bit);

                        int step = stepByChannel[channelIndex];
                        int reconstructed = previousByChannel[channelIndex];

                        if (bit)
                            reconstructed += step;
                        else
                            reconstructed -= step;

                        previousByChannel[channelIndex] = ClampToShort(reconstructed);

                        if (hasLastBitByChannel[channelIndex] && lastBitByChannel[channelIndex] == bit)
                            stepByChannel[channelIndex] = Math.Min(stepByChannel[channelIndex] + Math.Max(1, settings.DeltaStep / 8), 10000);
                        else
                            stepByChannel[channelIndex] = Math.Max(1, stepByChannel[channelIndex] - Math.Max(1, settings.DeltaStep / 16));

                        lastBitByChannel[channelIndex] = bit;
                        hasLastBitByChannel[channelIndex] = true;
                    }

                    totalSamples++;
                }

                ReportProgress(progress, reader, originalSizeBytes, writer.BaseStream.Length, stopwatch, "Applying Adaptive Delta Modulation...");
            }

            bitWriter.Flush();

            return totalSamples;
        }

        private void ReportProgress(
            IProgress<AudioProgressInfo> progress,
            AudioFileReader reader,
            long originalSizeBytes,
            long compressedBytes,
            Stopwatch stopwatch,
            string status)
        {
            if (progress == null)
                return;

            int percentage = 0;

            if (reader.Length > 0)
            {
                percentage = (int)((reader.Position * 100.0) / reader.Length);

                if (percentage < 0)
                    percentage = 0;

                if (percentage > 100)
                    percentage = 100;
            }

            double compressionRatio = 0;
            if (compressedBytes > 0)
                compressionRatio = (double)originalSizeBytes / compressedBytes;

            double speed = 0;
            if (stopwatch.Elapsed.TotalSeconds > 0)
                speed = (reader.Position / 1024.0) / stopwatch.Elapsed.TotalSeconds;

            progress.Report(new AudioProgressInfo
            {
                ProgressPercentage = percentage,
                OriginalBytesProcessed = reader.Position,
                CompressedBytesProduced = compressedBytes,
                CompressionRatio = compressionRatio,
                ProcessingSpeedKbPerSecond = speed,
                StatusMessage = status
            });
        }

        private void ForwardDct(double[] input, double[] output, int size)
        {
            double factor = Math.PI / size;

            for (int k = 0; k < size; k++)
            {
                double sum = 0;

                for (int n = 0; n < size; n++)
                    sum += input[n] * Math.Cos(factor * (n + 0.5) * k);

                double scale = k == 0 ? Math.Sqrt(1.0 / size) : Math.Sqrt(2.0 / size);
                output[k] = sum * scale;
            }
        }

        private short FloatToPcm16(float sample)
        {
            if (sample > 1f)
                sample = 1f;

            if (sample < -1f)
                sample = -1f;

            return (short)(sample * 32767f);
        }

        private byte EncodeNonlinear(short pcm, int levels)
        {
            if (levels < 2)
                levels = 2;

            if (levels > 256)
                levels = 256;

            double x = pcm / 32768.0;
            double mu = levels - 1;

            double sign = x < 0 ? -1.0 : 1.0;
            double magnitude = Math.Abs(x);

            double compressed =
                sign * Math.Log(1.0 + mu * magnitude) / Math.Log(1.0 + mu);

            int code = (int)Math.Round(((compressed + 1.0) / 2.0) * (levels - 1));

            if (code < 0)
                code = 0;

            if (code > levels - 1)
                code = levels - 1;

            return (byte)code;
        }

        private byte QuantizeSignedValue(int value, int min, int max, int levels)
        {
            if (levels < 2)
                levels = 2;

            if (levels > 256)
                levels = 256;

            if (value < min)
                value = min;

            if (value > max)
                value = max;

            double normalized = (value - min) / (double)(max - min);
            int code = (int)Math.Round(normalized * (levels - 1));

            if (code < 0)
                code = 0;

            if (code > levels - 1)
                code = levels - 1;

            return (byte)code;
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

        private class BitWriter
        {
            private readonly BinaryWriter writer;
            private byte currentByte;
            private int bitCount;

            public BitWriter(BinaryWriter writer)
            {
                this.writer = writer;
                currentByte = 0;
                bitCount = 0;
            }

            public void WriteBit(bool bit)
            {
                if (bit)
                    currentByte |= (byte)(1 << bitCount);

                bitCount++;

                if (bitCount == 8)
                    FlushCurrentByte();
            }

            public void Flush()
            {
                if (bitCount > 0)
                    FlushCurrentByte();
            }

            private void FlushCurrentByte()
            {
                writer.Write(currentByte);
                currentByte = 0;
                bitCount = 0;
            }
        }
    }
}