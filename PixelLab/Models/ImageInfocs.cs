using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelLab.Models
{
    public class ImageInfo
    {
        public string FileName { get; set; }
        public string Format { get; set; }
        public long FileSizeBytes { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public override string ToString() =>
            $"File: {FileName} \n " +
            $" Format: {Format}  \n" +
            $"Size: {Width}x{Height} \n " +
            $"Storage: {FileSizeBytes / 1024.0:F1} KB \n";
    }
}
