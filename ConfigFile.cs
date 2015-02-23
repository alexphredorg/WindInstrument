#undef CONFIG
#if CONFIG
// CONFIG is untested code that will allow you to change the wind direction correction
// factor from the display.  It would require a memory card, and the Netduino 2 that I'm
// using doesn't have one.


using System;
using System.IO;
using System.Reflection;
using Microsoft.SPOT;

namespace WindInstrumentToNMEA
{
    class ConfigFile
    {
        public static ushort DegreesCorrection
        {
            get
            {
                try
                {
                    ConfigFileBuffer configFile = ConfigFile.ReadConfig();
                    return configFile.DegreesCorrection;
                }
                catch (IOException)
                {
                    return 0;
                }
            }
            set
            {
                try
                {
                    ConfigFile.WriteConfig(value);
                }
                catch (IOException)
                {
                }
            }
        }

        private static ConfigFileBuffer ReadConfig()
        {
            byte[] buffer = new byte[ConfigFile.ConfigLength];
            FileStream fs = new FileStream(ConfigFile.ConfigPath, FileMode.OpenOrCreate);
            int lengthRead = fs.Read(buffer, 0, buffer.Length);
            
            ConfigFileBuffer configFile = new ConfigFileBuffer();
            
            if (lengthRead == ConfigLength)
            {
                configFile.ConfigVersion = (ushort) Microsoft.SPOT.Hardware.Utility.ExtractValueFromArray(buffer, 0, 2);
                configFile.DegreesCorrection = (ushort) Microsoft.SPOT.Hardware.Utility.ExtractValueFromArray(buffer, 2, 2);
            }

            if (lengthRead != ConfigLength || configFile.ConfigVersion != 1)
            {
                configFile = new ConfigFileBuffer();
                configFile.ConfigVersion = 1;
                configFile.DegreesCorrection = 0;
            }

            return configFile;
        }

        private static void WriteConfig(ushort degreesCorrection)
        {
            byte[] buffer = new byte[ConfigFile.ConfigLength];
            FileStream fs = new FileStream(ConfigFile.ConfigPath, FileMode.OpenOrCreate);

            ConfigFileBuffer configFile = new ConfigFileBuffer();
            configFile.ConfigVersion = 1;
            configFile.DegreesCorrection = degreesCorrection;

            Microsoft.SPOT.Hardware.Utility.InsertValueIntoArray(buffer, 0, 2, configFile.ConfigVersion);
            Microsoft.SPOT.Hardware.Utility.InsertValueIntoArray(buffer, 2, 2, configFile.DegreesCorrection);

            fs.Write(buffer, 0, ConfigLength);
        }

        private struct ConfigFileBuffer
        {
            public ushort ConfigVersion;
            public ushort DegreesCorrection;
        }

        private const string ConfigPath = "\\SD\\config.bin";
        private const int ConfigLength = ConfigVersionLength + ConfigDegreesCorrectionLength;
        private const int ConfigVersionLength = 2;
        private const int ConfigDegreesCorrectionLength = 2;
    }
}

#endif
