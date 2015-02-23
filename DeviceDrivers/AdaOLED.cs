// 
// WindInstrumentToNMEA - OLED.cs
// 
// Created 01 - 2013
// 
// Alex Wetmore
//

using System;
using System.Text;
using System.Threading;
using Microsoft.SPOT.Hardware;

// OLED driver, taken from http://netduinohelpers.codeplex.com/

namespace WindInstrumentToNMEA
{
    /// <summary>
    ///     This class is a driver for the monochrome 128x64 OLED display referenced SSD1306.
    ///     http://www.adafruit.com/index.php?main_page=product_info&cPath=37&products_id=326
    ///     The code is an adaptation of the Arduino library written by Limor Fried: https://github.com/adafruit/SSD1306
    /// </summary>
    public class Adafruit1306OledDriver : IDisposable
    {
        public enum Color
        {
            Black,
            White
        }

        public enum VccType
        {
            EXTERNALVCC = 0x1,
            SWITCHCAPVCC = 0x2
        }

        // width and height of screen in pixels
        private const int Width = 128;
        private const int Height = 64;

        protected const bool Data = true;
        protected const bool DisplayCommand = false;
        private const int BufferSize = 1024;

        // This is the 5x7 font used by DrawString and DrawCharacter
        private static readonly byte[] Font = new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00,
            0x3E, 0x5B, 0x4F, 0x5B, 0x3E,
            0x3E, 0x6B, 0x4F, 0x6B, 0x3E,
            0x1C, 0x3E, 0x7C, 0x3E, 0x1C,
            0x18, 0x3C, 0x7E, 0x3C, 0x18,
            0x1C, 0x57, 0x7D, 0x57, 0x1C,
            0x1C, 0x5E, 0x7F, 0x5E, 0x1C,
            0x00, 0x18, 0x3C, 0x18, 0x00,
            0xFF, 0xE7, 0xC3, 0xE7, 0xFF,
            0x00, 0x18, 0x24, 0x18, 0x00,
            0xFF, 0xE7, 0xDB, 0xE7, 0xFF,
            0x30, 0x48, 0x3A, 0x06, 0x0E,
            0x26, 0x29, 0x79, 0x29, 0x26,
            0x40, 0x7F, 0x05, 0x05, 0x07,
            0x40, 0x7F, 0x05, 0x25, 0x3F,
            0x5A, 0x3C, 0xE7, 0x3C, 0x5A,
            0x7F, 0x3E, 0x1C, 0x1C, 0x08,
            0x08, 0x1C, 0x1C, 0x3E, 0x7F,
            0x14, 0x22, 0x7F, 0x22, 0x14,
            0x5F, 0x5F, 0x00, 0x5F, 0x5F,
            0x06, 0x09, 0x7F, 0x01, 0x7F,
            0x00, 0x66, 0x89, 0x95, 0x6A,
            0x60, 0x60, 0x60, 0x60, 0x60,
            0x94, 0xA2, 0xFF, 0xA2, 0x94,
            0x08, 0x04, 0x7E, 0x04, 0x08,
            0x10, 0x20, 0x7E, 0x20, 0x10,
            0x08, 0x08, 0x2A, 0x1C, 0x08,
            0x08, 0x1C, 0x2A, 0x08, 0x08,
            0x1E, 0x10, 0x10, 0x10, 0x10,
            0x0C, 0x1E, 0x0C, 0x1E, 0x0C,
            0x30, 0x38, 0x3E, 0x38, 0x30,
            0x06, 0x0E, 0x3E, 0x0E, 0x06,
            0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x5F, 0x00, 0x00,
            0x00, 0x07, 0x00, 0x07, 0x00,
            0x14, 0x7F, 0x14, 0x7F, 0x14,
            0x24, 0x2A, 0x7F, 0x2A, 0x12,
            0x23, 0x13, 0x08, 0x64, 0x62,
            0x36, 0x49, 0x56, 0x20, 0x50,
            0x00, 0x08, 0x07, 0x03, 0x00,
            0x00, 0x1C, 0x22, 0x41, 0x00,
            0x00, 0x41, 0x22, 0x1C, 0x00,
            0x2A, 0x1C, 0x7F, 0x1C, 0x2A,
            0x08, 0x08, 0x3E, 0x08, 0x08,
            0x00, 0x80, 0x70, 0x30, 0x00,
            0x08, 0x08, 0x08, 0x08, 0x08,
            0x00, 0x00, 0x60, 0x60, 0x00,
            0x20, 0x10, 0x08, 0x04, 0x02,
            0x3E, 0x51, 0x49, 0x45, 0x3E,
            0x00, 0x42, 0x7F, 0x40, 0x00,
            0x72, 0x49, 0x49, 0x49, 0x46,
            0x21, 0x41, 0x49, 0x4D, 0x33,
            0x18, 0x14, 0x12, 0x7F, 0x10,
            0x27, 0x45, 0x45, 0x45, 0x39,
            0x3C, 0x4A, 0x49, 0x49, 0x31,
            0x41, 0x21, 0x11, 0x09, 0x07,
            0x36, 0x49, 0x49, 0x49, 0x36,
            0x46, 0x49, 0x49, 0x29, 0x1E,
            0x00, 0x00, 0x14, 0x00, 0x00,
            0x00, 0x40, 0x34, 0x00, 0x00,
            0x00, 0x08, 0x14, 0x22, 0x41,
            0x14, 0x14, 0x14, 0x14, 0x14,
            0x00, 0x41, 0x22, 0x14, 0x08,
            0x02, 0x01, 0x59, 0x09, 0x06,
            0x3E, 0x41, 0x5D, 0x59, 0x4E,
            0x7C, 0x12, 0x11, 0x12, 0x7C,
            0x7F, 0x49, 0x49, 0x49, 0x36,
            0x3E, 0x41, 0x41, 0x41, 0x22,
            0x7F, 0x41, 0x41, 0x41, 0x3E,
            0x7F, 0x49, 0x49, 0x49, 0x41,
            0x7F, 0x09, 0x09, 0x09, 0x01,
            0x3E, 0x41, 0x41, 0x51, 0x73,
            0x7F, 0x08, 0x08, 0x08, 0x7F,
            0x00, 0x41, 0x7F, 0x41, 0x00,
            0x20, 0x40, 0x41, 0x3F, 0x01,
            0x7F, 0x08, 0x14, 0x22, 0x41,
            0x7F, 0x40, 0x40, 0x40, 0x40,
            0x7F, 0x02, 0x1C, 0x02, 0x7F,
            0x7F, 0x04, 0x08, 0x10, 0x7F,
            0x3E, 0x41, 0x41, 0x41, 0x3E,
            0x7F, 0x09, 0x09, 0x09, 0x06,
            0x3E, 0x41, 0x51, 0x21, 0x5E,
            0x7F, 0x09, 0x19, 0x29, 0x46,
            0x26, 0x49, 0x49, 0x49, 0x32,
            0x03, 0x01, 0x7F, 0x01, 0x03,
            0x3F, 0x40, 0x40, 0x40, 0x3F,
            0x1F, 0x20, 0x40, 0x20, 0x1F,
            0x3F, 0x40, 0x38, 0x40, 0x3F,
            0x63, 0x14, 0x08, 0x14, 0x63,
            0x03, 0x04, 0x78, 0x04, 0x03,
            0x61, 0x59, 0x49, 0x4D, 0x43,
            0x00, 0x7F, 0x41, 0x41, 0x41,
            0x02, 0x04, 0x08, 0x10, 0x20,
            0x00, 0x41, 0x41, 0x41, 0x7F,
            0x04, 0x02, 0x01, 0x02, 0x04,
            0x40, 0x40, 0x40, 0x40, 0x40,
            0x00, 0x03, 0x07, 0x08, 0x00,
            0x20, 0x54, 0x54, 0x78, 0x40,
            0x7F, 0x28, 0x44, 0x44, 0x38,
            0x38, 0x44, 0x44, 0x44, 0x28,
            0x38, 0x44, 0x44, 0x28, 0x7F,
            0x38, 0x54, 0x54, 0x54, 0x18,
            0x00, 0x08, 0x7E, 0x09, 0x02,
            0x18, 0xA4, 0xA4, 0x9C, 0x78,
            0x7F, 0x08, 0x04, 0x04, 0x78,
            0x00, 0x44, 0x7D, 0x40, 0x00,
            0x20, 0x40, 0x40, 0x3D, 0x00,
            0x7F, 0x10, 0x28, 0x44, 0x00,
            0x00, 0x41, 0x7F, 0x40, 0x00,
            0x7C, 0x04, 0x78, 0x04, 0x78,
            0x7C, 0x08, 0x04, 0x04, 0x78,
            0x38, 0x44, 0x44, 0x44, 0x38,
            0xFC, 0x18, 0x24, 0x24, 0x18,
            0x18, 0x24, 0x24, 0x18, 0xFC,
            0x7C, 0x08, 0x04, 0x04, 0x08,
            0x48, 0x54, 0x54, 0x54, 0x24,
            0x04, 0x04, 0x3F, 0x44, 0x24,
            0x3C, 0x40, 0x40, 0x20, 0x7C,
            0x1C, 0x20, 0x40, 0x20, 0x1C,
            0x3C, 0x40, 0x30, 0x40, 0x3C,
            0x44, 0x28, 0x10, 0x28, 0x44,
            0x4C, 0x90, 0x90, 0x90, 0x7C,
            0x44, 0x64, 0x54, 0x4C, 0x44,
            0x00, 0x08, 0x36, 0x41, 0x00,
            0x00, 0x00, 0x77, 0x00, 0x00,
            0x00, 0x41, 0x36, 0x08, 0x00,
            0x02, 0x01, 0x02, 0x04, 0x02,
            0x3C, 0x26, 0x23, 0x26, 0x3C,
            0x1E, 0xA1, 0xA1, 0x61, 0x12,
            0x3A, 0x40, 0x40, 0x20, 0x7A,
            0x38, 0x54, 0x54, 0x55, 0x59,
            0x21, 0x55, 0x55, 0x79, 0x41,
            0x21, 0x54, 0x54, 0x78, 0x41,
            0x21, 0x55, 0x54, 0x78, 0x40,
            0x20, 0x54, 0x55, 0x79, 0x40,
            0x0C, 0x1E, 0x52, 0x72, 0x12,
            0x39, 0x55, 0x55, 0x55, 0x59,
            0x39, 0x54, 0x54, 0x54, 0x59,
            0x39, 0x55, 0x54, 0x54, 0x58,
            0x00, 0x00, 0x45, 0x7C, 0x41,
            0x00, 0x02, 0x45, 0x7D, 0x42,
            0x00, 0x01, 0x45, 0x7C, 0x40,
            0xF0, 0x29, 0x24, 0x29, 0xF0,
            0xF0, 0x28, 0x25, 0x28, 0xF0,
            0x7C, 0x54, 0x55, 0x45, 0x00,
            0x20, 0x54, 0x54, 0x7C, 0x54,
            0x7C, 0x0A, 0x09, 0x7F, 0x49,
            0x32, 0x49, 0x49, 0x49, 0x32,
            0x32, 0x48, 0x48, 0x48, 0x32,
            0x32, 0x4A, 0x48, 0x48, 0x30,
            0x3A, 0x41, 0x41, 0x21, 0x7A,
            0x3A, 0x42, 0x40, 0x20, 0x78,
            0x00, 0x9D, 0xA0, 0xA0, 0x7D,
            0x39, 0x44, 0x44, 0x44, 0x39,
            0x3D, 0x40, 0x40, 0x40, 0x3D,
            0x3C, 0x24, 0xFF, 0x24, 0x24,
            0x48, 0x7E, 0x49, 0x43, 0x66,
            0x2B, 0x2F, 0xFC, 0x2F, 0x2B,
            0xFF, 0x09, 0x29, 0xF6, 0x20,
            0xC0, 0x88, 0x7E, 0x09, 0x03,
            0x20, 0x54, 0x54, 0x79, 0x41,
            0x00, 0x00, 0x44, 0x7D, 0x41,
            0x30, 0x48, 0x48, 0x4A, 0x32,
            0x38, 0x40, 0x40, 0x22, 0x7A,
            0x00, 0x7A, 0x0A, 0x0A, 0x72,
            0x7D, 0x0D, 0x19, 0x31, 0x7D,
            0x26, 0x29, 0x29, 0x2F, 0x28,
            0x26, 0x29, 0x29, 0x29, 0x26,
            0x30, 0x48, 0x4D, 0x40, 0x20,
            0x38, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x38,
            0x2F, 0x10, 0xC8, 0xAC, 0xBA,
            0x2F, 0x10, 0x28, 0x34, 0xFA,
            0x00, 0x00, 0x7B, 0x00, 0x00,
            0x08, 0x14, 0x2A, 0x14, 0x22,
            0x22, 0x14, 0x2A, 0x14, 0x08,
            0xAA, 0x00, 0x55, 0x00, 0xAA,
            0xAA, 0x55, 0xAA, 0x55, 0xAA,
            0x00, 0x00, 0x00, 0xFF, 0x00,
            0x10, 0x10, 0x10, 0xFF, 0x00,
            0x14, 0x14, 0x14, 0xFF, 0x00,
            0x10, 0x10, 0xFF, 0x00, 0xFF,
            0x10, 0x10, 0xF0, 0x10, 0xF0,
            0x14, 0x14, 0x14, 0xFC, 0x00,
            0x14, 0x14, 0xF7, 0x00, 0xFF,
            0x00, 0x00, 0xFF, 0x00, 0xFF,
            0x14, 0x14, 0xF4, 0x04, 0xFC,
            0x14, 0x14, 0x17, 0x10, 0x1F,
            0x10, 0x10, 0x1F, 0x10, 0x1F,
            0x14, 0x14, 0x14, 0x1F, 0x00,
            0x10, 0x10, 0x10, 0xF0, 0x00,
            0x00, 0x00, 0x00, 0x1F, 0x10,
            0x10, 0x10, 0x10, 0x1F, 0x10,
            0x10, 0x10, 0x10, 0xF0, 0x10,
            0x00, 0x00, 0x00, 0xFF, 0x10,
            0x10, 0x10, 0x10, 0x10, 0x10,
            0x10, 0x10, 0x10, 0xFF, 0x10,
            0x00, 0x00, 0x00, 0xFF, 0x14,
            0x00, 0x00, 0xFF, 0x00, 0xFF,
            0x00, 0x00, 0x1F, 0x10, 0x17,
            0x00, 0x00, 0xFC, 0x04, 0xF4,
            0x14, 0x14, 0x17, 0x10, 0x17,
            0x14, 0x14, 0xF4, 0x04, 0xF4,
            0x00, 0x00, 0xFF, 0x00, 0xF7,
            0x14, 0x14, 0x14, 0x14, 0x14,
            0x14, 0x14, 0xF7, 0x00, 0xF7,
            0x14, 0x14, 0x14, 0x17, 0x14,
            0x10, 0x10, 0x1F, 0x10, 0x1F,
            0x14, 0x14, 0x14, 0xF4, 0x14,
            0x10, 0x10, 0xF0, 0x10, 0xF0,
            0x00, 0x00, 0x1F, 0x10, 0x1F,
            0x00, 0x00, 0x00, 0x1F, 0x14,
            0x00, 0x00, 0x00, 0xFC, 0x14,
            0x00, 0x00, 0xF0, 0x10, 0xF0,
            0x10, 0x10, 0xFF, 0x10, 0xFF,
            0x14, 0x14, 0x14, 0xFF, 0x14,
            0x10, 0x10, 0x10, 0x1F, 0x00,
            0x00, 0x00, 0x00, 0xF0, 0x10,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xF0, 0xF0, 0xF0, 0xF0, 0xF0,
            0xFF, 0xFF, 0xFF, 0x00, 0x00,
            0x00, 0x00, 0x00, 0xFF, 0xFF,
            0x0F, 0x0F, 0x0F, 0x0F, 0x0F,
            0x38, 0x44, 0x44, 0x38, 0x44,
            0x7C, 0x2A, 0x2A, 0x3E, 0x14,
            0x7E, 0x02, 0x02, 0x06, 0x06,
            0x02, 0x7E, 0x02, 0x7E, 0x02,
            0x63, 0x55, 0x49, 0x41, 0x63,
            0x38, 0x44, 0x44, 0x3C, 0x04,
            0x40, 0x7E, 0x20, 0x1E, 0x20,
            0x06, 0x02, 0x7E, 0x02, 0x02,
            0x99, 0xA5, 0xE7, 0xA5, 0x99,
            0x1C, 0x2A, 0x49, 0x2A, 0x1C,
            0x4C, 0x72, 0x01, 0x72, 0x4C,
            0x30, 0x4A, 0x4D, 0x4D, 0x30,
            0x30, 0x48, 0x78, 0x48, 0x30,
            0xBC, 0x62, 0x5A, 0x46, 0x3D,
            0x3E, 0x49, 0x49, 0x49, 0x00,
            0x7E, 0x01, 0x01, 0x01, 0x7E,
            0x2A, 0x2A, 0x2A, 0x2A, 0x2A,
            0x44, 0x44, 0x5F, 0x44, 0x44,
            0x40, 0x51, 0x4A, 0x44, 0x40,
            0x40, 0x44, 0x4A, 0x51, 0x40,
            0x00, 0x00, 0xFF, 0x01, 0x03,
            0xE0, 0x80, 0xFF, 0x00, 0x00,
            0x08, 0x08, 0x6B, 0x6B, 0x08,
            0x36, 0x12, 0x36, 0x24, 0x36,
            0x06, 0x0F, 0x09, 0x0F, 0x06,
            0x00, 0x00, 0x18, 0x18, 0x00,
            0x00, 0x00, 0x10, 0x10, 0x00,
            0x30, 0x40, 0xFF, 0x01, 0x01,
            0x00, 0x1F, 0x01, 0x01, 0x1E,
            0x00, 0x19, 0x1D, 0x17, 0x12,
            0x00, 0x3C, 0x3C, 0x3C, 0x3C,
            0x00, 0x00, 0x00, 0x00, 0x00
        };

