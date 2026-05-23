using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelLab.Models
{
    public class ColorValues
    {
        public byte R, G, B;
        public float H, S, V;           // H:0-360, S,V:0-100
        public float C_val, M, Y_val, K; // 0-100
        public float Y_yuv, U, Vu;      // YUV
        public float L_lab, A_lab, B_lab;
        public float Y_ycbcr, Cb, Cr;
    }
}
