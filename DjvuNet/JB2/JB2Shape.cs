using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using DjvuNet.Graphics;

namespace DjvuNet.JB2
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct JB2Shape : IEquatable<JB2Shape>
    {
        /// <summary>
        /// Gets or sets the parent for the shape
        /// </summary>
        public int Parent;

        /// <summary>
        /// Gets or sets the user data for the shape
        /// </summary>
        public long UserData;

        private Bitmap _bitmap;

        /// <summary>
        /// Gets or sets the bitmap for the shape
        /// </summary>
        [JsonIgnore]
        [UnscopedRef]
        public ref Bitmap Bitmap => ref _bitmap;

        #region Constructors

        public JB2Shape(int parent)
        {
            Init(parent);
        }

        /// <summary>
        /// Creates a new Shape object.
        /// </summary>
        public JB2Shape()
        {
        }

        #endregion Constructors

        #region Public Methods

        public JB2Shape Duplicate()
        {
            JB2Shape retval = new JB2Shape();
            retval.Bitmap = Bitmap.Duplicate();
            retval.Parent = Parent;
            retval.UserData = UserData;

            return retval;
        }

        [UnscopedRef]
        public ref JB2Shape Init(int parent)
        {
            Parent = parent;
            Bitmap = default;
            return ref this;
        }

        public bool Equals(JB2Shape other)
        {
            return this == other;
        }

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            return obj is JB2Shape other && this == other;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Parent, UserData, _bitmap.GetHashCode());
        }

        public static bool operator ==(JB2Shape shape1, JB2Shape shape2)
        {
            return shape1.Parent == shape2.Parent && shape1.UserData == shape2.UserData && shape1._bitmap == shape2._bitmap;
        }

        public static bool operator !=(JB2Shape shape1, JB2Shape shape2)
        {
            return !(shape1 == shape2);
        }

        #endregion Public Methods
    }
}
