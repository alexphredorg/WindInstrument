using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    class VerticalLineGraphDisplayEntity : VerticalLineGraphDisplayEntityBase
    {
        public VerticalLineGraphDisplayEntity(int x, int y, int width, int height, double min, double max, DisplayVariable displayVariable, bool fFill, double tickPeriod)
            : base(x, y, width, height, displayVariable, fFill)
        {
            this.scaleFactor = this.Width / (max - min);
            this.min = min;
            this.max = max;
            this.tickPeriod = tickPeriod;
        }

        protected override double TickMax
        {
            get { return min; }
        }

        protected override double TickMin
        {
            get { return max; }
        }

        protected override double TickPeriod
        {
            get { return tickPeriod; }
        }

        /// <summary>
        /// Scale an input point (v) to fit inside of the graph area
        /// </summary>
        /// <param name="v">input value</param>
        /// <returns>The X value for the graph point</returns>
        protected override int ScalePoint(double v)
        {
            if (v <= min)
            {
                return this.X;
            }
            else if (v >= max)
            {
                return this.X + this.Width;
            }
            else
            {
                return (int)(this.X + ((v - min) * this.scaleFactor));
            }
        }

        private double min;
        private double max;
        private double scaleFactor;
        private double tickPeriod;
    }
}
