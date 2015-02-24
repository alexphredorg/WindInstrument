// 
// SailboatComputer - FontInfo.cs
// 
// Created 01 - 2013
// 
// Alex Wetmore

using System.IO;
using System;
using System.Resources;
using Microsoft.SPOT;

namespace SailboatComputer
{
    /// <summary>
    /// OledFont represents a bitmap font that uses 4bpp greyscale for each pixel.  Characters are variable width, but
    /// the width must be even to stay byte aligned.
    /// 
    /// The font is loaded from a font resource.  The file that contains the font resource should have this 
    /// file format:
    /// bytes  0- 7: "OledFont"
    /// bytes  7- 11: 0x00000001  (version)
    /// bytes 12- 15: character height.  This only supports heights up to 255
    /// bytes 15-272: one byte per character (0-255) with the width of that character.  This value must be even.  
    /// the rest:     A bitmap for every character.  The size of this bitmap is height*characterWidth/2
    ///
    /// If the character width is 0 then there is no bitmap for that character.  This is valid to save space by only 
    /// making a font with the characters that will be used by the application.
    /// 
    /// Fonts can be made by using CBFG, which will convert a Windows-supported font into a bitmap array.  The array
    /// can then be converted to a ".font" file using CBFGtoCode.
    /// 
    /// Only 8-bit (ASCII) characters are supported.
    /// </summary>
    public class OledFont
    {
        /// <summary>
        /// Create a new font instance by loading it from a resource.  Fonts take a lot of 
        /// memory, your app should only load them once.
        /// </summary>
        /// <param name="fontResource">The resource that contains this font.</param>
        public OledFont(Enum fontResource)
        {
            var resourceId = fontResource;

            ResourceManager resourceManager = SailboatComputer.Properties.Resources.ResourceManager;
            byte[] fontData = (byte[]) ResourceUtility.GetObject(resourceManager, resourceId, 0, 16);

            if (fontData[0] != 'O' && fontData[1] != 'l' && fontData[2] != 'e' && fontData[3] != 'd' &&
                fontData[4] != 'F' && fontData[5] != 'o' && fontData[6] != 'n' && fontData[7] != 't')
            {
                throw new IOException("invalid font");
            }

            // check version
            if (fontData[8] != 0x01 && fontData[9] != 0x00 && fontData[10] != 0x00 && fontData[11] != 0x00)
            {
                throw new IOException("invalid font version");
            }

            // get the cell height
            if (fontData[13] != 0 && fontData[14] != 0 && fontData[15] != 0)
            {
                throw new IOException("font too tall, max 255 supported");
            }
            this.m_height = fontData[12];

            int offset = 16;

            // load the width table
            this.m_characterWidthTable = (byte[])ResourceUtility.GetObject(resourceManager, resourceId, offset, 256);
            offset += 256;

            // load each character bitmap
            this.m_charBitmaps = new byte[256][];
            for (int i = 0; i < 256; i++)
            {
                if (this.m_characterWidthTable[i] != 0)
                {
                    // round up to the nearest even number.  We don't support odd sized widths 
                    // since there are two pixels per byte.
                    int width = this.m_characterWidthTable[i];
                    width = ((width % 2) == 1 ? width + 1 : width);

                    int length = width * this.m_height / 2;
                    this.m_charBitmaps[i] = (byte[])ResourceUtility.GetObject(resourceManager, resourceId, offset, length);
                    offset += length;
                }
            }
        }

        /// <summary>
        ///     Get the height of this font
        /// </summary>
        public byte CharacterHeight
        {
            get { return this.m_height; }
        }

        /// <summary>
        ///     Get the width of a specific character.
        /// </summary>
        /// <param name="c">The character to look up</param>
        /// <returns>The width, in pixels, of this character</returns>
        public byte GetCharacterWidth(byte c) { return this.m_characterWidthTable[c]; }

        /// <summary>
        ///     Get the bitmap for a specific character
        /// </summary>
        /// <param name="c">The character to look up</param>
        /// <returns>A bitmap for this character</returns>
        public byte[] GetCharacterBitmap(byte c) { return this.m_charBitmaps[c]; }

        /// <summary>
        /// Does this font have a bitmap for this character?
        /// </summary>
        /// <param name="c">The character to look up</param>
        /// <returns>True if this character has a bitmap</returns>
        public bool IsCharacterInFont(byte c) { return (this.GetCharacterWidth(c) != 0); }

        /// <summary>
        /// Get the width that this string will take when drawn with this font.  The width is
        /// in pixels.
        /// </summary>
        /// <param name="s">the string to check</param>
        /// <returns>the width of the string</returns>
        public int GetStringWidthInPixels(string s)
        {
            int width = 0;
            char[] stringArray = s.ToCharArray();
            foreach (char c in stringArray)
            {
                width += GetCharacterWidth((byte) c);
            }
            return width;
        }

        /// <summary>
        /// The array of bitmaps, one per character.  A null bitmap pointer means that there is no bitmap for this
        /// character, and thus it can't be drawn.
        /// </summary>
        private readonly byte[][] m_charBitmaps;
        /// <summary>
        /// The height of a character.  This is fixed for all characters in the font.
        /// </summary>
        private readonly byte m_height; // height of the font's characters
        /// <summary>
        /// The width of each character in pixels.  0 means that there is no bitmap for that character.
        /// </summary>
        private readonly byte[] m_characterWidthTable;
    }
}
