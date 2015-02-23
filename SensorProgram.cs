#undef CONFIG
#if CONFIG
// CONFIG is untested code that will allow you to change the wind direction correction
// factor from the display.  It would require a memory card, and the Netduino 2 that I'm
// using doesn't have one.
#endif


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

// Netduino wiring for the Sensor Computer.  This uses a non-Plus Netduino:
//  D0 - RX1 (unused, used to be wired to e7d) NMEA from GPS
//  D1 - TX1 (wired to e7d) NMEA to GPS
//  D2 - RX2 (unused)
//  D3 - TX2 (unused, used to be wired to display head) NMEA to display head
//  D4 -
//  D5 - Button input
//  D6 - Wind Sensor Speed 
//  D7 - Wind Sensor Direction Pulse
//  D8 - Display SA0
//  D9 - Display Rst
// D10 - Display CS
// D11 - Display Data
// D12 -
// D13 - Display Clk
//  A0 - Wind Sensor Direction
//  A1 -
//  A2 -
//  A3 -
//  A4 - 
//  A5 - 

namespace WindInstrumentToNMEA
{
    public class SensorProgram
    {
        public void Go()
        {
            // the oled is used for debugging.  This could be removed if more pins were necessary for
            // sensors or to save on material costs
            this.oled = new Adafruit1306OledDriver(
                dc: Pins.GPIO_PIN_D8,
                reset: Pins.GPIO_PIN_D9,
                chipSelect: Pins.GPIO_PIN_D10,
                spiModule:SPI.SPI_module.SPI1,
                speedKHz:10000);

#if CONFIG
            m_inputButton = new AutoRepeatInputPort(Pins.GPIO_PIN_D5, Port.ResistorMode.PullUp, true);
            m_inputButton.StateChanged += new AutoRepeatEventHandler(m_inputButton_StateChanged);
#endif

            this.oled.Initialize();
            this.oled.ClearScreen();
            this.oled.DrawString(0, 0, "sensorProgram");
            
            // initialize the Wind Sensor
            m_windSensor = new WindSensor(
                Pins.GPIO_PIN_D6,
                Pins.GPIO_PIN_D7,
                AnalogChannels.ANALOG_PIN_A0,
                new WindSensor.WindSensorCallback(WindSensorCallback));

#if CONFIG
            // read configuration
            m_windSensor.DegreesCorrection = ConfigFile.DegreesCorrection;
#endif

            // initalize the serial ports
            this.serialPort = new SerialPort("COM1", 38400, Parity.None, 8, StopBits.One);
            this.serialPort.Open();
            this.nmeaOutputPort = new NmeaOutputPort(this.serialPort);

            this.oled.ClearScreen();
            while (true)
            {
                Thread.Sleep(OledDisplayTime);
                //this.oled.DisplayOff();
            }
        }

#if CONFIG
        void m_inputButton_StateChanged(object sender, AutoRepeatEventArgs e)
        {
            switch (e.State)
            {
                case AutoRepeatInputPort.AutoRepeatState.Press:
                    m_fLongPress = false;
                    if (!m_fEditMode)
                    {
                        this.oled.DrawString(0, 5, "hold to adjust");
                    }
                    break;
                case AutoRepeatInputPort.AutoRepeatState.Tick:
                    m_fLongPress = true;
                    if (!m_fEditMode)
                    {
                        m_fEditMode = true;
                        this.UpdateOLED();
                    }
                    break;
                case AutoRepeatInputPort.AutoRepeatState.Release:
                    if (m_fEditMode)
                    {
                        if (m_fLongPress)
                        {
                            this.shouldIncrement = !this.shouldIncrement;
                        }
                        else
                        {
                            int correction = (this.shouldIncrement) ? +1 : +359;
                            m_windSensor.DegreesCorrection = (m_windSensor.DegreesCorrection + correction) % 360;
                            ConfigFile.DegreesCorrection = (ushort) m_windSensor.DegreesCorrection;
                        }
                        this.UpdateOLED();
                    }
                    else
                    {
                        this.oled.ClearScreen();
                        this.UpdateOLED();
                    }
                    break;
            }
        }
#endif

        /// <summary>
        /// Wind Sensor Callback is called when there is new data from the wind sensor.  
        /// </summary>
        /// <param name="relativeHeading"></param>
        /// <param name="mph"></param>
        public void WindSensorCallback(double relativeHeading, double knots)
        {
            // send it to the display head and the GPS'
            if (this.nmeaOutputPort != null)
            {
                this.nmeaOutputPort.WriteWindHeadingAndVelocity(relativeHeading, knots);
            }
            // update the OLED
            m_lastRelativeHeading = relativeHeading;
            m_lastKnots = knots;
            this.UpdateOLED();
        }

        private void UpdateOLED()
        {
            this.oled.DrawString(0, 0, Program.Version);
            this.oled.DrawString(0, 1, " speed: " + m_lastKnots.ToString("N1") + "  ");
            this.oled.DrawString(0, 2, " heading: " + m_lastRelativeHeading.ToString("N1") + "  ");
            this.oled.DrawString(0, 3, " offset: " + m_windSensor.DegreesCorrection.ToString() + "  ");
#if CONFIG
            if (m_fEditMode)
            {
                this.oled.DrawString(0, 5, " ** Adj Mode **");
                this.oled.DrawString(0, 6, (this.shouldIncrement) ? " incr" : " decr");
            }
#endif
        }

        private Adafruit1306OledDriver oled;
        private SerialPort serialPort;
        private NmeaOutputPort nmeaOutputPort;

        private WindSensor m_windSensor;
        private const int OledDisplayTime = 30 * 60;

        private double m_lastRelativeHeading;
        private double m_lastKnots;

#if CONFIG
        private AutoRepeatInputPort m_inputButton;
        private bool m_fLongPress = false;
        private bool m_fEditMode = false;
        private bool shouldIncrement = true;
#endif
    }
}