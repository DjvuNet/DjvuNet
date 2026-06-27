using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DjvuNet.Errors;
using DjvuNet.Interfaces;

namespace DjvuNet.JB2
{
    public class JB2Dictionary : JB2Item, IDecoder
    {
        #region Internal Fields

        internal List<JB2Item> _Shapes;

        #endregion Internal Fields

        #region Public Properties

        /// <summary>
        /// Gets or sets the string coded by the JB2 file
        /// </summary>
        public string Comment;

        /// <summary>
        /// Query if this is image data.  Note that even though this data affects
        /// rendering, the effect is indirect.  This class itself does not produce
        /// an image, so the return value is false.
        /// </summary>
        public virtual bool ImageData
        {
            get { return false; }
        }

        private JB2Dictionary _inheritedDictionary;

        /// <summary>
        /// Gets or sets the inherited dictionary
        /// </summary>
        public JB2Dictionary InheritedDictionary
        {
            get { return _inheritedDictionary; }
            set
            {
                if (_inheritedDictionary != value)
                {
                    SetInheritedDict(value, false);
                }
            }
        }

        /// <summary>
        /// Gets the total inherited shapes
        /// </summary>
        public int InheritedShapes;

        /// <summary>
        /// Gets the total shape count
        /// </summary>
        public int ShapeCount
        {
            get { return InheritedShapes + _Shapes.Count; }
        }

        #endregion Public Properties

        #region Constructors

        public JB2Dictionary() : base()
        {
            _Shapes = new List<JB2Item>();
        }

        #endregion Constructors

        #region Public Methods

        public void Decode(IBinaryReader pool)
        {
            Decode(pool, null);
        }

        public virtual int AddShape(JB2Shape jb2Shape)
        {
            if (jb2Shape.Parent >= ShapeCount)
            {
                DjvuExceptionUtil.ThrowArgument("JB2 decoding failed: Illegal parent shape number in JB2Shape.", nameof(jb2Shape));
            }

            int retval = InheritedShapes + _Shapes.Count;
            _Shapes.Add(jb2Shape);
            return retval;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Decode(IBinaryReader gbs, JB2Dictionary zdict)
        {
            Init();
            JB2Decoder codec = new JB2Decoder();
            codec.Init(gbs, zdict);
            codec.Code(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual JB2Shape GetShape(int shapeNum)
        {
            if (shapeNum < 0 || shapeNum >= ShapeCount)
            {
                DjvuExceptionUtil.ThrowArgumentOutOfRange(nameof(shapeNum), shapeNum, "Shape number is outside the bounds of the dictionary.");
            }

            if (shapeNum >= InheritedShapes)
            {
                return (JB2Shape)_Shapes[shapeNum - InheritedShapes];
            }

            return InheritedDictionary.GetShape(shapeNum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Init()
        {
            InheritedDictionary = null;
            _Shapes.Clear();
        }

        public virtual void SetInheritedDict(JB2Dictionary value, bool force)
        {
            if (value == null)
            {
                _inheritedDictionary = null;
                InheritedShapes = 0;
                return;
            }

            if (force == false)
            {
                if (_Shapes.Count > 0)
                {
                    DjvuExceptionUtil.ThrowInvalidOperation("JB2 decoding failed: Cannot set dictionary after adding shapes.");
                }

                if (InheritedDictionary != null)
                {
                    DjvuExceptionUtil.ThrowInvalidOperation("JB2 decoding failed: Cannot change dictionary once set.");
                }
            }

            _inheritedDictionary = value;
            InheritedShapes = value.ShapeCount;

            //    for (int i=0; i<inherited_shapes; i++)
            //    {
            //      Shape jshp = dict.get_shape(i);
            //      if (jshp.bits != null) jshp.bits.share();
            //    }
        }

        #endregion Public Methods
    }
}
