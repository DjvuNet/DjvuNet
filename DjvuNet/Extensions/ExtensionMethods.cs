// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace DjvuNet.Extensions
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class ExtensionMethods
    {

        /// <summary>
        /// Orients the rectangle for the proper page location
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="pageHeight"></param>
        /// <returns></returns>
        public static System.Drawing.Rectangle OrientRectangle(this System.Drawing.Rectangle rectangle, int pageHeight)
        {
            return new System.Drawing.Rectangle(rectangle.X, pageHeight - rectangle.Y - rectangle.Height, rectangle.Width, rectangle.Height);
        }

        /// <summary>
        /// Orients the rectangle for the proper page location
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="pageHeight"></param>
        /// <returns></returns>
        public static System.Drawing.Rectangle OrientRectangle(this Graphics.Rectangle rectangle, int pageHeight)
        {
            return new System.Drawing.Rectangle(rectangle.XMin, pageHeight - rectangle.YMin - rectangle.Height, rectangle.Width, rectangle.Height);
        }

    }
}
