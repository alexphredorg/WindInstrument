using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    public class DisplayVariables
    {
        static public DisplayVariable WindSpeed = new DisplayVariable("Wind Speed", typeof(double));
        static public DisplayVariable WindDirection = new DisplayVariable("Wind Direction", typeof(int));
        static public DisplayVariable SpeedOverGround = new DisplayVariable("Speed over Ground", typeof(double));
        static public DisplayVariable Depth = new DisplayVariable("Depth", typeof(double));
    }
}
