// 
// SailboatComputer - NmeaInputPort.cs
// 
// Created 01 - 2013
// 
// Alex Wetmore

using System;
using System.Collections;
using System.IO.Ports;
using Microsoft.SPOT;

namespace SailboatComputer
{
    /// <summary>
    ///     This implements a serial port which can read NMEA sentences.
    /// </summary>
    internal class NmeaInputPort
    {
        private readonly SerialPort m_serialPort;

        /// <summary>
        ///     input buffer.  Note that the length here specifies the maximum
        ///     nmea string that we can read
        /// </summary>
        private byte[] m_buffer = new byte[1024];

        /// <summary>
        ///     the number of bytes already filled on m_buffer
        /// </summary>
        private int m_bufferUsed;

        /// <summary>
        ///     Initialize a new port for reading.
        /// </summary>
        /// <param name="port">COM1 for pins 0 and 1, COM2 for pins 2 and 3</param>
        /// <param name="baudrate">The baudrate to use.  9600 or 38400 are recommended</param>
        public NmeaInputPort(SerialPort serialPort)
        {
            m_serialPort = serialPort;
            this.m_sentenceParsers.Add("MWV", new SentenceParser(ParseWindSpeedAndVelocity));
            //this.m_sentenceParsers.Add("HDM", new ParseSentence(ParseHeadingMagnetic));
            //this.m_sentenceParsers.Add("PRA", new ParseSentence(ParsePitchAndRoll));
            this.m_sentenceParsers.Add("VTG", new SentenceParser(ParseCOGandSOG));
            this.m_sentenceParsers.Add("DPT", new SentenceParser(ParseDepth));
        }

        public void Initialize()
        {
            m_serialPort.DiscardInBuffer();
            m_serialPort.DataReceived += serialPort_DataReceived;
        }

        /// <summary>
        ///     This event is called whenever new data is available on our
        ///     serial port.  This method is responsible for managing the input
        ///     buffer and turning the bytes into strings that downstream methods
        ///     can parse.
        /// </summary>
        /// <param name="ignored1">not used</param>
        /// <param name="ignored2">not used</param>
        private void serialPort_DataReceived(object ignored1, SerialDataReceivedEventArgs ignored2)
        {
            lock (this)
            {
                // if the buffer was all scanned and not useful then dump it
                if (m_bufferUsed == m_buffer.Length)
                {
                    m_bufferUsed = 0;
                }

                // assume that all of the existing buffer was already scanned
                // for newlines.  We will scan what we just received.
                int lastScanIndex = this.m_bufferUsed;

                // this hack backs up our scan by one byte if we saw the CR, 
                // but didn't find the LF because it's in the next byte
                if (this.m_bufferUsed > 0 && this.m_buffer[this.m_bufferUsed - 1] == '\r')
                {
                    lastScanIndex--;
                }

                // read all of the data that is avaiable
                int toRead = System.Math.Min(this.m_serialPort.BytesToRead, (this.m_buffer.Length - this.m_bufferUsed));
                int bytesRead = this.m_serialPort.Read(this.m_buffer, this.m_bufferUsed, toRead);

                // publish the debug event if one is registered
                if (this.RawDataDebugEvent != null)
                {
                    byte[] rawDataBuffer = new byte[bytesRead];
                    Array.Copy(m_buffer, m_bufferUsed, rawDataBuffer, 0, bytesRead);
                    var rawData = new NmeaInputDebugEventArgs(rawDataBuffer);
                    RawDataDebugEvent(this, rawData);
                }

                this.m_startProcessing = DateTime.Now.Ticks;
                this.m_bufferUsed += bytesRead;

                // loop looking for newlines
                bool foundNewline = true;
                while (foundNewline)
                {
                    // scan the buffer for a new line from the last point scanned
                    int nextNewline = 0;
                    int pastNewline = 0;
                    int i;
                    for (i = lastScanIndex; i < this.m_bufferUsed; i++)
                    {
                        // check for a \r\n line ending. 
                        if ((this.m_bufferUsed - i) >= 2 &&
                            (this.m_buffer[i] == (byte) '\r' && this.m_buffer[i + 1] == (byte) '\n'))
                        {
                            nextNewline = i;
                            pastNewline = i + 2;
                            break;
                        }
                            // check for a \n line ending
                        else if (this.m_buffer[i] == (byte) '\n')
                        {
                            nextNewline = i;
                            pastNewline = i + 1;
                            break;
                        }
                    }

                    // we found a line ending
                    if (nextNewline > 0)
                    {
                        char[] chars = System.Text.Encoding.UTF8.GetChars(this.m_buffer, 0, nextNewline);
                        string line = new string(chars);
                        if (line != null)
                        {
                            ParseNmeaSentence(line);
                            m_startProcessing = DateTime.Now.Ticks;
                        }

                        // shuffle the buffer.  not very efficient, we really should use a circular array
                        int toCopy = this.m_bufferUsed - pastNewline;
                        if (toCopy == 0)
                        {
                            // we used the whole buffer, just reset what we've got
                            this.m_bufferUsed = 0;
                        }
                        else
                        {
                            var newBuffer = new byte[this.m_buffer.Length];
                            Array.Copy(this.m_buffer, pastNewline, newBuffer, 0, this.m_buffer.Length - pastNewline);
                            this.m_bufferUsed = toCopy + 1;
                            this.m_buffer = newBuffer;
                        }
                    }
                    else
                    {
                        foundNewline = false;
                    }
                }
            }
        }

