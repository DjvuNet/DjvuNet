// -----------------------------------------------------------------------
// <copyright file="TextZone.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DjvuNet.Extentions;
using DjvuNet.Graphics;

namespace DjvuNet.DataChunks
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class TextZone
    {
        #region Private Members

        #endregion Private Members

        #region Public Properties

        #region X

        /// <summary>
        /// Gets the X offset for the zone
        /// </summary>
        public int X { get; internal set; }

        #endregion X

        #region Y

        /// <summary>
        /// Gets the Y offset for the zone
        /// </summary>
        public int Y { get; internal set; }

        #endregion Y

        #region Width

        /// <summary>
        /// Gets the width of the text zone
        /// </summary>
        public int Width { get; internal set; }

        #endregion Width

        #region Height

        /// <summary>
        /// Gets the height of the text zone
        /// </summary>
        public int Height { get; internal set; }

        #endregion Height

        #region TextLength

        /// <summary>
        /// Gets the length of text in the zone
        /// </summary>
        public int TextLength { get; internal set; }

        #endregion TextLength

        #region TextOffset

        /// <summary>
        /// Gets the offset of the text
        /// </summary>
        public int TextOffset { get; internal set; }

        #endregion TextOffset

        #region Children

        /// <summary>
        /// Gets the children text zones
        /// </summary>
        public TextZone[] Children { get; internal set; }

        #endregion Children

        #region ZoneType

        /// <summary>
        /// Gets the zone type
        /// </summary>
        public ZoneTypes ZoneType { get; internal set; }

        #endregion ZoneType

        #region Parent

        /// <summary>
        /// Gets the parent for this text zone
        /// </summary>
        public TextZone Parent { get; internal set; }

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

        /// <summary>
        /// Gets the rectangle for the zone
        /// </summary>
        public Rectangle Rectangle { get; internal set; }

        #endregion Rectangle

        #region ChunkParent

        /// <summary>
        /// Gets the text chunk parent for the zone
        /// </summary>
        public TextChunk ChunkParent { get; internal set; }

        #endregion ChunkParent

        #endregion Public Properties

        #region Constructors

        public TextZone(IDjvuReader reader, TextZone parent, TextZone sibling, TextChunk chunkParent)
        {
            Parent = parent;
            ChunkParent = chunkParent;

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
        internal void DecodeZoneData(IDjvuReader reader, TextZone sibling, TextChunk chunkParent)
        {
            ZoneType = (ZoneTypes)reader.ReadByte();
            X = reader.ReadUInt16BigEndian() - 0x8000;
            Y = reader.ReadUInt16BigEndian() - 0x8000;
            Width = reader.ReadUInt16BigEndian() - 0x8000;
            Height = reader.ReadUInt16BigEndian() - 0x8000;

            TextOffset = reader.ReadUInt16BigEndian() - 0x8000;
            TextLength = reader.ReadInt24BigEndian();

            ResolveOffsets(Parent, sibling);

            Rectangle = new Rectangle(X, Y, Width, Height);

            int childrenZones = reader.ReadInt24BigEndian();
            List<TextZone> children = new List<TextZone>();

            TextZone childrenSibling = null;

            for (int x = 0; x < childrenZones; x++)
            {
                TextZone newZone = new TextZone(reader, this, childrenSibling, chunkParent);
                childrenSibling = newZone;

                children.Add(newZone);
            }

            Children = children.ToArray();
        }

        /// <summary>
        /// Gets the text which occurs before the word
        /// </summary>
        /// <returns></returns>
        internal string GetPreTextData()
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
        internal string GetPostTextData()
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

        internal string GetTextData()
        {
            if (ZoneType == ZoneTypes.Word)
            {
                int length = Math.Min(TextLength, ChunkParent.Text.Length - TextOffset);

                if (length <= 0)
                {
                    return "";
                }

                return ChunkParent.Text.Substring(TextOffset, length).Trim();
            }

            if (ZoneType == ZoneTypes.Line)
            {
                return string.Join(" ", Children.Select(x => x.Text)) + "\r\n";
            }

            return string.Join(" ", Children.Select(x => x.Text));
        }

        /// <summary>
        /// Resolves the zone offsets for the location and text
        /// </summary>
        internal void ResolveOffsets(TextZone parent, TextZone sibling)
        {
            if (parent == null && sibling == null) return;

            if (sibling == null)
            {
                X += parent.Rectangle.Right;
                Y = parent.Rectangle.Top - (Y + Height);
                TextOffset += parent.TextOffset;
            }
            else
            {
                if (sibling.ZoneType == ZoneTypes.Page || sibling.ZoneType == ZoneTypes.Paragraph 
                    || sibling.ZoneType == ZoneTypes.Line)
                {
                    X += sibling.Rectangle.Right;
                    Y = sibling.Rectangle.Bottom - (Y + Height);
                }
                else if (sibling.ZoneType == ZoneTypes.Column || sibling.ZoneType == ZoneTypes.Word 
                    || sibling.ZoneType == ZoneTypes.Character)
                {
                    X += sibling.Rectangle.Left;
                    Y += sibling.Rectangle.Bottom;
                }

                TextOffset += sibling.TextOffset + sibling.TextLength;
            }
        }

        #endregion Private Methods
    }
}