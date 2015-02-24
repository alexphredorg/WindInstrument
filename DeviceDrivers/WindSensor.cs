using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace WindInstrumentToNMEA
{
    /// <summary>
    /// This is a sensor driver for the Davis 6410 anemometer.  It was implemented using the helpful documentation
    /// at http://www.lexingtonwx.com/anemometer/ and http://www.emesystems.com/OL2wind.htm.
    /// 
    /// The hardware has the following pinout on an RJ14 plug:
    /// pin 2: yellow: direction pulse
    /// pin 3: green: wind direction 
    /// pin 4: red: ground (connect to ground on the Netduino)
    /// pin 5: black: wind speed pulse
    /// 
    /// Since we don't use a lookup table to convert pulses to mph the mph values may not be accurate above 40mph.
    /// 
    /// The anemometer should be mounted facing forward at the masthead to avoid be influenced by wind spilling from the
    /// sails.  Use a correction of 180 if you have it facing aft.
    /// </summary>
    class WindSensor
    {
        public delegate void WindSensorCallback(double relativeHeading, double knots);

        /// <summary>
        /// Create a new sensor driver instance.
        /// </summary>
        /// <param name="windSpeedPin">The Netduino pin that is connected to sensor pin 5</param>
        /// <param name="windDirectionPulse">The Netduino pin that is connected to sensor pin 2</param>
        /// <param name="windDirectionSensor">The Netduino pin that is connected to sensor pin 3</param>
        /// <param name="logger">The logger that should receive the NMEA output</param>
        public WindSensor(
            Cpu.Pin windSpeedPin, 
            Cpu.Pin windDirectionPulse,
            Cpu.AnalogChannel windDirectionSensorPin,
            WindSensorCallback windSensorCallback)
        {
            windDirectionSensor = new AnalogInput(windDirectionSensorPin);
            pulsePort = new OutputPort(windDirectionPulse, false);
            pulsePort.Write(true);
            m_callback = windSensorCallback;

            speedPort = new InterruptPort(windSpeedPin, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeHigh);
            speedPort.OnInterrupt += new NativeEventHandler(speedPort_OnInterrupt);
            timer = new Timer(new TimerCallback(RateCallback), null, windDirectionInterval, windDirectionInterval);
        }

        /// <summary>
        /// This is called ten times per second.  Every time it is called we sample the wind direction.
        /// On every 10th call we average those samples and report them.  
        /// 
        /// This also collects the wind speed samples and computes the speed every time that the wind 
        /// direction is reported.
        /// </summary>
        /// <param name="ignored"></param>
        protected void RateCallback(Object ignored)
        {
            // pulse the wind direction port and read the analog channel with it's value.
            double direction = windDirectionSensor.Read();
            recentDirection[recentDirectionIndex] = direction;
            recentDirectionIndex = (recentDirectionIndex + 1) % windDirectionSamples;

            // RateCallback is called 10 times per second.  windDirectionSamples is set to 10.  
            // So once per second report our average wind direction and wind speed.
            if (recentDirectionIndex == 0)
            {
                // average recent direction samples.
                direction = 0;
                for (int i = 0; i < windDirectionSamples; i++)
                {
                    direction += recentDirection[i];
                }
                direction /= windDirectionSamples;

                // direction is a value from 0 to 1.  0.5 points to the back of the boat, 0 and 1 both point to the front of the boat.  
                // Convert that to degrees * 10.
                int degrees = (int)(direction * 3600);
                degrees = (degrees + degreesCorrection) % 3600;

                // get the oldest and newest pulse times from the wind speed indicator.
                long newestPulseTime = 0;
                long oldestPulseTime = long.MaxValue;
                lock (this)
                {
                    int newestTimeIndex = (this.lastPulseTimesIndex == 0) ? (windSpeedSamples - 1) : (this.lastPulseTimesIndex - 1);
                    oldestPulseTime = this.lastPulseTimes[this.lastPulseTimesIndex];
                    newestPulseTime = this.lastPulseTimes[newestTimeIndex];
                }

                double knots = 0;
                if (oldestPulseTime != 0)
                {
                    // we average the wind speed by figuring out the average pulse period over the
                    // last (windSpeedSamples) pulses.  The period is then converted to hz, and finally
                    // that is converted into knots.
                    long timeDelta = newestPulseTime - oldestPulseTime;
                    double period = ((double)timeDelta / (windSpeedSamples - 1)) / TimeSpan.TicksPerSecond;
                    double hz = 1 / period;
                    knots = hz * hz2knots;
                }

                // report the wind speed back to our program
                m_callback(((double) degrees) / 10, knots);
            }
        }

        /// <summary>
        /// This is called every time a pulse is received on the wind speed pin
        /// </summary>
        /// <param name="data1">ignored</param>
        /// <param name="data2">ignored</param>
        /// <param name="time">what time did the event occur</param>
        private void speedPort_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            lock (this)
            {
                this.lastPulseTimes[this.lastPulseTimesIndex] = time.Ticks;
                this.lastPulseTimesIndex = (this.lastPulseTimesIndex + 1) % windSpeedSamples;
            }
        }

        /// <summary>
        /// DegreesCorrection is used to override the correction value.
        /// </summary>
        public int DegreesCorrection
        {
            get { return this.degreesCorrection; }
            set { this.degreesCorrection = value; }
        }

        // this is how often the wind direction callback is called
        static readonly TimeSpan windDirectionInterval = new TimeSpan(0, 0, 0, 0, 100);
        // the number of wind direction samples to average per reported reading
        // windDirectionSamples * windDirectionInterval is how often the callback function
        // will be called with data.
        static readonly int windDirectionSamples = 10;
        // The number of wind speed samples to average per reading.  Note that the clock time
        // for this will vary depending on how fast or slow the wind speed cups are turning.
        static readonly int windSpeedSamples = 10;
        // Constant to convert wind speed pulses to knots
        static readonly double hz2knots = 2.25 * 0.868976;
        // must be positive, so use 357 to mean -3
        int degreesCorrection = 0;
        
        // mapped to an analog pin to read the wind direction
        AnalogInput windDirectionSensor;
        // this needs to be true at the time that the wind direction is read
        OutputPort pulsePort;
        // We call this when there is new wind data to report.
        WindSensorCallback m_callback;
        // speedPort is pulsed whenever the wind speed cups rotate past their sensor
        private InterruptPort speedPort;
        // this Timer is used for wind direction sampling
        Timer timer;

        // recentDirection is a set of samples for recent direction data
        double[] recentDirection = new double[windDirectionSamples];
        int recentDirectionIndex = 0;

        // lastPulseTimes is the last set of times (measured in ticks) that the wind 
        // speed sensor was pulsed
        long[] lastPulseTimes = new long[windSpeedSamples];
        int lastPulseTimesIndex = 0;
    }
}
