/*
using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    class AutoScaleVerticalLineGraphDisplayEntity : VerticalLineGraphDisplayEntityBase
    {
        public AutoScaleVerticalLineGraphDisplayEntity(int x, int y, int width, int height, double min, double max, DisplayVariable displayVariable, bool fFill, double tickPeriod)
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
        /// <param name="fReportFault">If true then report faults</param>
        /// <returns>The X value for the graph point</returns>
        protected override int ScalePoint(double v, bool fReportFault)
        {
            if (v <= min)
            {
                if (fReportFault) { this.AutoScale(); }
                return this.X;
            }
            else if (v >= max)
            {
                if (fReportFault) { this.AutoScale(); }
                return this.X + this.Width;
            }
            else
            {
                return (int)(this.X + ((v - min) * this.scaleFactor));
            }
        }

        /// <summary>
        /// This is called when a point is loaded that is outside of our normal scale.  It is
        /// used by the AutoScale version of this class
        /// </summary>
        void AutoScale()
        {
            lock (this)
            {
                double newMin = double.MaxValue;
                double newMax = double.MinValue;

                for (int i = 0; i < this.DataPoints.Length; i++)
                {
                    if (this.DataPoints[i] != -1 && this.DataPointsOriginal[i] < newMin)
                    {
                        newMin = this.DataPointsOriginal[i];
                    }
                    if (this.DataPoints[i] != -1 && this.DataPointsOriginal[i] > newMax)
                    {
                        newMax = this.DataPointsOriginal[i];
                    }
                }

                this.min = newMin;
                this.max = newMax;
                this.scaleFactor = this.Width / (max - min);

                for (int i = 0; i < this.DataPoints.Length; i++)
                {
                    this.DataPoints[i] = this.ScalePoint(this.DataPointsOriginal[i], false);
                }

                this.Refresh();
            }
        }

        private double min;
        private double max;
        private double scaleFactor;
        private double tickPeriod;
    }
}
*/