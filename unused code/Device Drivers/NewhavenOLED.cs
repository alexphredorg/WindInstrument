// 
// SailboatComputer - OLED.cs
// 
// Created 01 - 2013
// 
// Alex Wetmore
//

using System;
using System.Text;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace SailboatComputer
{
    /// <summary>
    /// This class is a driver for the greyscale 256x64 OLED display made by Newhaven Display.
    /// Part number NHD-3.12-25664UCB2.
    ///     
    /// The display can be addressed in columns and rows.  A column 4 pixels or two bytes wide and that
    /// is the finest level of resolution for addressing the display.
    /// </summary>
    public class Newhaven25664OledDriver : IDisposable
    {
        /// <summary>
        /// Display width in pixels
        /// </summary>
        private const int Width = 256;
        /// <summary>
        /// Display height in pixels
        /// </summary>
        private const int Height = 64;
        /// <summary>
        /// The column offset of the left-most column on the display
        /// </summary>
        private const int FirstCol = 0x1C;
        /// <summary>
        /// The column of the right-most column on the display
        /// </summary>
        private const int LastCol = 0x5B;
        /// <summary>
        /// The number of bytes for one row of the display
        /// </summary>
        private const int BytesPerRow = 128;
        /// <summary>
        /// THe row offset of the top row on the display
        /// </summary>
        private const int FirstRow = 0x00;
        /// <summary>
        /// The bottom row of the display
        /// </summary>
        private const int LastRow = 0x40;
        /// <summary>
        /// Pixels per byte.  2 pixels per byte since the display is 4 bits per pixel.
        /// </summary>
        private const int PixelsPerByte = 2;
        /// <summary>
        /// Pixels per column.  A column is two bytes.
        /// </summary>
        private const int PixelsPerColumn = 4;
        /// <summary>
        /// Bytes per column.  A column is 2 bytes.
        /// </summary>
        private const int BytesPerColumn = 2;

        /// <summary>
        /// The display buffer 
        /// </summary>
        private byte[] displayBuffer = new byte[Width * Height / PixelsPerByte];

        // display connection resources
        private SPI Spi;
        private OutputPort DcPin;
        private OutputPort ResetPin;

        // preallocated small arrays used to send short commands
        private byte[] SpiBuffer1 = new byte[1];
        private byte[] SpiBuffer2 = new byte[2];

        // should each drawing command automatically refresh the display?
        private bool m_fAutoRefresh = true;

        /// <summary>
        /// Initialize the driver and the hardware connection to the display.
        /// </summary>
        /// <param name="dc">Data/Command pin</param>
        /// <param name="reset">Reset pin</param>
        /// <param name="chipSelect">Chip select pin</param>
        /// <param name="spiModule">SPI module</param>
        /// <param name="speedKHz">SPI clock speed in khz</param>
        public Newhaven25664OledDriver(
            Cpu.Pin dc,
            Cpu.Pin reset,
            Cpu.Pin chipSelect,
            SPI.SPI_module spiModule = SPI.SPI_module.SPI1,
            uint speedKHz = 100000)
        {
            var spiConfig = new SPI.Configuration(
                SPI_mod: spiModule,
                ChipSelect_Port: chipSelect,
                ChipSelect_ActiveState: false,
                ChipSelect_SetupTime: 1,
                ChipSelect_HoldTime: 1,
                Clock_IdleState: true,
                Clock_Edge: true,
                Clock_RateKHz: speedKHz
                );

            this.Spi = new SPI(spiConfig);

            this.DcPin = new OutputPort(dc, false);
            this.ResetPin = new OutputPort(reset, false);
        }

        /// <summary>
        /// Initialize the OLED for use
        /// </summary>
        public void Initialize()
        {
            Thread.Sleep(1); // VDD (3.3V) goes high at start, lets just chill for a ms
            this.ResetPin.Write(false); // bring reset low
            Thread.Sleep(10); // wait 10ms
            this.ResetPin.Write(true); // bring out of reset
            Thread.Sleep(300); // give it a little time to boot

            // these are copied out of the application notes found here:
            // http://www.newhavendisplay.com/app_notes/OLED_25664.txt
            SendCommand(Command.SetCommandLock, 0x12);
            SendCommand(Command.SetSleepModeOn);
            SendCommand(Command.SetColumnAddress, 0x00, 0x7F);
            SendCommand(Command.SetRowAddress, 0x00, 0x3F);
            SendCommand(Command.SetDisplayClockRatio, 0x91);
            SendCommand(Command.SetMultiplexRatio, 0x3F);
            SendCommand(Command.SetDisplayOffset, 0x00);
            SendCommand(Command.SetDisplayStartLine, 0x00);
            SendCommand(Command.SetRemap, 0x14, 0x11);
            SendCommand(Command.SetGPIO, 0x00);
            SendCommand(Command.InternalRegulatorSelection, 0x01);
            SendCommand(Command.DisplayEnhancementA, 0xA0, 0xFD);
            SendCommand(Command.SetContrast, 0xCF);
            SendCommand(Command.SetMasterContrast, 0x0F);
            SendCommand(Command.SetDefaultGreyscaleTable);
            SendCommand(Command.SetPhaseLength, 0xE2);
            SendCommand(Command.DisplayEnhancementB, 0x20);
            SendCommand(Command.SetPrechargeVoltage, 0x1F);
            SendCommand(Command.SetSecondPrechargePeriod, 0x08);
            SendCommand(Command.SetVcomVoltage, 0x07);
            SendCommand(Command.DisplayModeNormal);
            SendCommand(Command.ExitPartialDisplay);
            ClearDisplay();
            SendCommand(Command.SetSleepModeOff);
        }

        /// <summary>
        /// Release resources used to drive the display
        /// </summary>
        public void Dispose()
        {
            this.DcPin.Dispose();
            this.ResetPin.Dispose();
            this.Spi.Dispose();

            this.DcPin = null;
            this.ResetPin = null;
            this.Spi = null;
            this.SpiBuffer1 = null;
            this.SpiBuffer2 = null;
        }

        /// <summary>
        /// Invert the display colors
        /// </summary>
        /// <param name="shouldInvert">true to invert, false for normal</param>
        public void InvertDisplay(bool shouldInvert)
        {
            if (shouldInvert)
            {
                SendCommand(Command.DisplayModeInverted);
            }
            else
            {
                SendCommand(Command.DisplayModeNormal);
            }
        }

        public bool DrawString(
            OledFont font,
            int leftPixel,
            int width,
            int topRow,
            Justify justification,
            string s)
        {
            return this.DrawStringFit(font, null, leftPixel, width, topRow, justification, s);
        }

        /// <summary>
        /// Draw a string on the display, fitting it into available horizontal space.  If the string doesn't fit 
        /// using firstChoiceFont then secondChoiceFont will be used.  If it still doesn't fit then it will be 
        /// truncated, perhaps in the middle of a character.
        /// </summary>
        /// <param name="firstChoiceFont">The best font to use</param>
        /// <param name="secondChoiceFont">A smaller font to use if firstChoiceFont doesn't fit.  Null is valid.</param>
        /// <param name="leftPixel">The left pixel of the bounding box</param>
        /// <param name="numPixels">The number of horizontal pixels in the bounding box</param>
        /// <param name="topRow">The top row of the bounding box.  The bottom row is defined by character height</param>
        /// <param name="justification">Left, Center, or Right?</param>
        /// <param name="s">The string to draw</param>
        /// <returns>Was the string truncated?</returns>
        public bool DrawStringFit(
            OledFont firstChoiceFont, 
            OledFont secondChoiceFont, 
            int leftPixel, 
            int numPixels, 
            int topRow, 
            Justify justification, 
            string s)
        {
            if (firstChoiceFont == null)
            {
                throw new ArgumentNullException("firstChoiceFont");
            }
            if (leftPixel % PixelsPerColumn != 0)
            {
                throw new ArgumentException("leftPixel must be column (4 pixels) aligned");
            }
            /*
            if (leftPixel > Width)
            {
                throw new ArgumentException("leftPixel is off of the display");
            }
             */
            if ((numPixels % PixelsPerColumn) != 0)
            {
                throw new ArgumentException("numPixels must be column (4 pixels) aligned");
            }
            if (leftPixel + numPixels > Width)
            {
                throw new ArgumentException("leftPixel + numPixels is off of the display");
            }
            if (secondChoiceFont != null && firstChoiceFont.CharacterHeight != secondChoiceFont.CharacterHeight)
            {
                throw new ArgumentException("firstChoiceFont and secondChoiceFont must be same height");
            }
            if (topRow + firstChoiceFont.CharacterHeight > Height)
            {
                throw new ArgumentException("topRow, string is too tall for display");
            }

            // set to true if we overflow available space
            bool truncated = false;

            // Try the first choice font.  If it won't fit then try the smaller second choice font.
            // That may be too big, but we'll truncate it if necessary.
            OledFont font = firstChoiceFont;
            int stringWidth = font.GetStringWidthInPixels(s);
            if (secondChoiceFont != null && stringWidth > numPixels)
            {
                font = secondChoiceFont;
                stringWidth = font.GetStringWidthInPixels(s);
            }

            int height = font.CharacterHeight;

            // this bitmap is sized to fit the entire character area.  We'll then fill it in 
            // with characters
            int widthInBytes = numPixels / PixelsPerByte;

            // set the correct offset for the desired justification
            int rowOffsetInPixels = 0;
            if (stringWidth < numPixels && justification != Justify.Left)
            {
                rowOffsetInPixels = numPixels - stringWidth;
                if (justification == Justify.Center) rowOffsetInPixels /= 2;
                // make the offset even, bias left
                if (rowOffsetInPixels % 2 == 1) rowOffsetInPixels -= 1;
            }
            int rowOffsetInBytes = rowOffsetInPixels / PixelsPerByte;

            // this is the left offset into the displayBuffer
            byte leftOffset = (byte)(leftPixel / PixelsPerByte);

            char[] charArray = s.ToCharArray();
            lock (this)
            {
                // zero out our target area on the display
                for (int y = 0; y < height; y++)
                {
                    Array.Clear(displayBuffer, leftOffset + ((y + topRow) * BytesPerRow), widthInBytes);
                }

                for (int i = 0; i < charArray.Length && !truncated; i++)
                {
                    byte ch = (byte) (charArray[i]);
                    byte chWidthInPixels = font.GetCharacterWidth(ch);
                    byte chWidthInBytes = (byte) (chWidthInPixels / PixelsPerByte);
                    byte[] chBitmap = font.GetCharacterBitmap(ch);

                    // truncate the string, it won't fit.  We'll keep just the part of this 
                    // character that fits
                    int chWidthInBytesToCopy = chWidthInBytes;
                    if (rowOffsetInBytes + chWidthInBytes > widthInBytes)
                    {
                        truncated = true;
                        chWidthInBytesToCopy = (byte) (widthInBytes - rowOffsetInBytes);
                        //chWidthInPixels = chWidthInBytes * PixelsPerByte;
                    }

                    for (int y = 0; y < height; y++)
                    {
                        /* WORKS
                        for (int x = 0; x < chWidthInBytes; x++)
                        {
                            displayBuffer[(rowOffsetInBytes + x + leftOffset) + ((y + topRow) * BytesPerRow)] =
                                chBitmap[x + (y * chWidthInBytes)];
                        } */
                        Array.Copy(
                            sourceArray:chBitmap, 
                            sourceIndex:(y * chWidthInBytes), 
                            destinationArray:displayBuffer, 
                            destinationIndex:((rowOffsetInBytes + leftOffset) + ((y + topRow) * BytesPerRow)), 
                            length:chWidthInBytesToCopy); 
                    }
                    rowOffsetInBytes += chWidthInBytesToCopy;
                }

                AutoRefresh();
            }

            return truncated;
        }

        /// <summary>
        /// Scroll the full width of the display buffer between two lines.  This is used by DrawStringAndScroll.
        /// </summary>
        /// <param name="topRow">The top row of the display region</param>
        /// <param name="bottomRow">The bottom row of the display region</param>
        /// <param name="numRows">The number of rows to scroll in the display region (typically the character
        /// height of a font).</param>
        private void ScrollDisplayBuffer(int topRow, int bottomRow, int numRows)
        {
            Array.Copy(
                sourceArray: displayBuffer,
                sourceIndex: ((topRow + numRows) * BytesPerRow),
                destinationArray: displayBuffer,
                destinationIndex: (topRow * BytesPerRow),
                length: (bottomRow - topRow - numRows) * BytesPerRow);
            Array.Clear(
                array: displayBuffer, 
                index: (bottomRow * BytesPerRow), 
                length: numRows * BytesPerRow);
        }

        public void ScrollRegionUp(int x0, int x1, int y0, int y1)
        {
            int columnOffset = x0 / PixelsPerByte;
            int copyLength = (x1 - x0) / PixelsPerByte;

            // copy each line from the next one down
            for (int y = y0; y < y1 - 1; y++)
            {
                Array.Copy(
                    sourceArray: displayBuffer,
                    sourceIndex: ((y + 1) * BytesPerRow) + columnOffset,
                    destinationArray: displayBuffer,
                    destinationIndex: (y * BytesPerRow) + columnOffset,
                    length: copyLength);
            }

            // clear the last line
            this.HorizontalLine(x0, x1, y1, 0);
        }

        /// <summary>
        /// Scroll a scrolling display region, then draw a string at the bottom of that region.  This
        /// is useful for a data log.  The scroll region uses the entire width of the display, but the 
        /// height can be configured.
        /// </summary>
        /// <param name="font">The font to use</param>
        /// <param name="topRow">The top row of the scrolling region.</param>
        /// <param name="bottomRow">The bottom row of the scrolling region</param>
        /// <param name="s">The string to display</param>
        public void DrawStringAndScroll(OledFont font, int topRow, int bottomRow, string s)
        {
            //int clearToRow = bottomRow;

            // make the scrolling region an integer number of font heights tall
            bottomRow -= ((bottomRow - topRow) % font.CharacterHeight);

            ScrollDisplayBuffer(topRow, bottomRow, font.CharacterHeight);
            DrawStringFit(font, null, 0, Width, (byte) (bottomRow - font.CharacterHeight), Newhaven25664OledDriver.Justify.Left, s);
        }

        /// <summary>
        /// Draw a test pattern.  This fills the whole screen and uses every available shade.
        /// </summary>
        public void TestPattern()
        {
            for (int i = 0; i < 16; i++)
            {
                byte fill = (byte)((i << 4) + i);
                byte antifill = (byte)(((15 - i) << 4) + (15 - i));
                for (int y = 0; y < 31; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        displayBuffer[((i * 16) + x) + (y * BytesPerRow)] = fill;
                        displayBuffer[((i * 16) + x) + ((y + 32) * BytesPerRow)] = antifill;
                    }
                }
            }
            AutoRefresh();
        }

        /// <summary>
        /// get the color of a display pixel
        /// </summary>
        /// <param name="x">pixel x</param>
        /// <param name="y">pixel y</param>
        /// <returns>pixel color</returns>
        public byte GetPixel(int x, int y)
        {
            int shift = ((x % PixelsPerByte) == 0) ? 4 : 0;
            byte mask = (byte) (0xf << shift);
            return (byte) ((displayBuffer[(y * BytesPerRow) + (x / PixelsPerByte)] & mask) >> shift); 
        }

        /// <summary>
        /// Set the color of a specific pixel in the display buffer
        /// </summary>
        /// <param name="x">pixel x</param>
        /// <param name="y">pixel y</param>
        /// <param name="color">color (4 bits)</param>
        public void SetPixel(int x, int y, byte color)
        {
            int shift = ((x % PixelsPerByte) == 0) ? 4 : 0;
            byte mask = (byte) (~(0xf << shift));
            int index = (y * BytesPerRow) + (x / PixelsPerByte);
            displayBuffer[index] = (byte) ((displayBuffer[index] & mask) | (color << shift));
        }

        /// <summary>
        /// Draw a horizontal line from x0 to x1 at row y
        /// </summary>
        /// <param name="x0">must be even (untested)</param>
        /// <param name="x1">must be odd (untested)</param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void HorizontalLine(int x0, int x1, int y, byte color)
        {
            int index = (y * BytesPerRow) + (x0 / PixelsPerByte);
            color = (byte) ((color << 4) | color);
            for (int i = 0; i < ((x1 - x0) / 2); i++)
            {
                displayBuffer[index + i] = color;
            }
        }

        public void DrawBlock(int x0, int x1, int y0, int y1, byte color)
        {
            byte[] lineBuffer = new byte[(x1 - x0) / PixelsPerByte];
            color = (byte) ((color << 4) | color);
            for (int i = 0; i < lineBuffer.Length; i++)
            {
                lineBuffer[i] = color;
            }
            int rowOffsetInBytes = x0 / PixelsPerByte;
            for (int y = y0; y < y1; y++)
            {
                Array.Copy(
                    sourceArray: lineBuffer,
                    sourceIndex: 0,
                    destinationArray: displayBuffer,
                    destinationIndex: ((rowOffsetInBytes) + (y * BytesPerRow)),
                    length: lineBuffer.Length);
            }
        }

        /// <summary>
        /// Draw a vertical line from y0 to y1 on column x
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y0"></param>
        /// <param name="y1"></param>
        /// <param name="color"></param>
        public void VerticalLine(int x, int y0, int y1, byte color)
        {
            for (int y = y0; y < y1; y++)
            {
                this.SetPixel(x, y, color);
            }
        }

        /// <summary>
        /// Set the display contrast using a value between 0 and 0xff
        /// </summary>
        /// <param name="contrast">The contrast</param>
        public void SetContrast(int contrast)
        {
            /*
            if (contrast > 15)
            {
                throw new ArgumentException("contrast");
            } 
             */

            SendCommand(Command.SetContrast, (byte) contrast);
        }

        /// <summary>
        /// Clear the whole display to black.
        /// </summary>
        public void ClearDisplay()
        {
            Array.Clear(displayBuffer, 0, displayBuffer.Length);
            AutoRefresh();
        }

        /// <summary>
        /// Temporarily pause automatic screen refreshes by display commands.  This can be used to create a display
        /// transaction when drawing complex shapes on the display.
        /// </summary>
        public void PauseRefresh()
        {
            m_fAutoRefresh = false;
        }

        /// <summary>
        /// Enable automatic refreshing of the display, and refresh the screen with the display buffer.
        /// </summary>
        public void ResumeRefresh()
        {
            m_fAutoRefresh = true;
            Refresh();
        }

        /// <summary>
        /// Copy the contents of the display buffer to the display
        /// </summary>
        public void Refresh()
        {
            SendCommand(Command.SetColumnAddress, FirstCol, LastCol);
            SendCommand(Command.SetRowAddress, FirstRow, LastRow);
            SendCommand(Command.WriteRamCommand, displayBuffer);            
        }

        /// <summary>
        /// Refresh the display if automatic refreshing is enabled.
        /// </summary>
        private void AutoRefresh()
        {
            if (m_fAutoRefresh) this.Refresh();
        }

        /// <summary>
        /// Turn the display on (out of sleep mode)
        /// </summary>
        public void On()
        {
            SendCommand(Command.SetSleepModeOff);
        }

        /// <summary>
        /// Turn the display off (sleep mode)
        /// </summary>
        public void Off()
        {
            SendCommand(Command.SetSleepModeOn);
        }

        /// <summary>
        /// Justification for DrawStringFit
        /// </summary>
        public enum Justify
        {
            Left = 0,
            Center = 1,
            Right = 2
        };

        /// <summary>
        /// The command list for this display
        /// </summary>
        private enum Command
        {
            EnableGreyscaleTable = 0x00,
            SetColumnAddress = 0x15,
            WriteRamCommand = 0x5c,
            ReadRamCommand = 0x5d,
            SetRowAddress = 0x75,
            SetRemap = 0xa0,
            SetDisplayStartLine = 0xa1,
            SetDisplayOffset = 0xa2,
            DisplayModeOff = 0xa4,
            DisplayModeAllOn = 0xa5,
            DisplayModeNormal = 0xa6,
            DisplayModeInverted = 0xA7,
            EnablePartialDisplay = 0xA8,
            ExitPartialDisplay = 0xA9,
            InternalRegulatorSelection = 0xAB,
            SetSleepModeOn = 0xAE,
            SetSleepModeOff = 0xAF,
            SetPhaseLength = 0xB1,
            SetDisplayClockRatio = 0xB3,
            DisplayEnhancementA = 0xB4,
            SetGPIO = 0xB5,
            SetSecondPrechargePeriod = 0xB6,
            SetGreyscaleTable = 0xB8,
            SetDefaultGreyscaleTable = 0xB9,
            SetPrechargeVoltage = 0xBB,
            SetVcomVoltage = 0xBE,
            SetContrast = 0xC1,
            SetMasterContrast = 0xC7,
            SetMultiplexRatio = 0xCA,
            DisplayEnhancementB = 0xD1,
            SetCommandLock = 0xFD
        }

        /// <summary>
        /// Send a command to the screen
        /// </summary>
        /// <param name="cmd">The command</param>
        private void SendCommand(Command cmd)
        {
            this.DcPin.Write(false);

            this.SpiBuffer1[0] = (byte)cmd;
            this.Spi.Write(this.SpiBuffer1);
        }

        /// <summary>
        /// Send a command to the screen with one byte of data
        /// </summary>
        /// <param name="cmd">Command</param>
        /// <param name="data">Data byte</param>
        private void SendCommand(Command cmd, byte data)
        {
            this.SendCommand(cmd);
            this.SendData(data);
        }

        /// <summary>
        /// Send a command to the screen with two bytes of data
        /// </summary>
        /// <param name="cmd">Command</param>
        /// <param name="data1">Data 1</param>
        /// <param name="data2">Data 2</param>
        private void SendCommand(Command cmd, byte data1, byte data2)
        {
            this.SendCommand(cmd);

            this.DcPin.Write(true);
            this.SpiBuffer2[0] = data1;
            this.SpiBuffer2[1] = data2;
            this.Spi.Write(this.SpiBuffer2);
        }

        /// <summary>
        /// Send a command with a byte array of data.
        /// </summary>
        /// <param name="cmd">command</param>
        /// <param name="data">data</param>
        private void SendCommand(Command cmd, byte[] data)
        {
            SendCommand(cmd);
            SendData(data);
        }

        /// <summary>
        /// Send 1 byte of data to the screen
        /// </summary>
        /// <param name="data">A byte of data</param>
        private void SendData(byte data)
        {
            this.DcPin.Write(true);
            this.SpiBuffer1[0] = data;
            SendData(this.SpiBuffer1);
        }

        /// <summary>
        /// Send an array of data to the screen
        /// </summary>
        /// <param name="data">data</param>
        private void SendData(byte[] data)
        {
            this.DcPin.Write(true);
            this.Spi.Write(data);
        }
    }
}
