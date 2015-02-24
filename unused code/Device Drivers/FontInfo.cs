// 
// SailboatComputer - FontInfo.cs
// 
// Created 01 - 2013
// 
// Alex Wetmore

namespace SailboatComputer
{
    /// <summary>
    ///     FontInfo describes a bitmap font for use with display drivers.  This is for fixed height but variable width fonts.  FontInfo
    ///     is a base class that should be inherited by the font itself.
    /// 
    ///     Font bitmaps are stored in column order.  A 60 pixel tall font will contain 8 bytes per column (only the top 4 bits of the
    ///     last column will be used).  Each bit represents one pixel.  A 30 pixel wide character for this font will have 240 bytes 
    ///     in the bitmap: 8*30 = 240.
    /// 
    ///     To make large fonts space efficient it is safe to use a very limited set of bitmaps by setting a small range for StartChar
    ///     and EndChar.
    /// 
    ///     Only 8-bit ASCII characters are supported.
    /// 
    ///     The free tool "CFE" can be useful for generating the bitmap structures:
    ///     https://forum.crystalfontz.com/showthread.php?3619-Bitmap-font-editor-for-graphic-LCD
    ///     Softy can convert TTF to SFP: http://users.breathe.com/l-emmett/
    /// </summary>
    public class FontInfo
    {
        private readonly byte[][] charBitmaps;
        private readonly int endChar; // the last character in the font (e.g. in charInfo and data)
        private readonly byte height; // height of the font's characters
        private readonly byte padding;
        private readonly int startChar; // the first character in the font (e.g. in charInfo and data)
        private readonly byte[] widthTable;

        /// <summary>
        ///     Initialize a new instance of a font.  This should be wrapped by the inheriting class which will
        ///     specifiy most parameters.
        /// </summary>
        /// <param name="height">The height of each glyph.</param>
        /// <param name="padding">Horizontal padding (measured in pixels) to be added between characters.</param>
        /// <param name="widthTable">An ordered table of glyph widths in order from startChar to endChar.</param>
        /// <param name="startChar">The first character that this font has a glyph for.</param>
        /// <param name="endChar">The last character that this font has a glyph for.</param>
        /// <param name="charBitmaps">An array of bitmaps, one per character.</param>
        protected FontInfo(byte height, byte padding, byte[] widthTable, int startChar, int endChar, byte[][] charBitmaps)
        {
            this.height = height;
            this.padding = padding;
            this.widthTable = widthTable;
            this.startChar = startChar;
            this.endChar = endChar;
            this.charBitmaps = charBitmaps;
        }

        /// <summary>
        ///     Get the height of this font
        /// </summary>
        public byte CharacterHeight
        {
            get { return this.height; }
        }

        /// <summary>
        ///     How many horizontal pixels should be inserted between each character when drawing a string?
        /// </summary>
        public byte CharacterPadding
        {
            get { return this.padding; }
        }

        /// <summary>
        ///     Get the width of a specific character.
        /// </summary>
        /// <param name="c">The character to look up</param>
        /// <returns>The width, in pixels, of this character</returns>
        public byte GetCharacterWidth(byte c) { return this.widthTable[c - this.startChar]; }

        /// <summary>
        ///     Get the bitmap for a specific character
        /// </summary>
        /// <param name="c">The character to look up</param>
        /// <returns>A bitmap for this character</returns>
        public byte[] GetCharacterBitmap(byte c) { return this.charBitmaps[c - this.startChar]; }

        /// <summary>
        /// Does this font have a bitmap for this character?
        /// </summary>
        /// <param name="c">The character to look up</param>
        /// <returns>True if this character has a bitmap</returns>
        public bool IsCharacterInFont(byte c) { return (c >= this.startChar && c <= this.endChar); }
    }
}
