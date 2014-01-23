// <copyright file="MutableValue.cs" company="">
// TODO: Update copyright text.
// </copyright>

namespace DjvuNet.Compression
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class MutableValue<T> where T : IComparable<T>, IEquatable<T>
    {
        #region Public Properties

        #region Value

        private T _value;

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public T Value
        {
            get
            {
                return _value;
            }

            set
            {
                //Console.WriteLine("Mutable value changing: {0} to {1}", _value, value);
                //if (Value.Equals(value) == false)
                {
                    _value = value;
                }
            }
        }

        #endregion Value

        #endregion Public Properties

        #region Constructors

        public MutableValue()
        {
            // Nothing
        }

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