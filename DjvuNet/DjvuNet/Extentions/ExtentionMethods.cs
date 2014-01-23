// -----------------------------------------------------------------------
// <copyright file="ExtentionMethods.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System.Drawing;

namespace DjvuNet.Extentions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class ExtentionMethods
    {
        /// <summary>
        /// Orients the rectangle for the proper page location
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="pageHeight"></param>
        /// <returns></returns>
        public static Rectangle OrientRectangle(this Rectangle rectangle, int pageHeight)
        {
            return new Rectangle(rectangle.X, pageHeight - rectangle.Y - rectangle.Height, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        /// Orients the rectangle for the proper page location
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="pageHeight"></param>
        /// <returns></returns>
        public static Rectangle OrientRectangle(this Graphics.Rectangle rectangle, int pageHeight)
        {
            return new Rectangle(rectangle.Right, pageHeight - rectangle.Bottom - rectangle.Height, rectangle.Width, rectangle.Height);
        }
    }
}