        /// <summary>
        /// Parse a complete NMEA sentence
        /// </summary>
        /// <param name="sentence">The sentence to parse</param>
        private void ParseNmeaSentence(string sentence)
        {
            DebugLog.WriteLine("NmeaInputPort: " + sentence);

            // shortest legal would be:
            // $xxVVV.*xx
            if (sentence.Length < 10)
            {
                DebugLog.WriteLine("invalid sentence, too short:" + sentence);
                return;
            }

            // pull off the global structure of the sentence
            string verb = sentence.Substring(3, 3);

            // see if we have a parser for this verb.  If not we'll jump out now
            SentenceParser parser = (SentenceParser) m_sentenceParsers[verb];
            if (parser == null)
            {
                DebugLog.WriteLine("skipped unknown verb: " + sentence);
                long processingTime = DateTime.Now.Ticks - m_startProcessing;
                if (this.ParsedDataDebugEvent != null)
                {
                    this.ParsedDataDebugEvent(this, new NmeaInputParsedDebugEventArgs(sentence, processingTime));
                }
                return;
            }

            // must start with $
            if (sentence[0] != '$')
            {
                DebugLog.WriteLine("invalid sentence, no prefix: " + sentence);
                return;
            }

            string talkerId = sentence.Substring(1, 2);
            string checksumSuffix = sentence.Substring(sentence.Length - 3);

            // the checksum is the last 3 characters and starts with *
            if (checksumSuffix[0] != '*')
            {
                DebugLog.WriteLine("Invalid NMEA, weird checksum: " + sentence);
                return;
            }

            // the digits in the checksum are hex, convert them to a byte
            byte checksum;
            try
            {
                checksum = (byte) Convert.ToInt32(checksumSuffix.Substring(1), 16);
            }
            catch (Exception e)
            {
                DebugLog.WriteLine("Invalid NMEA, bad checksum: " + sentence + " e:" + e.ToString());
                return;
            }

            // compute the checksum and see if it matches
            byte computedChecksum = 0;
            string checkedString = sentence.Substring(1, sentence.Length - 4);
            for (int i = 0; i < checkedString.Length; i++)
            {
                var c = (byte)checkedString[i];
                computedChecksum ^= c;
            }

            if (checksum != computedChecksum)
            {
                DebugLog.WriteLine("Checksum failed: " + sentence);
                return;
            }

            // we send the sentence without the prefix and suffix to verb parsers
            string usefulSentence = sentence.Substring(3, sentence.Length - 6);

            // call the parser
            parser(talkerId, usefulSentence.Split(','), sentence);

            if (this.ParsedDataDebugEvent != null)
            {
                long processingTime = DateTime.Now.Ticks - m_startProcessing;
                this.ParsedDataDebugEvent(this, new NmeaInputParsedDebugEventArgs(sentence, processingTime));
            }
        }

