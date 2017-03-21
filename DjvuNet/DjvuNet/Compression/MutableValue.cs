// <copyright file="MutableValue.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using DjvuNet.Configuration;

namespace DjvuNet.Compression
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class MutableValue<T> where T : IComparable<T>, IEquatable<T>
    {
        #region Public Properties

        #region Value

        //private T _value;

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set;
        }
        //{
        //    get { return _value; }

        //    set
        //    {
        //        //if (_value.Equals(value) == false)
        //        //{
        //            _value = value;
        //        //    Trace.WriteLineIf(DjvuSettings.LogLevel.TraceVerbose, $"Mutable value changing: {_value} to {value}");
        //        //}
        //    }
        //}

        #endregion Value

        #endregion Public Properties

        #region Constructors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MutableValue()
        {
            // Nothing
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MutableValue(T value)
        {
            Value = value;
        }

        #endregion Constructors

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion Public Methods
    }
}