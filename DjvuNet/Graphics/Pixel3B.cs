using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DjvuNet.Graphics
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct Pixel3B
    {
        #region Private Members

        [FieldOffset(0)]
        fixed byte _Color[3];

        [FieldOffset(0)]
        byte _Blue;

        [FieldOffset(1)]
        byte _Green;

        [FieldOffset(2)]
        byte _Red;

        #endregion Private Members

        #region Public Static Properties

        #region TotalColorsPerPixel

        /// <summary>
        /// Gets the total number of colors in a pixel
        /// </summary>
        public const int TotalColorsPerPixel = 3;

        #endregion TotalColorsPerPixel

        #region WhitePixel

        /// <summary>
        /// Gets a white pixel
        /// </summary>
        public static Pixel WhitePixel
        {
            get { return new Pixel(-1, -1, -1); }
        }

        #endregion WhitePixel

        #region BlackPixel

        /// <summary>
        /// Gets a black pixel
        /// </summary>
        public static Pixel BlackPixel
        {
            get { return new Pixel(0, 0, 0); }
        }

        #endregion BlackPixel

        #region BluePixel

        /// <summary>
        /// Gets a blue pixel
        /// </summary>
        public static Pixel BluePixel
        {
            get { return new Pixel(-1, 0, 0); }
        }

        #endregion BluePixel

        #region GreenPixel

        /// <summary>
        /// Gets a green pixel
        /// </summary>
        public static Pixel GreenPixel
        {
            get { return new Pixel(0, -1, 0); }
        }

        #endregion GreenPixel

        #region RedPixel

        /// <summary>
        /// Gets a red pixel
        /// </summary>
        public static Pixel RedPixel
        {
            get { return new Pixel(0, 0, -1); }
        }

        #endregion RedPixel

        #endregion Public Static Properties

        #region Public Properties

        #region Blue

        /// <summary>
        /// Gets or sets the blue value for the pixel
        /// </summary>
        public byte Blue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _Blue; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _Blue = value; }
        }

        #endregion Blue

        #region Green

        /// <summary>
        /// Gets or sets the green value for the pixel
        /// </summary>
        public byte Green
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _Green; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _Green = value; }
        }

        #endregion Green

        #region Red

        /// <summary>
        /// Gets or sets the red value for the pixel
        /// </summary>
        public byte Red
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _Red; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _Red = value; }
        }

        #endregion Red

        #endregion Public Properties

        #region Constructors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Pixel3B(int color = 0)
        {
            _Blue = unchecked((byte)(color >> 16));
            _Green = unchecked((byte)(color >> 8));
            _Red = unchecked((byte)(color));
        }

        /// <summary> Creates a new Pixel object.
        ///
        /// </summary>
        /// <param name="blue">pixel value
        /// </param>
        /// <param name="green">pixel value
        /// </param>
        /// <param name="red">pixel value
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Pixel3B(byte blue, byte green = 0, byte red = 0)
        {
            _Blue = blue;
            _Green = green;
            _Red = red;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Create a clone of this pixel.
        /// </summary>
        /// <returns>
        /// The cloned pixel
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Pixel3B Duplicate()
        {
            return new Pixel3B(Blue, Green, Red);
        }

        public override string ToString()
        {
            return $"{{ {GetType().Name} Red: {Red} Green: {Green} Blue: {Blue} }}";
        }

        /// <summary> Initialize a pixel with bgr values.
        ///
        /// </summary>
        /// <param name="blue">pixel value
        /// </param>
        /// <param name="green">pixel value
        /// </param>
        /// <param name="red">pixel value
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBGR(uint blue, uint green, uint red)
        {
            _Blue = (byte)blue;
            _Red = (byte)red;
            _Green = (byte)green;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetBGR(uint color)
        {
            byte* colorPtr = (byte*)&color;
            colorPtr++;
            _Blue = *colorPtr; colorPtr++;
            _Green = *colorPtr; colorPtr++;
            _Red = *colorPtr;
        }

        /// <summary> Test if two pixels are equal.
        ///
        /// </summary>
        /// <param name="object">pixel to compare to
        ///
        /// </param>
        /// <returns> true if red, green, and blue values are all equal
        /// </returns>
        public override bool Equals(object item)
        {
            if (item != null)
                return this == (Pixel3B)item;
            else
                return false;
        }

        /// <summary> Set the gray color.
        ///
        /// </summary>
        /// <param name="gray">pixel value
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetGray(byte gray)
        {
            _Blue = gray;
            _Red = gray;
            _Green = gray;
        }

        /// <summary> Generates a hashCode equal to 0xffRRGGBB.
        ///
        /// </summary>
        /// <returns> hashCode of 0xffRRGGBB
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            uint red = _Red;
            uint green = _Green;
            uint mask = 0xff000000;
            return unchecked((int)(mask | red << 16 | green << 8 | _Blue));
        }

        /// <summary>
        /// Copy the pixel values.
        /// </summary>
        /// <param name="pixel">
        /// pixel to copy
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(Pixel3B pixel)
        {
            _Blue = pixel.Blue;
            _Red = pixel.Red;
            _Green = pixel.Green;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Pixel3B other)
        {
            return this == other;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool Equals(IPixel other)
        //{
        //    return this == (Pixel)other;
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Pixel3B first, Pixel3B second)
        {
            return first.Red == second.Red && first.Green == second.Green && first.Blue == second.Blue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Pixel3B first, Pixel3B second)
        {
            return first.Red != second.Red || first.Green != second.Green || first.Blue != second.Blue;
        }

        #endregion Public Methods
    }
}
