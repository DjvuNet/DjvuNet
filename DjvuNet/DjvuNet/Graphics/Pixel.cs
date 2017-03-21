using System;
using System.Runtime.CompilerServices;

namespace DjvuNet.Graphics
{
    /// <summary>
    /// This class represents a single pixel.
    /// </summary>
    public class Pixel
    {
        #region Private Members

        #endregion Private Members

        #region Public Static Properties

        #region TotalColorsPerPixel

        /// <summary>
        /// Gets the total number of colors in a pixel
        /// </summary>
        public static int TotalColorsPerPixel
        {
            get { return 3; }
        }

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
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set;
        }

        #endregion Blue

        #region Green

        /// <summary>
        /// Gets or sets the green value for the pixel
        /// </summary>
        public virtual sbyte Green
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set;
        }

        #endregion Green

        #region Red

        /// <summary>
        /// Gets or sets the red value for the pixel
        /// </summary>
        public sbyte Red
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set;
        }

        #endregion Red

        #endregion Public Properties

        #region Constructors

        protected Pixel()
        {
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
            Blue = blue;
            Green = green;
            Red = red;
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
            return new Pixel(Blue, Green, Red);
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
            Blue = (sbyte)blue;
            Red = (sbyte)red;
            Green = (sbyte)green;
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
            if (!(item is Pixel))
            {
                return false;
            }

            Pixel other = (Pixel)item;

            return (other.Blue == Blue) &&
                   (other.Green == Green) &&
                   (other.Red == Red);
        }

        /// <summary> Set the gray color.
        ///
        /// </summary>
        /// <param name="gray">pixel value
        /// </param>
        public void SetGray(sbyte gray)
        {
            Blue = gray;
            Red = gray;
            Green = gray;
        }

        /// <summary> Generates a hashCode equal to 0xffRRGGBB.
        ///
        /// </summary>
        /// <returns> hashCode of 0xffRRGGBB
        /// </returns>
        public override int GetHashCode()
        {
            return unchecked((int)0xff000000) | ((int)Red << 16) | ((int)Green << 8) | (int)Blue;
        }

        /// <summary>
        /// Copy the pixel values.
        /// </summary>
        /// <param name="pixel">
        /// pixel to copy
        /// </param>
        public void CopyFrom(Pixel pixel)
        {
            Blue = pixel.Blue;
            Red = pixel.Red;
            Green = pixel.Green;
        }

        #endregion Public Methods
    }
}