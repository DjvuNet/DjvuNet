using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Wavelet
{
    /// <summary>
    /// This data structure gathers the quality specification parameters needed
    /// for encoding each chunk of an IW44 file.Chunk data is generated until
    /// meeting either the slice target, the size target or the decibel target.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class InterWaveEncoderSettings
    {
        public InterWaveEncoderSettings()
        {
        }

        /// <summary>
        /// Slice target. Data generation for the current chunk stops if the total
        /// number of slices (in this chunk and all the previous chunks) reaches
        /// value Slices. The default value 0 has a special meaning: data will
        /// be generated regardless of the number of slices in the file.
        /// </summary>
        public int Slices;

        /// <summary>
        /// Size target. Data generation for the current chunk stops if the total
        /// data size(in this chunk and all the previous chunks), expressed in
        /// bytes, reaches value Size.  The default value 0 has a special
        /// meaning: data will be generated regardless of the file size.
        /// </summary>
        public int Bytes;

        /// <summary>
        /// Decibel target. Data generation for the current chunk stops if the
        /// estimated luminance error, expressed in decibels, reaches value
        /// Decibel. The default value #0# has a special meaning: data will be
        /// generated regardless of the estimated luminance error. Specifying value
        /// 0 in fact shortcuts the computation of the estimated luminance error
        /// and sensibly speeds up the encoding process.
        /// </summary>
        public float Decibels;
    }
}
