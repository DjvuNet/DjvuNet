using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DjvuNet.Interfaces;

namespace DjvuNet.JB2
{
    public class JB2Dictionary : JB2Item, ICodec
    {
        #region Private Variables

        private List<JB2Item> _shapes = new List<JB2Item>();

        #endregion Private Variables

        #region Public Properties

        #region Comment

        private string _comment;

        /// <summary>
        /// Gets or sets the string coded by the JB2 file
        /// </summary>
        public string Comment
        {
            get { return _comment; }

            set
            {
                if (Comment != value)
                {
                    _comment = value;
                }
            }
        }

        #endregion Comment

        #region ImageData

        /// <summary>
        /// Query if this is image data.  Note that even though this data effects
        /// rendering, the effect is indirect.  This class itself does not produce
        /// an image, so the return value is false.
        /// </summary>
        public virtual bool ImageData
        {
            get { return false; }
        }

        #endregion ImageData

        #region InheritedDictionary

        private JB2Dictionary _inheritedDictionary;

        /// <summary>
        /// Gets or sets the inherited dictionary
        /// </summary>
        public JB2Dictionary InheritedDictionary
        {
            get { return _inheritedDictionary; }

            set
            {
                if (InheritedDictionary != value)
                {
                    SetInheritedDict(value, false);
                }
            }
        }

        #endregion InheritedDictionary

        #region InheritedShapes

        private int _inheritedShapes;

        /// <summary>
        /// Gets the total inherited shapes
        /// </summary>
        public int InheritedShapes
        {
            get { return _inheritedShapes; }

            private set
            {
                if (InheritedShapes != value)
                {
                    _inheritedShapes = value;
                }
            }
        }

        #endregion InheritedShapes

        #region ShapeCount

        /// <summary>
        /// Gets the total shape count
        /// </summary>
        public int ShapeCount
        {
            get { return InheritedShapes + _shapes.Count; }
        }

        #endregion ShapeCount

        #endregion Public Properties

        #region Constructors

        #endregion Constructors

        #region Public Methods

        public void Decode(BinaryReader pool)
        {
            Decode(pool, null);
        }

        public virtual int AddShape(JB2Shape jb2Shape)
        {
            if (jb2Shape.Parent >= ShapeCount)
            {
                throw new ArgumentException("Image bad parent shape");
            }

            int retval = InheritedShapes + _shapes.Count;
            _shapes.Add(jb2Shape);

            return retval;
        }

        public virtual void Decode(BinaryReader gbs, JB2Dictionary zdict)
        {
            Init();

            JB2Decoder codec = new JB2Decoder();
            codec.Init(gbs, zdict);
            codec.Code(this);
        }

        public virtual JB2Shape GetShape(int shapeno)
        {
            JB2Shape retval;

            if (shapeno >= InheritedShapes)
            {
                retval = (JB2Shape)_shapes[shapeno - InheritedShapes];
            }
            else if (InheritedDictionary != null)
            {
                retval = InheritedDictionary.GetShape(shapeno);
            }
            else
            {
                throw new SystemException("Image bad number");
            }

            return retval;
        }

        public virtual void Init()
        {
            InheritedDictionary = null;
            _shapes.Clear();
        }

        public virtual void SetInheritedDict(JB2Dictionary value, bool force)
        {
            if (value == null)
            {
                _inheritedDictionary = null;
                _inheritedShapes = 0;

                return;
            }

            if (force == false)
            {
                if (_shapes.Count > 0)
                {
                    throw new SystemException("Image cannot set");
                }

                if (InheritedDictionary != null)
                {
                    throw new SystemException("Image cannot change");
                }
            }

            _inheritedDictionary = value;
            _inheritedShapes = value.ShapeCount;

            //    for (int i=0; i<inherited_shapes; i++)
            //    {
            //      Shape jshp = dict.get_shape(i);
            //      if (jshp.bits != null) jshp.bits.share();
            //    }
        }

        #endregion Public Methods
    }
}