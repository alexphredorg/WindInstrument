This folder contains tested classes that are related to NMEA 0183 or hardware used on sailboats.
It is not necessary for the basic Wind Instrument, but others might find it useful for their own
projects.  I had used it to build a custom NMEA 0183 display.  It worked pretty well, but requires
a Netduino Plus 2 (the wind sensor is fine with a basic Netduino) and would crash (due to low memory)
after a few hours.  I abandoned the project because the display was too hard to read in daylight,
but there is some useful code buried in here.

DeviceDrivers\CompassSensor - A driver that reads heading and heel from a LSM303DLHC I2C compass.
DeviceDrivers\FontInfo & OledFont - Bitmap format for OLED font
DeviceDrivers\NewhavenOLED - Driver to draw text and graphics on a 256x64 pixel Newhaven 4-bit display
DeviceDrivers\NmeaInputPort - Read NMEA 0183 verbs from a NMEA 0183 network
DeviceDrivers\NmeaRepeater - Repeat NMEA 0183 verbs on a NMEA 0183 network.  Useful for building a multiplexor

User Interface\* - This is code for a generic NMEA 0183 display using the above device drivers
	DisplayVariable - This is tied to each type of variable (wind direction, boat speed, etc) that can be displayed
	DisplayEntity - A graphical representation of a display variable
	Page - A layout of DisplayEntities
	SimpleUserInterface - Defines pages and the code for switching between them
CockpitProgram.cs - The main code for the NMEA 0183 display

The .font files are bitmap representations of fonts that are used on a graphical display.  The 
FontInfo.cs class explains the format.  The format is tailored around fast writing to a 
Newhaven 4-bit per pixel OLED and might not be as useful on other displays.