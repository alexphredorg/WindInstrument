// 
// SailboatComputer - CompassSensor.cs
// 
// Created 01 - 2013
// 
// Alex Wetmore

using System;
using System.IO;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using trig = System.Math;

namespace SailboatComputer
{
    /// <summary>
    ///     This is an I2C sensor to read heading using the LSM303DLHC chip.
    ///     I got this working by looking at the the application notes found here:
    ///     http://www.pololu.com/file/download/LSM303DLH-compass-app-note.pdf?file_id=0J434
    ///     and the sample Arduino code from these sources:
    ///     https://github.com/pololu/LSM303 (most useful)
    ///     https://github.com/adafruit/Adafruit_LSM303 (copied constants)
    ///     Basic usage:
    ///     I2CDevice m_i2c = new I2CDevice(null);
    ///     CompassSensor sensor = new CompassSensor(m_i2c);
    ///     double roll;
    ///     double pitch;
    ///     double heading;
    ///     heading = sensor.ReadAngleAndHeading(out roll, out pitch);
    ///     Use CompassSensor.Calibrate() to get calibration parameters and save
    ///     them into the m_magMin and m_magMax vectors in this class.
    /// </summary>
    public class CompassSensor
    {
        private const int TransactionTimeout = 500; // ms
        private const int ClockRateKHz = 100;
        private const byte AccelAddress = 0x19;
        private const byte MagAddress = 0x1e;
        private readonly I2CDevice.Configuration m_accelConfig;
        private readonly I2CDevice m_i2c;
        private readonly I2CDevice.Configuration m_magConfig;
        private readonly Int16[] m_recentHeadings = new Int16[5];
        private int m_recentHeadingIndex;
        private vector m_magMax = new vector(629, 594, 719);
        private vector m_magMin = new vector(-666, -713, -565);

        /// <summary>
        ///     Initialize the CompassSensor
        /// </summary>
        /// <param name="i2c">single I2CDevice being used on the Netduino</param>
        public CompassSensor(I2CDevice i2c)
        {
            this.m_accelConfig = new I2CDevice.Configuration(AccelAddress, ClockRateKHz);
            this.m_magConfig = new I2CDevice.Configuration(MagAddress, ClockRateKHz);
            this.m_i2c = i2c;

            WriteRegister(this.m_accelConfig, (byte) AccelRegisters.LSM303_REGISTER_ACCEL_CTRL_REG1_A, 0x27);
            WriteRegister(this.m_accelConfig, (byte) AccelRegisters.LSM303_REGISTER_ACCEL_CTRL_REG4_A, 0x40 | 0x80 | 0x08);
            WriteRegister(this.m_magConfig, (byte) MagRegisters.LSM303_REGISTER_MAG_CRA_REG_M, 0x14);
            WriteRegister(this.m_magConfig, (byte) MagRegisters.LSM303_REGISTER_MAG_MR_REG_M, 0x00);
        }

