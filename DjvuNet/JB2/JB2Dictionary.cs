using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using DjvuNet.Interfaces;

namespace DjvuNet.JB2
{
    public class JB2Dictionary : JB2Item, ICodec
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
        /// Query if this is image data.  Note that even though this data effects
        /// rendering, the effect is indirect.  This class itself does not produce
        /// an image, so the return value is false.
        /// </summary>
        public virtual bool ImageData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return false; }
        }

        private JB2Dictionary _inheritedDictionary;

        /// <summary>
        /// Gets or sets the inherited dictionary
        /// </summary>
        public JB2Dictionary InheritedDictionary
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _inheritedDictionary; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_inheritedDictionary != value)
                    SetInheritedDict(value, false);
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return InheritedShapes + _Shapes.Count; }
        }

        #endregion Public Properties

        #region Constructors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JB2Dictionary() : base()
        {
            _Shapes = new List<JB2Item>();
        }

        #endregion Constructors

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Decode(IBinaryReader pool)
        {
            Decode(pool, null);
        }

        public virtual int AddShape(JB2Shape jb2Shape)
        {
            if (jb2Shape.Parent >= ShapeCount)
                throw new ArgumentException("Image bad parent shape");

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
            JB2Shape retval;

            if (shapeNum >= InheritedShapes)
                retval = (JB2Shape)_Shapes[shapeNum - InheritedShapes];
            else if (InheritedDictionary != null)
                retval = InheritedDictionary.GetShape(shapeNum);
            else
                throw new DjvuFormatException("Bad image number");

            return retval;
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
                    throw new DjvuFormatException("Can not set image.");

                if (InheritedDictionary != null)
                    throw new DjvuFormatException("Image can not be changed.");
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