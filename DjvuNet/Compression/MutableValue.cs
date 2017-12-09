// <copyright file="MutableValue.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DjvuNet.Compression
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class MutableValue<T> where T : IComparable<T>, IEquatable<T>
    {
        #region Public Properties

        #region Value

        //private T _value;

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public T Value;
        //{
        //    get;
        //    set;
        //}

        #endregion Value

        #endregion Public Properties

        #region Constructors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MutableValue()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MutableValue(T value)
        {
            Value = value;
        }

        #endregion Constructors

        #region Public Methods

        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }
}