        /// <summary>
        ///     Read the heading and angle data from the device.
        ///     These formulas come from:
        ///     http://www.pololu.com/file/download/LSM303DLH-compass-app-note.pdf?file_id=0J434
        /// </summary>
        /// <param name="roll">The device's roll measured in degrees</param>
        /// <param name="pitch">The device's pitch measured in degrees</param>
        /// <returns>
        ///     The compass heading rounded to the nearest integer.  Zero is
        ///     aligned with the X-> on the top of the Adafruit board.
        /// </returns>
        public Int16 ReadHeading(out double roll, out double pitch)
        {
            intvector accRaw = ReadRawAcc();

            // compute roll and pitch, results are in radians
            pitch = trig.Asin((double) accRaw.x / 1024);
            roll = trig.Asin(((double) accRaw.y / 1024) / System.Math.Cos(pitch));

            intvector magRaw = ReadRawMag();
            vector mag;

            // normalize mag output using calibration results.  
            // This corresponds to M1 in the application notes
            mag.x = ((magRaw.x - this.m_magMin.x) / (this.m_magMax.x - this.m_magMin.x) * 2 - 1.0);
            mag.y = ((magRaw.y - this.m_magMin.y) / (this.m_magMax.y - this.m_magMin.y) * 2 - 1.0);
            mag.z = ((magRaw.z - this.m_magMin.z) / (this.m_magMax.z - this.m_magMin.z) * 2 - 1.0);

            // tilt compenstation.  magtilt corresponds to M2 in the
            // application notes.  These are from equation 12 in the
            // app notes
            vector magtilt;
            double cospitch = trig.Cos(pitch);
            double sinpitch = trig.Sin(pitch);
            double cosroll = trig.Cos(roll);
            double sinroll = trig.Sin(roll);
            magtilt.x = mag.x * cospitch + mag.z * sinpitch;
            magtilt.y = mag.x * sinroll * sinpitch + mag.y * cosroll - mag.z * sinroll * cospitch;
            magtilt.z = -mag.x * cosroll * sinpitch + mag.y * sinroll + mag.z * cosroll * cospitch;

            // compute heading.  This is equation 13 in the app notes
            Int16 heading;
            double rawheading = trig.Atan(magtilt.y / magtilt.x) * 180 / trig.PI;
            if (magtilt.x == 0 && magtilt.y < 0)
            {
                heading = 90;
            }
            else if (magtilt.y == 0 && magtilt.y >= 0)
            {
                heading = 270;
            }
            else if (magtilt.x < 0)
            {
                heading = (Int16) (rawheading + 180);
            }
            else if (magtilt.x > 0 && magtilt.y < 0)
            {
                heading = (Int16) (rawheading + 360);
            }
            else
            {
                heading = (Int16) rawheading;
            }

            // convert pitch and roll to degrees
            pitch *= (180 / trig.PI);
            roll *= (180 / trig.PI);

            this.m_recentHeadings[this.m_recentHeadingIndex] = heading;
            this.m_recentHeadingIndex = (this.m_recentHeadingIndex + 1) % this.m_recentHeadings.Length;

            // double the most recent value as a weighting
            int dampenedHeading = heading;
            for (int i = 0; i < this.m_recentHeadings.Length; i++)
            {
                dampenedHeading += this.m_recentHeadings[i];
            }
            dampenedHeading /= (this.m_recentHeadings.Length + 1);

            //return (Int16) dampenedHeading;
            return heading;
        }

        /// <summary>
        ///     Calibration function.  Run this on your device and spin the device
        ///     in all possible orientations.  When done note the most recent
        ///     output in the Debug console.
        /// </summary>
        public void Calibrate()
        {
            intvector min;
            intvector max;
            intvector oldmin;
            intvector oldmax;
            intvector raw;

            min.x = Int16.MaxValue;
            min.y = Int16.MaxValue;
            min.z = Int16.MaxValue;
            max.x = Int16.MinValue;
            max.y = Int16.MinValue;
            max.z = Int16.MinValue;

            while (true)
            {
                oldmin = min;
                oldmax = max;

                raw = ReadRawMag();
                min.x = (Int16) System.Math.Min(min.x, raw.x);
                min.y = (Int16) System.Math.Min(min.y, raw.y);
                min.z = (Int16) System.Math.Min(min.z, raw.z);
                max.x = (Int16) System.Math.Max(max.x, raw.x);
                max.y = (Int16) System.Math.Max(max.y, raw.y);
                max.z = (Int16) System.Math.Max(max.z, raw.z);

                if (!(Object.Equals(min, oldmin)) || !(Object.Equals(max, oldmax)))
                {
                    Debug.Print("new values");
                    Debug.Print("x: min=" + min.x + "  max=" + max.x);
                    Debug.Print("y: min=" + min.y + "  max=" + max.y);
                    Debug.Print("z: min=" + min.z + "  max=" + max.z);
                    Debug.Print("");
                }
            }
        }

