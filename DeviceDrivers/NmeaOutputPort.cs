// 
// WindInstrumentToNMEA - NmeaOutputPort.cs
// 
// Created 01 - 2013
// 
// Alex Wetmore

using System.IO;
using System.IO.Ports;
using System.Text;
using Microsoft.SPOT;

namespace WindInstrumentToNMEA
{
    /// <summary>
    ///     Write NMEA 0183 sentences to a Serial Port.
    /// </summary>
    internal class NmeaOutputPort
    {
        /// <summary>
        ///     The NMEA talker ID used by this object.  II means "integrated instrumentation".
        /// </summary>
        private const string talkerId = "II";

        /// <summary>
        ///     The serial port that we write sentences to
        /// </summary>
        private readonly SerialPort m_serialPort;

        /// <summary>
        ///     Initialize the NMEA port.
        /// </summary>
        /// <param name="port">The serial port to use.  COM1 is pins0 and 1, COM2 is pins 2 and 3.</param>
        /// <param name="baudrate">The baud rate to communicate at.  9600 or 38400 are recommended.</param>
        public NmeaOutputPort(SerialPort serialPort)
        {
            m_serialPort = serialPort;
        }

        /// <summary>
        /// Write directly to the output port.  This is used by the NmeaRepeater class
        /// </summary>
        /// <param name="buffer">The bytes to write</param>
        /// <param name="bytesToWrite">How many bytes should be written?</param>
        public void WriteRaw(byte[] buffer, int bytesToWrite)
        {
            m_serialPort.Write(buffer, 0, bytesToWrite);
        }

        /// <summary>
        ///     Output a magnetic heading sentence with this form:
        ///     HDM,x.x,M
        ///     x.x: Heading Degrees, magnetic
        ///     M: magnetic
        /// </summary>
        /// <param name="heading">The heading to write.</param>
        public void WriteHeadingMagnetic(int heading)
        {
            WriteSentence("HDM," + heading + ".0,M");
        }

        /// <summary>
        /// Write pitch and roll sentence.  Sort of loosely based on the Maretron PMAROUT.  Format:
        /// PRA,r.r,p.p
        /// r.r = roll angle, negative for port
        /// p.p = pitch angle, negative for bow down
        /// </summary>
        /// <param name="pitch">pitch, negative for bow down</param>
        /// <param name="roll">roll, negative for port</param>
        public void WritePitchAndRollAngles(double pitch, double roll)
        {
            WriteSentence("PRA," + pitch.ToString("N1") + "," + roll.ToString("N1"));
        }

        /// <summary>
        ///     Output a Wind Heading and velocity sentence with this format:
        ///     MWV,x.x,R,v.v,M,A
        ///     x.x = relative heading of the wind
        ///     v.v = the wind velocity in knots
        /// </summary>
        /// <param name="heading">Relative heading of the wind (0 == forward)</param>
        /// <param name="velocity">The velocity of the wind</param>
        public void WriteWindHeadingAndVelocity(double heading, double velocity)
        {
            WriteSentence("MWV," + heading.ToString("N1") + ",R," + velocity.ToString("N1") + ",N,A");
        }

        /// <summary>
        ///     Write a NMEA sentence to the serial port.  This function prefixes the supplied sentence with a
        ///     talker ID and postfixes it with a checksum.
        /// </summary>
        /// <exception cref="IOException">IOException is thrown when the write fails</exception>
        /// <param name="sentence">The sentence to write</param>
        private void WriteSentence(string sentence)
        {
            // prefix the talkerId
            string fulldata = talkerId + sentence;

            // the checksum
            byte checksum = 0;

            // compute checksum
            for (int i = 0; i < fulldata.Length; i++)
            {
                var c = (byte) fulldata[i];
                checksum ^= c;
            }

            // append checksum
            string outputdata = "$" + fulldata + "*" + checksum.ToString("x2");

            byte[] outputbytes = Encoding.UTF8.GetBytes(outputdata + "\r\n");

            lock (this)
            {
                DebugLog.WriteLine("sending:" + outputdata);
                this.m_serialPort.Write(outputbytes, 0, outputbytes.Length);
            }
        }
    }
}

// The rest of this has notes on the Raymarine e7d wiring and supported sentences.

// Raymarine e7d wiring:
// NMEA port 1:  4800/9600 baud
// white: +ve in
// green: -ve in
// yellow: +ve out
// brown: -ve out
// NMEA port 2:  4800/9600/38400 baud
// orange/white: +ve in
// orange/green: -ve in
// orange/yellow: +ve out
// orange/brown: -ve out
// NMEA port 3: 4800 baud
// blue/white: +ve in
// blue/green: -ve in

// We fake it by using gnd for all -ve pins

// This is what my Raymarine e7d supports:

// Transmit
// APB - Autopilot b
// BWC - Bearing and distance to waypoint
// BWR - Bearing and distance to waypoint rhumb line
// DBT - Depth below transducer
// DPT - Depth
// MTW - Water temperature
// RMB - Recommended minimum navigation information
// RSD - Radar system data
// TTM - Tracked target message
// VHW - Water speed and heading
// VLW - Distance travelled through the water
// GGA - Global positioning system fix data
// GLL - Geographic position latitude longitude
// GSA - GPS DOP and active satellites
// GSV - GPS satellites in view
// RMA - Recommended minimum specific loran c data
// RMC - Recommended minimum specific GPS transit data
// VTG - Course over ground and ground speed
// ZDA - Time and date
// MWV - Wind speed and angle
// RTE - Routes sentence
// WPL - Waypoint location sentence

// Receive
// AAM - Waypoint arrival alarm sentence
// DBT - Depth below transducer sentence
// DPT - Depth sentence
// DTM - Datum reference sentence
// APB - Autopilot b sentence
// BWC - Bearing and distance to waypoint sentence
// BWR - Bearing and distance to waypoint rhumb line sentence
// DSC - Digital selective calling information sentence
// DSE - Distress sentence expansion
// GGA - Global positioning system fix data sentence
// GLC - Geographic position loran c sentence
// GLL - Geographic position latitude longitude sentence
// GSA - GPS DOP and active satellites sentence
// GSV - GPS satellites in view sentence
// HDG - Heading deviation and variation sentence
// HDT - Heading true sentence
// HDM - Heading magnetic sentence
// MSK - MSK receiver interface sentence
// MSS - MSK receive r signal status sentence
// MTW - Water temperature sentence
// MWV - Wind speed and angle sentence
// RMA - Recommended minimum specific loran c data sentence
// RMB - Recommended minimum navigation information sentence
// RMC - Recommended minimum specific GPS transit data sentence
// VHW - Water speed and heading sentence
// VLW - Distance travelled through the water sentence
// VTG - Course over ground and ground speed sentence
// XTE - Cross track error measured sentence
// ZDA - Time and date sentence
// MDA - Meteorological composite sentence
// GBS -
// GPS - satellite fault detection data sentence
// RTE - Routes sentence
// WPL - Waypoint location sentence