        /// <summary>
        /// Parse VTG verb with this structure:
        ///    VTG,t.t,T,m.m,M,s.s,N,k.k,K,m
        ///    t.t: true heading
        ///    m.m: mag heading
        ///    s.s: speed knots
        ///    k.k: speed k/mh
        /// </summary>
        /// <param name="talkerId"></param>
        /// <param name="words"></param>
        /// <param name="sentence"></param>
        private void ParseCOGandSOG(string talkerId, string[] words, string sentence)
        {
            double cogT;
            double cogM;
            double sog;

            if (words.Length < 6)
            {
                DebugLog.WriteLine("Invalid VTG sentence: " + sentence);
                return;
            }

            try
            {
                cogT = Double.Parse(words[1]);
                if (cogT < 0 || cogT > 360)
                {
                    DebugLog.WriteLine("Invalid heading: " + sentence);
                    return;
                }
                cogM = Double.Parse(words[3]);
                if (cogM < 0 || cogM > 360)
                {
                    DebugLog.WriteLine("Invalid heading: " + sentence);
                    return;
                }
                sog = Double.Parse(words[5]);
            }
            catch
            {
                DebugLog.WriteLine("Invalid VTG sentence: " + sentence);
                return;
            }

            if (COGSOGEvent != null)
            {
                var args = new NmeaCOGSOGEventArgs(cogT, cogM, sog);
                COGSOGEvent(this, args);
            }
        }

        /// <summary>
        /// Parse a magnetic heading sentence with this structure:
        ///     HDM,x.x,M
        ///     x.x: Heading Degrees, magnetic
        ///     M: magnetic
        /// </summary>
        /// <param name="talkerId"></param>
        /// <param name="sentence"></param>
        private void ParseHeadingMagnetic(string talkerId, string[] words, string sentence)
        {
            if (words.Length != 3)
            {
                DebugLog.WriteLine("Invalid HDM sentence: " + sentence);
                return;
            }

            double heading;
            try
            {
                heading = Double.Parse(words[1]);
                if (heading < 0 || heading > 360)
                {
                    DebugLog.WriteLine("Invalid heading: " + sentence);
                    return;
                }
            }
            catch (Exception e)
            {
                DebugLog.WriteLine("Could not parse double: " + sentence + " e:" + e.ToString());
                return;
            }

            DebugLog.WriteLine("magnetic heading update: " + heading);
            if (HeadingEvent != null)
            {
                var args = new NmeaHeadingEventArgs(heading);
                HeadingEvent(this, args);
            }
        }

        /// <summary>
        /// Parse a depth sentence with this structure:
        ///     DPT,d.d,o.o
        ///     d.d: depth, meters
        ///     o.o: offset from transducer
        /// </summary>
        /// <param name="talkerId"></param>
        /// <param name="sentence"></param>
        private void ParseDepth(string talkerId, string[] words, string sentence)
        {
            if (words.Length < 2)
            {
                DebugLog.WriteLine("Invalid DPT sentence: " + sentence);
                return;
            }

            double depth;
            try
            {
                depth = Double.Parse(words[1]);
            }
            catch (Exception e)
            {
                DebugLog.WriteLine("Could not parse double: " + sentence + " e:" + e.ToString());
                return;
            }

            DebugLog.WriteLine("depth update: depth=" + depth);
            
            if (DepthEvent != null)
            {
                var args = new NmeaDepthEventArgs(depth, 0);
                DepthEvent(this, args);
            }
        }

        ///     Output a Wind Heading and velocity sentence with this format:
        ///     MWV,x.x,R,v.v,M,A
        ///     x.x = relative heading of the wind
        ///     R = relative
        ///     v.v = the wind velocity in MPH
        ///     M/N/K = mph, knots, kph
        private void ParseWindSpeedAndVelocity(string talkerId, string[] words, string sentence)
        {
            if (words.Length != 6)
            {
                DebugLog.WriteLine("invalid MWV sentence: " + sentence);
                return;
            }

            int relativeHeading;
            double velocity;
            try
            {
                int period = words[1].IndexOf('.');
                if (period > 0)
                {
                    relativeHeading = Int32.Parse(words[1].Substring(0, period));
                }
                else
                {
                    relativeHeading = Int32.Parse(words[1]);
                }
                velocity = Double.Parse(words[3]);
            }
            catch (Exception e)
            {
                DebugLog.WriteLine("Could not parse: " + sentence + " e:" + e.ToString());
                return;
            }

            // convert velocity to knots
            switch (words[4])
            {
                case "M":
                    velocity *= 0.868976242;
                    break;
                case "K":
                    velocity *= 0.539956803;
                    break;
                case "N":
                    // velocity is already in knots
                    break;
                default:
                    DebugLog.WriteLine("Invalid wind speed: " + sentence);
                    return;
            }

            DebugLog.WriteLine("Wind velocity update: h=" + relativeHeading + " v=" + velocity);
            if (WindEvent != null)
            {
                var args = new NmeaWindEventArgs(relativeHeading, velocity);
                WindEvent(this, args);
            }
        }

