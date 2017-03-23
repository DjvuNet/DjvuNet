using System;
using System.IO;

namespace DjvuNet.Interfaces
{
    public interface ICodec
    {
        /// <summary> 
        /// Query if this is image data.  Any data which will effects Map data should return true.
        /// </summary>
        /// <returns> 
        /// true if effects image data
        /// </returns>
        bool ImageData
        {
            get;
        }

        /// <summary>
        /// Initialize the object from the specified data.
        /// </summary>
        /// <param name="pool">
        /// data to decode
        /// </param>
        /// <throws>  IOException if an error occurs </throws>
        void Decode(BinaryReader pool);
    }
}