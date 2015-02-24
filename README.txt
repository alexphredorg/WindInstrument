This is a program for the Netduino (any version) that interfaces it to 
a Davis 6410 anenometer and outputs data using the MWV verb to a NMEA 
0183 network.  I use it for a masthead wind instrument on my sailboat.  It
is interfaced with Raymarine equipment through a Raymarine e7d chart 
plotter.

The basic pinout (no DISPLAY, no CONFIG) for the Netduino is as follows:
* D1  - TX1 NMEA to GPS
* D6  - Wind Sensor Speed (black)
* D7  - Wind Sensor Direction Pulse (yellow on Davis)
* A0  - Wind Sensor Direction (green)
* GND - power supply group and Wind Sensor Ground (red)
* VIN  - power supply input

D1 is TTL serial.  NMEA 0183 is RS422, so you need a TTL to RS422 transceiver 
to be wired up to this port.  If you are sharing the data with a PC you could 
use a TTL to RS232 transceiver instead.  I used a DS8921N as my
transciever.  The data sheet is here:
http://www.ti.com/lit/ds/symlink/ds8921.pdf

Wire the transceiver up as follows:
* pin 1 VCC        - Netduino 5V
* pin 3 DI         - To Netduino pin D1 (TX1)
* pin 4 GND        - Netduino ground
* pin 5 NMEA 0183- - to NMEA 0183 network
* pin 6 NMEA 0183+ - to NMEA 0183 network
* You can leave pins 7, 8, 2, disconnected.

There are additional useful comments at the top of SensorProgram.cs and 
DeviceDrivers/WindSensor.cs to get more wiring details.

If you have an Adafruit OLED model SSD1306 you can use the DISPLAY define 
to write debug output to the display.  This works well, but is completely 
optional because debug information is also written to the
debug output console in Visual Studio.

If you have a Netduino Plus you can use the untested code in the CONFIG 
define to get the ability to adjust the heading one degree at a time.  
I don't have a Netduino Plus and haven't used it yet.  This will only 
be useful if you also have a DISPLAY.  You can otherwise adjust the wind 
offset by changing the constant in DeviceDrivers/WindSensor.cs