        /// <summary>
        ///     Read raw values from the accelerometer.  We shift the values down
        ///     to 12 bits to reduce noise (the chip values aren't accurate to 16
        ///     bits).
        /// </summary>
        /// <returns>An int vector with the raw values</returns>
        private intvector ReadRawAcc()
        {
            byte[] data = new byte[6];
            intvector acc;

            // assert the MSB of the address to get the accelerometer 
            // to do slave-transmit subaddress updating.
            ReadRegister(this.m_accelConfig, ((byte) AccelRegisters.LSM303_REGISTER_ACCEL_OUT_X_L_A) | 0x80, data);

            acc.x = (Int16) (((data[0] << 8) + data[1]));
            acc.y = (Int16) (((data[2] << 8) + data[3]));
            acc.z = (Int16) (((data[4] << 8) + data[5]));

            acc.x = (Int16) (acc.x >> 4);
            acc.y = (Int16) (acc.y >> 4);
            acc.z = (Int16) (acc.z >> 4);

            return acc;
        }

        /// <summary>
        ///     Read raw values from the mag sensor.
        /// </summary>
        /// <returns>An int vector with the raw values</returns>
        private intvector ReadRawMag()
        {
            byte[] data = new byte[6];
            intvector mag;

            ReadRegister(this.m_magConfig, ((byte) MagRegisters.LSM303_REGISTER_MAG_OUT_X_H_M), data);

            mag.x = (Int16) ((data[0] << 8) + data[1]);
            mag.z = (Int16) ((data[2] << 8) + data[3]);
            mag.y = (Int16) ((data[4] << 8) + data[5]);

            return mag;
        }

        /// <summary>
        ///     Read array of bytes at specific register from the I2C slave device.
        /// </summary>
        /// <param name="config">I2C slave device configuration.</param>
        /// <param name="register">The register to read bytes from.</param>
        /// <param name="readBuffer">The array of bytes that will contain the data read from the device.</param>
        /// <param name="transactionTimeout">The amount of time the system will wait before resuming execution of the transaction.</param>
        public void ReadRegister(I2CDevice.Configuration config, byte register, byte[] readBuffer)
        {
            byte[] writeBuffer = {register};

            // create an i2c write transaction to be sent to the device.
            I2CDevice.I2CTransaction[] transactions = new I2CDevice.I2CTransaction[]
            {
                I2CDevice.CreateWriteTransaction(writeBuffer),
                I2CDevice.CreateReadTransaction(readBuffer)
            };

            // the i2c data is sent here to the device.
            int transferred;
            lock (this.m_i2c)
            {
                this.m_i2c.Config = config;
                transferred = this.m_i2c.Execute(transactions, TransactionTimeout);
            }

            // make sure the data was sent.
            if (transferred != writeBuffer.Length + readBuffer.Length)
            {
                throw new IOException("Read Failed: " + transferred + "!=" + (writeBuffer.Length + readBuffer.Length));
            }
        }

        /// <summary>
        ///     Write a byte value to a specific register on the I2C slave device.
        /// </summary>
        /// <param name="config">I2C slave device configuration.</param>
        /// <param name="register">The register to send bytes to.</param>
        /// <param name="value">The byte that will be sent to the device.</param>
        /// <param name="transactionTimeout">The amount of time the system will wait before resuming execution of the transaction.</param>
        public void WriteRegister(I2CDevice.Configuration config, byte register, byte value)
        {
            byte[] writeBuffer = {register, value};

            // create an i2c write transaction to be sent to the device.
            I2CDevice.I2CTransaction[] transactions = new I2CDevice.I2CTransaction[] {I2CDevice.CreateWriteTransaction(writeBuffer)};

            // the i2c data is sent here to the device.
            int transferred;
            lock (this.m_i2c)
            {
                this.m_i2c.Config = config;
                transferred = this.m_i2c.Execute(transactions, TransactionTimeout);
            }

            // make sure the data was sent.
            if (transferred != writeBuffer.Length)
            {
                throw new IOException("Write Failed: " + transferred + "!=" + writeBuffer.Length);
            }
        }

