// -----------------------------------------------------------------------
// <copyright file="TextZone.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DjvuNet.DataChunks.Enums;
using DjvuNet.Extentions;
using DjvuNet.Graphics;

namespace DjvuNet.DataChunks.Text
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class TextZone
    {
        #region Private Variables

        #endregion Private Variables

        #region Public Properties

        #region X

        private int _x;

        /// <summary>
        /// Gets the X offset for the zone
        /// </summary>
        public int X
        {
            get
            {
                return _x;
            }

            private set
            {
                if (X != value)
                {
                    _x = value;
                }
            }
        }

        #endregion X

        #region Y

        private int _y;

        /// <summary>
        /// Gets the Y offset for the zone
        /// </summary>
        public int Y
        {
            get { return _y; }

            private set
            {
                if (Y != value)
                {
                    _y = value;
                }
            }
        }

        #endregion Y

        #region Width

        private int _width;

        /// <summary>
        /// Gets the width of the text zone
        /// </summary>
        public int Width
        {
            get { return _width; }

            private set
            {
                if (Width != value)
                {
                    _width = value;
                }
            }
        }

        #endregion Width

        #region Height

        private int _height;

        /// <summary>
        /// Gets the height of the text zone
        /// </summary>
        public int Height
        {
            get { return _height; }

            private set
            {
                if (Height != value)
                {
                    _height = value;
                }
            }
        }

        #endregion Height

        #region TextLength

        private int _textLength;

        /// <summary>
        /// Gets the length of text in the zone
        /// </summary>
        public int TextLength
        {
            get { return _textLength; }

            private set
            {
                if (TextLength != value)
                {
                    _textLength = value;
                }
            }
        }

        #endregion TextLength

        #region TextOffset

        private int _textOffset;

        /// <summary>
        /// Gets the offset of the text
        /// </summary>
        public int TextOffset
        {
            get { return _textOffset; }

            private set
            {
                if (TextOffset != value)
                {
                    _textOffset = value;
                }
            }
        }

        #endregion TextOffset

        #region Children

        private TextZone[] _children;

        /// <summary>
        /// Gets the children text zones
        /// </summary>
        public TextZone[] Children
        {
            get { return _children; }

            private set
            {
                if (Children != value)
                {
                    _children = value;
                }
            }
        }

        #endregion Children

        #region ZoneType

        private ZoneTypes _zoneType;

        /// <summary>
        /// Gets the zone type
        /// </summary>
        public ZoneTypes ZoneType
        {
            get { return _zoneType; }

            private set
            {
                if (ZoneType != value)
                {
                    _zoneType = value;
                }
            }
        }

        #endregion ZoneType

        #region Parent

        private TextZone _parent;

        /// <summary>
        /// Gets the parent for this text zone
        /// </summary>
        public TextZone Parent
        {
            get { return _parent; }

            private set
            {
                if (Parent != value)
                {
                    _parent = value;
                }
            }
        }

        #endregion Parent

        #region Text

        /// <summary>
        /// Gets the text for this zone
        /// </summary>
        public string Text
        {
            get { return GetTextData(); }
        }

        #endregion Text

        #region PreText

        /// <summary>
        /// Gets the pre text for word items
        /// </summary>
        public string PreText
        {
            get { return GetPreTextData(); }
        }

        #endregion PreText

        #region PostText

        /// <summary>
        /// Gets the post text for word items
        /// </summary>
        public string PostText
        {
            get { return GetPostTextData(); }
        }

        #endregion PostText

        #region Rectangle

        private Rectangle _rectangle;

        /// <summary>
        /// Gets the rectangle for the zone
        /// </summary>
        public Rectangle Rectangle
        {
            get { return _rectangle; }

            private set
            {
                if (Rectangle != value)
                {
                    _rectangle = value;
                }
            }
        }

        #endregion Rectangle

        #region ChunkParent

        private TextChunk _chunkParent;

        /// <summary>
        /// Gets the text chunk parent for the zone
        /// </summary>
        public TextChunk ChunkParent
        {
            get { return _chunkParent; }

            private set
            {
                if (ChunkParent != value)
                {
                    _chunkParent = value;
                }
            }
        }

        #endregion ChunkParent

        #endregion Public Properties

        #region Constructors

        public TextZone(DjvuReader reader, TextZone parent, TextZone sibling, TextChunk chunkParent)
        {
            _parent = parent;
            _chunkParent = chunkParent;

            DecodeZoneData(reader, sibling, chunkParent);
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Searches the zone for the given text
        /// </summary>
        /// <param name="text"></param>
        public TextZone[] SearchForText(string text)
        {
            if (Children.Length == 0 && ZoneType == ZoneTypes.Word)
            {
                if (Text.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    return new TextZone[] { this };
                }
            }

            // Search the children for the text
            return Children.SelectMany(x => x.SearchForText(text)).ToArray();
        }

        /// <summary>
        /// Returns the text zones that are contained within the rectangle
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public TextZone[] OrientedSearchForText(System.Drawing.Rectangle rectangle, int pageHeight)
        {
            if (Children.Length == 0 && ZoneType == ZoneTypes.Word)
            {
                if (rectangle.Contains(Rectangle.OrientRectangle(pageHeight)))
                {
                    return new TextZone[] { this };
                }
            }

            // Search the children for the text
            return Children.SelectMany(x => x.OrientedSearchForText(rectangle, pageHeight)).ToArray();
        }

        /// <summary>
        /// Returns the text zones that are contained within the rectangle
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public TextZone[] SearchForText(System.Drawing.Rectangle rectangle)
        {
            if (Children.Length == 0 && ZoneType == ZoneTypes.Word)
            {
                if (rectangle.Contains(Rectangle))
                {
                    return new TextZone[] { this };
                }
            }

            // Search the children for the text
            return Children.SelectMany(x => x.SearchForText(rectangle)).ToArray();
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Decodes the data for the zone
        /// </summary>
        /// <param name="reader"></param>
        private void DecodeZoneData(DjvuReader reader, TextZone sibling, TextChunk chunkParent)
        {
            _zoneType = (ZoneTypes)reader.ReadByte();
            _x = reader.ReadUInt16MSB() - 0x8000;
            _y = reader.ReadUInt16MSB() - 0x8000;
            _width = reader.ReadUInt16MSB() - 0x8000;
            _height = reader.ReadUInt16MSB() - 0x8000;

            _textOffset = reader.ReadUInt16MSB() - 0x8000;
            _textLength = reader.ReadInt24MSB();

            ResolveOffsets(_parent, sibling);

            _rectangle = new Rectangle(_x, _y, _width, _height);

            int childrenZones = reader.ReadInt24MSB();
            List<TextZone> children = new List<TextZone>();

            TextZone childrenSibling = null;

            for (int x = 0; x < childrenZones; x++)
            {
                TextZone newZone = new TextZone(reader, this, childrenSibling, chunkParent);
                childrenSibling = newZone;

                children.Add(newZone);
            }

            _children = children.ToArray();
        }

        /// <summary>
        /// Gets the text which occurs before the word
        /// </summary>
        /// <returns></returns>
        private string GetPreTextData()
        {
            StringBuilder result = new StringBuilder();

            if (ZoneType == ZoneTypes.Word)
            {
                // Get all the words up to the current word
                foreach (var word in Parent.Children)
                {
                    if (word != this)
                    {
                        result.Append(word.Text + " ");
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result.ToString().Trim();
        }

        /// <summary>
        /// Gets the text which occurs after the word
        /// </summary>
        /// <returns></returns>
        private string GetPostTextData()
        {
            StringBuilder result = new StringBuilder();

            if (ZoneType == ZoneTypes.Word)
            {
                bool wordFound = false;

                // Get all the words after the current word
                foreach (var word in Parent.Children)
                {
                    if (word == this)
                    {
                        wordFound = true;
                    }
                    else if (wordFound == true)
                    {
                        result.Append(word.Text + " ");
                    }
                }
            }

            return result.ToString().Trim();
        }

        private string GetTextData()
        {
            if (ZoneType == ZoneTypes.Word)
            {
                int length = Math.Min(TextLength, ChunkParent.Text.Length - _textOffset);

                if (length <= 0)
                {
                    return "";
                }

                return ChunkParent.Text.Substring(_textOffset, length).Trim();
            }

            if (ZoneType == ZoneTypes.Line)
            {
                return string.Join(" ", _children.Select(x => x.Text)) + "\r\n";
            }

            return string.Join(" ", _children.Select(x => x.Text));
        }

        /// <summary>
        /// Resolves the zone offsets for the location and text
        /// </summary>
        private void ResolveOffsets(TextZone parent, TextZone sibling)
        {
            if (parent == null && sibling == null) return;

            if (sibling == null)
            {
                _x += parent.Rectangle.Right;
                _y = parent.Rectangle.Top - (_y + Height);
                _textOffset += parent.TextOffset;
            }
            else
            {
                if (sibling.ZoneType == ZoneTypes.Page || sibling.ZoneType == ZoneTypes.Paragraph || sibling.ZoneType == ZoneTypes.Line)
                {
                    _x = _x + sibling.Rectangle.Right;
                    _y = sibling.Rectangle.Bottom - (_y + _height);
                }
                else if (sibling.ZoneType == ZoneTypes.Column || sibling.ZoneType == ZoneTypes.Word || sibling.ZoneType == ZoneTypes.Character)
                {
                    _x += sibling.Rectangle.Left;
                    _y += sibling.Rectangle.Bottom;
                }

                _textOffset += sibling.TextOffset + sibling.TextLength;
            }
        }

        #endregion Private Methods
    }
}