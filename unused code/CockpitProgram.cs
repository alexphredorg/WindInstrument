using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.IO.Ports;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using SailboatComputer.UI;

// Wiring.  The Cockpit Program only works on a Netduino 2 Plus or better (due to memory requirements):
// D0/D1 = COM1 (NMEA input)
// D6 = left button
// D7 = right button
// D8 = OLED chip select
// D9 = OLED reset
// D10 = OLED d/c
// D11 = OLED data
// D13 = OLED clock
namespace SailboatComputer
{
    public class CockpitProgram
    {
        private NmeaInputPort m_nmeaInputPort;
        private Newhaven25664OledDriver m_oled;
        private SimpleUserInterface m_ui;
        private SerialPort m_serialPort;
        private OledFont m_debugFont;
        private const double MetersToFeet = 3.280839895013123;

        public void Go()
        {
            // turn of Netduino devices that we don't need
            PowerManagement.SetPeripheralState(Peripheral.Ethernet, false);
            PowerManagement.SetPeripheralState(Peripheral.PowerLED, false);
            PowerManagement.SetPeripheralState(Peripheral.SDCard, false);

            // initalize the serial ports
            m_serialPort = new SerialPort("COM1", 38400, Parity.None, 8, StopBits.One);
            m_serialPort.Open();

            m_nmeaInputPort = new NmeaInputPort(m_serialPort);

            m_oled = new Newhaven25664OledDriver(
                chipSelect: Pins.GPIO_PIN_D10,
                reset: Pins.GPIO_PIN_D9,
                dc: Pins.GPIO_PIN_D8);
            m_oled.Initialize();
            m_oled.ClearDisplay();
            m_oled.TestPattern();
            Thread.Sleep(1000);
            m_oled.ClearDisplay();

            InputPort leftButton = new InputPort(Pins.GPIO_PIN_D7, false, Port.ResistorMode.PullUp);
            if (!leftButton.Read())
            {
                // debug mode
                //m_nmeaInputPort.RawDataDebugEvent += new NmeaInputDebugEventHandler(m_nmeaInputPort_RawDataDebugEvent);
                m_nmeaInputPort.ParsedDataDebugEvent += new NmeaInputParsedDebugEventHandler(m_nmeaInputPort_ParsedDataDebugEvent);
                m_debugFont = new OledFont(SailboatComputer.Properties.Resources.BinaryResources.fixed5x7);
                m_nmeaInputPort.Initialize();
                while (true)
                {
                    Thread.Sleep(Int16.MaxValue);
                }
            }
            else
            {
                leftButton.Dispose();
                leftButton = null;
                DebugLog.WriteLine("free memory (before fonts) = " + Microsoft.SPOT.Debug.GC(true));

                m_ui = new SimpleUserInterface(m_oled, Pins.GPIO_PIN_D7, Pins.GPIO_PIN_D6);

                DebugLog.WriteLine("free memory (after fonts) = " + Microsoft.SPOT.Debug.GC(true));

                // hook up NMEA events
                m_nmeaInputPort.WindEvent += new NmeaWindEventHandler(m_nmeaInputPort_WindEvent);
                m_nmeaInputPort.COGSOGEvent += new NmeaCOGSOGEventHandler(m_nmeaInputPort_CogSogEvent);
                m_nmeaInputPort.DepthEvent += new NmeaDepthEventHandler(m_nmeaInputPort_DepthEvent);
                m_nmeaInputPort.Initialize();

                while (true)
                {
                    Thread.Sleep(Int16.MaxValue);
                }
            }
        }

        void m_nmeaInputPort_ParsedDataDebugEvent(object sender, NmeaInputParsedDebugEventArgs args)
        {
            m_oled.DrawStringAndScroll(m_debugFont, 0, 63, args.ProcessingTime.ToString() + ":" + args.Sentence);
        }

        void m_nmeaInputPort_RawDataDebugEvent(object sender, NmeaInputDebugEventArgs args)
        {
            char[] chars = System.Text.Encoding.UTF8.GetChars(args.RawData, 0, args.RawData.Length);
            m_oled.DrawStringAndScroll(m_debugFont, 0, 63, new string(chars));
        }

        void m_nmeaInputPort_DepthEvent(object sender, NmeaDepthEventArgs args)
        {
            DisplayVariables.Depth.Value = args.Depth * 3.280839895013123;
        }

        void m_nmeaInputPort_WindEvent(object sender, NmeaWindEventArgs args)
        {
            DisplayVariables.WindDirection.Value = args.RelativeHeading;
            DisplayVariables.WindSpeed.Value = args.Velocity;
        }

        void m_nmeaInputPort_CogSogEvent(object sender, NmeaCOGSOGEventArgs args)
        {
            DisplayVariables.SpeedOverGround.Value = args.speedKnots;
        }
    }
}