        private enum AccelRegisters : byte
        {
            // DEFAULT    TYPE
            LSM303_REGISTER_ACCEL_CTRL_REG1_A = 0x20, // 00000111   rw
            LSM303_REGISTER_ACCEL_CTRL_REG2_A = 0x21, // 00000000   rw
            LSM303_REGISTER_ACCEL_CTRL_REG3_A = 0x22, // 00000000   rw
            LSM303_REGISTER_ACCEL_CTRL_REG4_A = 0x23, // 00000000   rw
            LSM303_REGISTER_ACCEL_CTRL_REG5_A = 0x24, // 00000000   rw
            LSM303_REGISTER_ACCEL_CTRL_REG6_A = 0x25, // 00000000   rw
            LSM303_REGISTER_ACCEL_REFERENCE_A = 0x26, // 00000000   r
            LSM303_REGISTER_ACCEL_STATUS_REG_A = 0x27, // 00000000   r
            LSM303_REGISTER_ACCEL_OUT_X_L_A = 0x28,
            LSM303_REGISTER_ACCEL_OUT_X_H_A = 0x29,
            LSM303_REGISTER_ACCEL_OUT_Y_L_A = 0x2A,
            LSM303_REGISTER_ACCEL_OUT_Y_H_A = 0x2B,
            LSM303_REGISTER_ACCEL_OUT_Z_L_A = 0x2C,
            LSM303_REGISTER_ACCEL_OUT_Z_H_A = 0x2D,
            LSM303_REGISTER_ACCEL_FIFO_CTRL_REG_A = 0x2E,
            LSM303_REGISTER_ACCEL_FIFO_SRC_REG_A = 0x2F,
            LSM303_REGISTER_ACCEL_INT1_CFG_A = 0x30,
            LSM303_REGISTER_ACCEL_INT1_SOURCE_A = 0x31,
            LSM303_REGISTER_ACCEL_INT1_THS_A = 0x32,
            LSM303_REGISTER_ACCEL_INT1_DURATION_A = 0x33,
            LSM303_REGISTER_ACCEL_INT2_CFG_A = 0x34,
            LSM303_REGISTER_ACCEL_INT2_SOURCE_A = 0x35,
            LSM303_REGISTER_ACCEL_INT2_THS_A = 0x36,
            LSM303_REGISTER_ACCEL_INT2_DURATION_A = 0x37,
            LSM303_REGISTER_ACCEL_CLICK_CFG_A = 0x38,
            LSM303_REGISTER_ACCEL_CLICK_SRC_A = 0x39,
            LSM303_REGISTER_ACCEL_CLICK_THS_A = 0x3A,
            LSM303_REGISTER_ACCEL_TIME_LIMIT_A = 0x3B,
            LSM303_REGISTER_ACCEL_TIME_LATENCY_A = 0x3C,
            LSM303_REGISTER_ACCEL_TIME_WINDOW_A = 0x3D
        };

        private enum MagRegisters : byte
        {
            LSM303_REGISTER_MAG_CRA_REG_M = 0x00,
            LSM303_REGISTER_MAG_CRB_REG_M = 0x01,
            LSM303_REGISTER_MAG_MR_REG_M = 0x02,
            LSM303_REGISTER_MAG_OUT_X_H_M = 0x03,
            LSM303_REGISTER_MAG_OUT_X_L_M = 0x04,
            LSM303_REGISTER_MAG_OUT_Z_H_M = 0x05,
            LSM303_REGISTER_MAG_OUT_Z_L_M = 0x06,
            LSM303_REGISTER_MAG_OUT_Y_H_M = 0x07,
            LSM303_REGISTER_MAG_OUT_Y_L_M = 0x08,
            LSM303_REGISTER_MAG_SR_REG_Mg = 0x09,
            LSM303_REGISTER_MAG_IRA_REG_M = 0x0A,
            LSM303_REGISTER_MAG_IRB_REG_M = 0x0B,
            LSM303_REGISTER_MAG_IRC_REG_M = 0x0C,
            LSM303_REGISTER_MAG_TEMP_OUT_H_M = 0x31,
            LSM303_REGISTER_MAG_TEMP_OUT_L_M = 0x32
        }

        public struct intvector
        {
            public Int16 x;
            public Int16 y;
            public Int16 z;
        };

        public struct vector
        {
            public double x;
            public double y;
            public double z;

            public vector(double x, double y, double z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        };
    }
}
