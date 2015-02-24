// 
// SailboatComputer - NmeaInputPort.cs
// 
// Created 01 - 2013
// 
// Alex Wetmore

using System;
using System.IO.Ports;
using Microsoft.SPOT;

namespace SailboatComputer
{
    /// <summary>
    ///     This implements a serial port which can read NMEA sentences.
    /// </summary>
    internal class NmeaRepeater
    {
        private readonly SerialPort m_inputPort;
        private readonly NmeaOutputPort m_outputPort;
        private readonly NmeaDebugHandler m_debugHandler;

        /// <summary>
        ///     input buffer.  Note that the length here specifies the maximum
        ///     nmea string that we can read
        /// </summary>
        private byte[] m_buffer = new byte[1024];

        /// <summary>
        /// This copies everything read from it's serial port out to another serial port
        /// </summary>
        /// <param name="port">COM1 for pins 0 and 1, COM2 for pins 2 and 3</param>
        /// <param name="baudrate">The baudrate to use.  9600 or 38400 are recommended</param>
        public NmeaRepeater(SerialPort inputPort, NmeaOutputPort outputPort, NmeaDebugHandler debugHandler)
        {
            m_inputPort = inputPort;
            m_outputPort = outputPort;
            m_debugHandler = debugHandler;
            m_inputPort.DataReceived += inputPort_DataReceived;
        }

        /// <summary>
        ///     This event is called whenever new data is available on our
        ///     serial port.  This method is responsible for managing the input
        ///     buffer and turning the bytes into strings that downstream methods
        ///     can parse.
        /// </summary>
        /// <param name="ignored1">not used</param>
        /// <param name="ignored2">not used</param>
        private void inputPort_DataReceived(object ignored1, SerialDataReceivedEventArgs ignored2)
        {
            lock (this)
            {
                int toRead = m_buffer.Length;
                int bytesRead = m_inputPort.Read(m_buffer, 0, toRead);

                if (m_debugHandler != null)
                {
                    BuildDebugData(m_buffer, bytesRead);
                }

                m_outputPort.WriteRaw(m_buffer, bytesRead);
            }
        }

        /// <summary>
        /// This does a quick scan of data looking for what might be a NMEA sentence.  This is hacky
        /// and doesn't do things like checksums or remember state from the last call.  It is good
        /// enough to light up a simple trace on the debug screen, not much else
        /// </summary>
        /// <param name="data">The data to scan</param>
        /// <param name="length">The length of the data</param>
        private void BuildDebugData(byte[] data, int length)
        {
            // we look for $xxHDR and will remember HDR
            for (int i = 0; i < length - 6; i++)
            {
                char[] hdr = new char[3];
                if (data[i] == '$')
                {
                    hdr[0] = (char) data[i + 3];
                    hdr[1] = (char) data[i + 4];
                    hdr[2] = (char) data[i + 5];
                    string sentenceType = new string(hdr);
                    m_debugHandler(sentenceType);
                }
            }
        }

        public delegate void NmeaDebugHandler(string sentenceType);
    }
}