        public event NmeaHeadingEventHandler HeadingEvent;
        public event NmeaWindEventHandler WindEvent;
        public event NmeaCOGSOGEventHandler COGSOGEvent;
        public event NmeaDepthEventHandler DepthEvent;
        public event NmeaInputDebugEventHandler RawDataDebugEvent;
        public event NmeaInputParsedDebugEventHandler ParsedDataDebugEvent;

        private delegate void SentenceParser(string talkerId, string[] words, string sentence);

        private Hashtable m_sentenceParsers = new Hashtable();

        public long m_startProcessing = DateTime.Now.Ticks;
    }

    public class NmeaPitchAndRollEventArgs : EventArgs
    {
        public NmeaPitchAndRollEventArgs(double pitch, double roll)
        {
            m_pitch = pitch;
            m_roll = roll;
        }

        public double Pitch { get { return m_pitch; } }
        public double Roll { get { return m_roll; } }

        readonly private double m_pitch;
        readonly private double m_roll;
    }

    public class NmeaHeadingEventArgs : EventArgs
    {
        public NmeaHeadingEventArgs(double heading)
        {
            m_heading = heading;
        }

        public double Heading { get { return m_heading; } }

        private readonly double m_heading;
    }

    public class NmeaWindEventArgs : EventArgs
    {
        public NmeaWindEventArgs(int relativeHeading, double velocity)
        {
            m_relativeHeading = relativeHeading;
            m_velocity = velocity;
        }

        public int RelativeHeading { get { return m_relativeHeading; } }
        public double Velocity { get { return m_velocity; } }

        private readonly int m_relativeHeading;
        private readonly double m_velocity;
    }

    public class NmeaCOGSOGEventArgs : EventArgs
    {
        public NmeaCOGSOGEventArgs(double cogT, double cogM, double speedKnots)
        {
            m_cogT = cogT;
            m_cogM = cogM;
            m_speedKnots = speedKnots;
        }

        public double cogT { get { return m_cogT; } }
        public double cogM { get { return m_cogM; } }
        public double speedKnots { get { return m_speedKnots; } }

        private readonly double m_cogT;
        private readonly double m_cogM;
        private readonly double m_speedKnots;
    }

    public class NmeaInputDebugEventArgs : EventArgs
    {
        public NmeaInputDebugEventArgs(byte[] data)
        {
            m_data = data;
        }

        public byte[] RawData { get { return m_data; } }

        public byte[] m_data;
    }

    public class NmeaInputParsedDebugEventArgs : EventArgs
    {
        public NmeaInputParsedDebugEventArgs(string sentence, long processingTime)
        {
            m_sentence = sentence;
            m_processingTime = processingTime;
        }

        public string Sentence { get { return m_sentence; } }
        public long ProcessingTime { get { return (m_processingTime / TicksPerMs); } }

        public string m_sentence;
        public long m_processingTime;

        private const int TicksPerMs = 10000;
    }

    public class NmeaDepthEventArgs : EventArgs
    {
        public NmeaDepthEventArgs(double depth, double offset)
        {
            m_depth = depth;
            m_offset = offset;
        }

        public double Depth { get { return m_depth; } }
        public double Offset { get { return m_offset; } }

        private double m_depth;
        private double m_offset;
    }

    public delegate void NmeaPitchAndRollEventHandler(object sender, NmeaPitchAndRollEventArgs args);
    public delegate void NmeaHeadingEventHandler(object sender, NmeaHeadingEventArgs args);
    public delegate void NmeaWindEventHandler(object sender, NmeaWindEventArgs args);
    public delegate void NmeaCOGSOGEventHandler(object sender, NmeaCOGSOGEventArgs args);
    public delegate void NmeaInputDebugEventHandler(object sender, NmeaInputDebugEventArgs args);
    public delegate void NmeaInputParsedDebugEventHandler(object sender, NmeaInputParsedDebugEventArgs args);
    public delegate void NmeaDepthEventHandler(object sender, NmeaDepthEventArgs args);
}
