using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DjvuNet.Graphics
{
    ///
    /// TODO - break inheritance hierarchy with PixelReference
    ///

    /// <summary>
    /// This class represents a single pixel.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public class Pixel : IEquatable<Pixel>
    {
        #region Private Members

        [FieldOffset(0)]
        private int _Color;

        [FieldOffset(0)]
        private sbyte _Alpha;

        [FieldOffset(1)]
        private sbyte _Blue;

        [FieldOffset(2)]
        private sbyte _Green;

        [FieldOffset(3)]
        private sbyte _Red;

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
            get { return new Pixel(0, 0, -1); }
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
            get { return new Pixel(-1, 0, 0); }
        }

        #endregion RedPixel

        #endregion Public Static Properties

        #region Public Properties

        #region Blue

        /// <summary>
        /// Gets or sets the blue value for the pixel
        /// </summary>
        public virtual sbyte Blue
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
        public virtual sbyte Green
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
        public sbyte Red
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _Red; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _Red = value; }
        }

        #endregion Red

        #endregion Public Properties

        #region Constructors

        protected Pixel()
        {
            _Color = unchecked((int)0xff000000);
        }

        protected Pixel(int color)
        {
            _Color = color;
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
        public Pixel(sbyte blue = -51, sbyte green = -51, sbyte red = -51)
        {
            _Color = unchecked((int)0xff000000);
            _Blue = blue;
            _Green = green;
            _Red = red;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary> Create a clone of this pixel.
        ///
        /// </summary>
        /// <returns> the cloned pixel
        /// </returns>
        public virtual Pixel Duplicate()
        {
            return new Pixel(this.GetHashCode());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"{{ {base.ToString()} Red: {Red} Green: {Green} Blue: {Blue} }}";
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
        public virtual void SetBGR(int blue, int green, int red)
        {
            _Blue = (sbyte)blue;
            _Red = (sbyte)red;
            _Green = (sbyte)green;
        }

        public unsafe virtual void SetBGR(int color)
        {
            sbyte* colorPtr = (sbyte*)&color;
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
        public override bool Equals(Object item)
        {
            if (null != item)
            {
                Pixel other = item as Pixel;
                if (null != other)
                    return _Color == other._Color;
                else
                    return false;
            }
            else
                return false;
        }

        /// <summary> Set the gray color.
        ///
        /// </summary>
        /// <param name="gray">pixel value
        /// </param>
        public void SetGray(sbyte gray)
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
            return _Color;
        }

        /// <summary>
        /// Copy the pixel values.
        /// </summary>
        /// <param name="pixel">
        /// pixel to copy
        /// </param>
        public void CopyFrom(Pixel pixel)
        {
            _Blue = pixel._Blue;
            _Red = pixel._Red;
            _Green = pixel._Green;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Pixel other)
        {
            if (other != null)
                return _Color == other._Color;
            else
                return false;
        }

        #endregion Public Methods
    }
}