        protected SPI Spi;
        protected byte[] SpiBuffer = new byte[1];
        protected OutputPort DcPin;
        public byte[] DisplayBuffer = new byte[BufferSize];
        protected OutputPort ResetPin;

        public Adafruit1306OledDriver(
            Cpu.Pin dc,
            Cpu.Pin reset,
            Cpu.Pin chipSelect,
            SPI.SPI_module spiModule = SPI.SPI_module.SPI1,
            uint speedKHz = 10000)
        {
            AutoRefreshScreen = true;

            var spiConfig = new SPI.Configuration(
                SPI_mod: spiModule,
                ChipSelect_Port: chipSelect,
                ChipSelect_ActiveState: false,
                ChipSelect_SetupTime: 0,
                ChipSelect_HoldTime: 0,
                Clock_IdleState: false,
                Clock_Edge: true,
                Clock_RateKHz: speedKHz
                );

            this.Spi = new SPI(spiConfig);

            this.DcPin = new OutputPort(dc, false);
            this.ResetPin = new OutputPort(reset, false);
        }

        public bool AutoRefreshScreen { get; set; }

        public void Dispose()
        {
            this.DcPin.Dispose();
            this.ResetPin.Dispose();
            this.Spi.Dispose();

            this.DcPin = null;
            this.ResetPin = null;
            this.Spi = null;
            this.SpiBuffer = null;
            this.DisplayBuffer = null;
        }

