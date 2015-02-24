using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    class CompassGraphDisplayEntity : VerticalLineGraphDisplayEntityBase
    {
        public CompassGraphDisplayEntity(int x, int y, int width, int height, DisplayVariable displayVariable)
            : base(x, y, width, height, displayVariable, false)
        {
            this.scaleFactor = this.Width / CompassGraphDisplayEntity.WidthInDegrees;
            this.centerpoint = 0;
            this.tickMin = -(WidthInDegrees / 2);
            this.tickMax = (WidthInDegrees / 2);
            this.tickPeriod = WidthInDegrees / 2;
            this.middlePixelX = this.X + (this.Width / 2);
        }

        protected override double TickMax
        {
            get { return tickMin; }
        }

        protected override double TickMin
        {
            get { return tickMax; }
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
            this.centerpoint = v;

            // shift all points already in array based around this one
            for (int i = 0; i < this.DataPointsOriginal.Length; i++)
            {
                double o;
                if (v < 180 && this.DataPointsOriginal[i] > 180)
                {
                    o = DataPointsOriginal[i] + 360;
                }
                else if (v > 180 && this.DataPointsOriginal[i] < 180)
                {
                    o = DataPointsOriginal[i] - 360;
                }
                else
                {
                    o = DataPointsOriginal[i];
                }

                if (System.Math.Abs(v - o) > 30)
                {
                    this.DataPoints[i] = -1;
                }
                else
                {
                    // o is 40, v is 42.  That means the wind to starboard and the o pixel should go left
                    // 40 - 42 = -2, so the it would go left
                    this.DataPoints[i] = (int) (middlePixelX + ((o - v) * this.scaleFactor));
                }
            }

            return middlePixelX;
        }

        /// <summary>
        /// Scroll the graph region up one pixel
        /// </summary>
        protected override void ScrollUp()
        {
            this.Refresh();
        }

        private double tickMin;
        private double tickMax;
        private double scaleFactor;
        private double tickPeriod;
        private double centerpoint;
        private int middlePixelX;
        private const int WidthInDegrees = 60;
        
    }
}