        public void InvertDisplay(bool cmd)
        {
            this.DcPin.Write(DisplayCommand);

            if (cmd)
            {
                SendCommand(Command.INVERTDISPLAY);
            }
            else
            {
                SendCommand(Command.NORMALDISPLAY);
            }

            this.DcPin.Write(Data);
        }

        /// <summary>
        /// Fill the screen with the contents of a bitmap laid out in native screen format. 
        /// 
        /// That means each byte contains 8 vertical pixels starting from the top.  That is called a line.  Byte
        /// order increases by X until 127 (end of screen), then goes down one line (8 pixels) before starting the 
        /// next line.
        /// 
        /// BMPtoArray (another simple tool that I've written) will convert a 128x64 pixel monochrome bitmap to this
        /// structure.
        /// 
        /// This is a very fast way to pre-load the screen contents with a form.
        /// </summary>
        /// <param name="sourceBitmap">the bitmap to display</param>
        public void DrawBitmap(byte[] sourceBitmap)
        {
            if (sourceBitmap.Length != this.DisplayBuffer.Length)
            {
                throw new ArgumentException("invalid source bitmap");
            }
            sourceBitmap.CopyTo(this.DisplayBuffer, 0);
            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        /*
        /// <summary>
        /// Draw a string using a variable width font.  Long strings will not wrap.
        /// </summary>
        /// <param name="x">The X coordinate for the top left of the string box</param>
        /// <param name="y">The Y coordinate for the top left of the string box</param>
        /// <param name="str">The string to draw</param>
        /// <param name="font">The font to use</param>
        public void DrawStringFont(int x, int y, string str, FontInfo font)
        {
            int fontHeight = font.CharacterHeight;
            int padding = font.CharacterPadding;

            byte[] ascii = Encoding.UTF8.GetBytes(str);

            foreach (byte c in ascii)
            {
                byte charWidth = DrawCharacterFont(x, y, c, font);

                x += charWidth + padding;

                if (x + charWidth >= Width)
                {
                    // we'd overrun the line, stop drawing
                    break;
                }
            }
            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Draw one character on the screen using a variable width font.
        /// </summary>
        /// <param name="x">The top left of the character box</param>
        /// <param name="y">The bottom right of the character box</param>
        /// <param name="c">The character to draw</param>
        /// <param name="font">The font to use</param>
        /// <returns>The width of the character that was drawn.</returns>
        protected byte DrawCharacterFont(int x, int y, byte c, FontInfo font)
        {
            if (!font.IsCharacterInFont(c))
            {
                throw new ArgumentException("c");
            }

            byte widthBits = font.GetCharacterWidth(c);
            byte[] bitmap = font.GetCharacterBitmap(c);
            int height = font.CharacterHeight;
            int lines = height / 8 + ((height % 8 == 0) ? 0 : 1);

            int lineOffset = y / 8;
            int yOffset = y % 8;
            int lineIndex = 0;
            byte lastLine = 0;
            byte lastLineMask = (byte) ~((2 ^ (8 - yOffset)) - 1);
            for (int i = 0; i < bitmap.Length; i++)
            {
                byte currentLine = bitmap[i];
                if (yOffset != 0)
                {
                    currentLine =
                        (byte) (((currentLine << yOffset) & 0xff) | ((lastLine & lastLineMask) >> (8 - yOffset)));
                    lastLine = bitmap[i];
                }
                this.DisplayBuffer[x + ((lineOffset + lineIndex) * 128)] = currentLine;
                lineIndex = (lineIndex + 1) % lines;
                if (lineIndex == 0)
                {
                    x++;
                }
            }

            return widthBits;
        }*/

        /// <summary>
        /// Draw a string using the 5x7 fixed font.  Vertical position is line oriented, not pixel oriented
        /// as with DrawStringFont.  Long strings will wrap to the next line.
        /// </summary>
        /// <param name="x">The left edge of the string</param>
        /// <param name="line">The line to drawn on.  There are 8 lines on the screen, 0 is at the top</param>
        /// <param name="str">The string to draw.</param>
        public void DrawString(int x, int line, string str)
        {
            foreach (Char c in str)
            {
                DrawCharacter(x, line, c);

                x += 6; // 6 pixels wide

                if (x + 6 >= Width)
                {
                    x = 0; // ran out of this line
                    line++;
                }

                if (line >= Height / 8)
                {
                    return; // ran out of space :(
                }
            }
            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Draw a single character using the fixed 5x7 font.
        /// </summary>
        /// <param name="x">The left edge of the character</param>
        /// <param name="line">The line to draw on</param>
        /// <param name="c">The character to draw</param>
        protected void DrawCharacter(int x, int line, Char c)
        {
            for (int i = 0; i < 5; i++)
            {
                this.DisplayBuffer[x + (line * 128)] = Font[(c * 5) + i];
                x++;
            }
        }

        
        /// <summary>
        /// Draw a line on the screen
        /// 
        /// bresenham's algorithm - thx wikipedia
        /// </summary>
        /// <param name="x0">x coordinate for start of line</param>
        /// <param name="y0">y coordinate for start of line</param>
        /// <param name="x1">x coordinate for end of line</param>
        /// <param name="y1">y coordinate for end of line</param>
        /// <param name="color">line color</param>
        public void DrawLine(int x0, int y0, int x1, int y1, Color color)
        {
            int steep = (System.Math.Abs(y1 - y0) > System.Math.Abs(x1 - x0)) ? 1 : 0;

            if (steep != 0)
            {
                Swap(ref x0, ref y0);
                Swap(ref x1, ref y1);
            }

            if (x0 > x1)
            {
                Swap(ref x0, ref x1);
                Swap(ref y0, ref y1);
            }

            int dx, dy;
            dx = x1 - x0;
            dy = System.Math.Abs(y1 - y0);

            int err = dx / 2;
            int ystep;

            if (y0 < y1)
            {
                ystep = 1;
            }
            else
            {
                ystep = -1;
            }

            for (; x0 < x1; x0++)
            {
                if (steep != 0)
                {
                    SetPixel(y0, x0, color);
                }
                else
                {
                    SetPixel(x0, y0, color);
                }

                err -= dy;

                if (err < 0)
                {
                    y0 += ystep;
                    err += dx;
                }
            }

            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Draw a rectangle
        /// </summary>
        /// <param name="x">x of top left</param>
        /// <param name="y">y of top left</param>
        /// <param name="w">width in pixels</param>
        /// <param name="h">height in pixels</param>
        /// <param name="color">color</param>
        public void DrawRectangle(int x, int y, int w, int h, Color color)
        {
            for (int i = x; i < x + w; i++)
            {
                SetPixel(i, y, color);
                SetPixel(i, y + h - 1, color);
            }
            for (int i = y; i < y + h; i++)
            {
                SetPixel(x, i, color);
                SetPixel(x + w - 1, i, color);
            }
            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        public void FillRectangle(int x, int y, int w, int h, Color color)
        {
            for (int i = x; i < x + w; i++)
            {
                for (int j = y; j < y + h; j++)
                {
                    SetPixel(i, j, color);
                }
            }
            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        public void DrawCircle(int x0, int y0, int r, Color color)
        {
            int f = 1 - r;
            int ddF_x = 1;
            int ddF_y = -2 * r;
            int x = 0;
            int y = r;

            SetPixel(x0, y0 + r, color);
            SetPixel(x0, y0 - r, color);
            SetPixel(x0 + r, y0, color);
            SetPixel(x0 - r, y0, color);

            while (x < y)
            {
                if (f >= 0)
                {
                    y--;
                    ddF_y += 2;
                    f += ddF_y;
                }

                x++;
                ddF_x += 2;
                f += ddF_x;

                SetPixel(x0 + x, y0 + y, color);
                SetPixel(x0 - x, y0 + y, color);
                SetPixel(x0 + x, y0 - y, color);
                SetPixel(x0 - x, y0 - y, color);

                SetPixel(x0 + y, y0 + x, color);
                SetPixel(x0 - y, y0 + x, color);
                SetPixel(x0 + y, y0 - x, color);
                SetPixel(x0 - y, y0 - x, color);
            }
            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        public void FillCircle(int x0, int y0, int r, Color color)
        {
            int f = 1 - r;
            int ddF_x = 1;
            int ddF_y = -2 * r;
            int x = 0;
            int y = r;

            for (int i = y0 - r; i <= y0 + r; i++)
            {
                SetPixel(x0, i, color);
            }

            while (x < y)
            {
                if (f >= 0)
                {
                    y--;
                    ddF_y += 2;
                    f += ddF_y;
                }

                x++;
                ddF_x += 2;
                f += ddF_x;

                for (int i = y0 - y; i <= y0 + y; i++)
                {
                    SetPixel(x0 + x, i, color);
                    SetPixel(x0 - x, i, color);
                }

                for (int i = y0 - x; i <= y0 + x; i++)
                {
                    SetPixel(x0 + y, i, color);
                    SetPixel(x0 - y, i, color);
                }
            }
            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Set the color of a single pixel on the screen.
        /// </summary>
        /// <param name="x">x of pixel</param>
        /// <param name="y">y of pixel</param>
        /// <param name="color">color</param>
        public void SetPixel(int x, int y, Color color)
        {
            if ((x >= Width) || (y >= Height))
            {
                return;
            }

            if (color == Color.White)
            {
                this.DisplayBuffer[x + (y / 8) * 128] |= (byte) (1 << (y % 8));
            }
            else
            {
                this.DisplayBuffer[x + (y / 8) * 128] &= (byte) ~(1 << (y % 8));
            }
        }

        /// <summary>
        /// Send a command to the screen
        /// </summary>
        /// <param name="cmd">The command</param>
        protected void SendCommand(Command cmd)
        {
            this.SpiBuffer[0] = (byte) cmd;
            this.Spi.Write(this.SpiBuffer);
        }

        /// <summary>
        /// Refresh the screen using the contents of the display buffer
        /// </summary>
        public virtual void Refresh() { this.Spi.Write(this.DisplayBuffer); }

        /// <summary>
        /// Clear the screen
        /// </summary>
        public void ClearScreen()
        {
            this.DisplayBuffer[0] = 0;
            this.DisplayBuffer[1] = 0;
            this.DisplayBuffer[2] = 0;
            this.DisplayBuffer[3] = 0;
            this.DisplayBuffer[4] = 0;
            this.DisplayBuffer[5] = 0;
            this.DisplayBuffer[6] = 0;
            this.DisplayBuffer[7] = 0;
            this.DisplayBuffer[8] = 0;
            this.DisplayBuffer[9] = 0;
            this.DisplayBuffer[10] = 0;
            this.DisplayBuffer[11] = 0;
            this.DisplayBuffer[12] = 0;
            this.DisplayBuffer[13] = 0;
            this.DisplayBuffer[14] = 0;
            this.DisplayBuffer[15] = 0;
            Array.Copy(this.DisplayBuffer, 0, this.DisplayBuffer, 16, 16);
            Array.Copy(this.DisplayBuffer, 0, this.DisplayBuffer, 32, 32);
            Array.Copy(this.DisplayBuffer, 0, this.DisplayBuffer, 64, 64);
            Array.Copy(this.DisplayBuffer, 0, this.DisplayBuffer, 128, 128);
            Array.Copy(this.DisplayBuffer, 0, this.DisplayBuffer, 256, 256);
            Array.Copy(this.DisplayBuffer, 0, this.DisplayBuffer, 512, 511);

            if (AutoRefreshScreen)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Turn on the display
        /// </summary>
        public void DisplayOn()
        {
            SendCommand(Command.DISPLAYON);
        }

        /// <summary>
        /// Turn off the display
        /// </summary>
        public void DisplayOff()
        {
            SendCommand(Command.DISPLAYOFF);
        }

        /// <summary>
        /// Initialize the OLED for use
        /// </summary>
        /// <param name="vcctype">Which VCC input should be used?</param>
        public void Initialize(VccType vcctype = VccType.SWITCHCAPVCC)
        {
            this.ResetPin.Write(true);
            Thread.Sleep(1); // VDD (3.3V) goes high at start, lets just chill for a ms
            this.ResetPin.Write(false); // bring reset low
            Thread.Sleep(10); // wait 10ms
            this.ResetPin.Write(true); // bring out of reset

            this.DcPin.Write(DisplayCommand);

            SendCommand(Command.DISPLAYOFF); // 0xAE
            SendCommand(Command.SETLOWCOLUMN | 0x0); // low col = 0
            SendCommand(Command.SETHIGHCOLUMN | 0x0); // hi col = 0
            SendCommand(Command.SETSTARTLINE | 0x0); // line #0
            SendCommand(Command.SETCONTRAST); // 0x81

            if (vcctype == VccType.EXTERNALVCC)
            {
                SendCommand((Command) 0x9F); // external 9V
            }
            else
            {
                SendCommand((Command) 0xCF); // chargepump
            }

            SendCommand((Command) 0xA1); // setment remap 95 to 0 (?)
            SendCommand(Command.NORMALDISPLAY); // 0xA6
            SendCommand(Command.DISPLAYALLON_RESUME); // 0xA4
            SendCommand(Command.SETMULTIPLEX); // 0xA8
            SendCommand((Command) 0x3F); // 0x3F 1/64 duty
            SendCommand(Command.SETDISPLAYOFFSET); // 0xD3
            SendCommand(0x0); // no offset
            SendCommand(Command.SETDISPLAYCLOCKDIV); // 0xD5
            SendCommand((Command) 0x80); // the suggested ratio 0x80
            SendCommand(Command.SETPRECHARGE); // 0xd9

            if (vcctype == VccType.EXTERNALVCC)
            {
                SendCommand((Command) 0x22); // external 9V
            }
            else
            {
                SendCommand((Command) 0xF1); // DC/DC
            }

            SendCommand(Command.SETCOMPINS); // 0xDA
            SendCommand((Command) 0x12); // disable COM left/right remap

            SendCommand(Command.SETVCOMDETECT); // 0xDB
            SendCommand((Command) 0x40); // 0x20 is default?

            SendCommand(Command.MEMORYMODE); // 0x20
            SendCommand(0x00); // 0x0 act like ks0108

            // left to right scan
            SendCommand(Command.SEGREMAP | (Command) 0x1);

            SendCommand(Command.COMSCANDEC);

            SendCommand(Command.CHARGEPUMP); //0x8D
            if (vcctype == VccType.EXTERNALVCC)
            {
                SendCommand((Command) 0x10); // disable
            }
            else
            {
                SendCommand((Command) 0x14); // disable    
            }

            SendCommand(Command.DISPLAYON); //--turn on oled panel

            // Switch to 'data' mode
            this.DcPin.Write(Data);
        }

        /// <summary>
        /// Swap two ints
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        private void Swap(ref int a, ref int b)
        {
            var t = a;
            a = b;
            b = t;
        }

        /// <summary>
        /// The command list for this display
        /// </summary>
        protected enum Command
        {
            SETCONTRAST = 0x81,
            DISPLAYALLON_RESUME = 0xA4,
            DISPLAYALLON = 0xA5,
            NORMALDISPLAY = 0xA6,
            INVERTDISPLAY = 0xA7,
            DISPLAYOFF = 0xAE,
            DISPLAYON = 0xAF,
            SETDISPLAYOFFSET = 0xD3,
            SETCOMPINS = 0xDA,
            SETVCOMDETECT = 0xDB,
            SETDISPLAYCLOCKDIV = 0xD5,
            SETPRECHARGE = 0xD9,
            SETMULTIPLEX = 0xA8,
            SETLOWCOLUMN = 0x00,
            SETHIGHCOLUMN = 0x10,
            SETSTARTLINE = 0x40,
            MEMORYMODE = 0x20,
            COMSCANINC = 0xC0,
            COMSCANDEC = 0xC8,
            SEGREMAP = 0xA0,
            CHARGEPUMP = 0x8D
        }
    }
}
