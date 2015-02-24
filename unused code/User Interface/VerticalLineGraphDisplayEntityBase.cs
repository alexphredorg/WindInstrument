using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    abstract class VerticalLineGraphDisplayEntityBase : DisplayEntity
    {
        /// <summary>
        /// Construct a new base for a VerticalLineGraphDisplayEntity
        /// </summary>
        /// <param name="x">left</param>
        /// <param name="y">top</param>
        /// <param name="width">width</param>
        /// <param name="height">height</param>
        /// <param name="displayVariable">the source display variable drawing on the graph</param>
        /// <param name="fFill">fill from the left to the middle?</param>
        /// <param name="tickPeriod"></param>
        public VerticalLineGraphDisplayEntityBase(int x, int y, int width, int height, DisplayVariable displayVariable, bool fFill)
            : base(x, y, width, height)
        {
            this.displayVariable = displayVariable;
            this.displayVariable.DisplayVariableUpdateEvent += this.Update;
            this.maxYOffset = height - VerticalLineGraphDisplayEntity.HashHeight - 1;
            this.dataPoints = new int[maxYOffset];
            this.dataPointsOriginal = new double[maxYOffset];
            this.fFill = fFill;
            for (int i = 0; i < this.dataPoints.Length; i++)
            {
                this.dataPoints[i] = -1;
                this.dataPointsOriginal[i] = 0;
            }
        }


        /// <summary>
        /// This is called whenever the display variable is updated
        /// </summary>
        public virtual void Update()
        {
            lock (this)
            {
                double datapoint;
                if (displayVariable.Value is Int32)
                {
                    datapoint = (double)((int)displayVariable.Value * 1.0);
                }
                else
                {
                    datapoint = (double)displayVariable.Value;
                }

                dataPointsOriginal[dataPointsIndex] = datapoint;
                dataPoints[dataPointsIndex] = this.ScalePoint(datapoint);

                int thisPoint = dataPointsIndex;
                dataPointsIndex = ((dataPointsIndex + 1) % dataPoints.Length);

                if (this.Page.IsVisible)
                {
                    if (yOffset == maxYOffset)
                    {
                        this.ScrollUp();
                        this.yOffset--;
                    }
                    this.DrawPoint(yOffset++, thisPoint);
                }
            }
        }

        /// <summary>
        /// Redraw the DisplayEntity
        /// </summary>
        public override void Refresh()
        {
            lock (this)
            {
                int y = 0;

                // clear the graph area
                for (y = this.Y; y < this.Y + this.Height; y++)
                {
                    this.Page.DrawHorizontalLine(this.X, this.X + this.Width, y, 0);
                }

                // draw points
                this.yOffset = 0;
                for (int i = dataPointsIndex; i < dataPoints.Length; i++)
                {
                    this.DrawPoint(this.yOffset++, i);
                }
                for (int i = 0; i < dataPointsIndex - 1; i++)
                {
                    this.DrawPoint(this.yOffset++, i);
                }

                // draw hashes
                if (this.TickPeriod != 0)
                {
                    for (double tick = this.TickMin; tick < this.TickMax; tick += this.TickPeriod)
                    {
                        int x = this.ScalePoint(tick);
                        this.Page.DrawVerticalLine(
                            x,
                            this.Y + this.Height - VerticalLineGraphDisplayEntity.HashHeight,
                            this.Y + this.Height - 1,
                            VerticalLineGraphDisplayEntity.PointColor);
                    }
                }
            }
        }

        /// <summary>
        /// Scale an input point (v) to fit inside of the graph area
        /// </summary>
        /// <param name="v">input value</param>
        /// <returns>The X value for the graph point</returns>
        protected abstract int ScalePoint(double v);

        /// <summary>
        /// Return the first tick mark location.
        /// </summary>
        protected abstract double TickMin { get; }

        /// <summary>
        /// What is the period between tick marks?
        /// </summary>
        protected abstract double TickPeriod { get; }

        /// <summary>
        /// Return the last tick mark location
        /// </summary>
        protected abstract double TickMax { get; }

        /// <summary>
        /// The raw data points that are drawn to the display.
        /// </summary>
        protected int[] DataPoints { get { return this.dataPoints; } }

        protected double[] DataPointsOriginal { get { return this.dataPointsOriginal; } }

        protected int DataPointsIndex { get { return this.dataPointsIndex; } }

        /// <summary>
        /// Scroll the graph region up one pixel
        /// </summary>
        protected virtual void ScrollUp()
        {
            this.Page.ScrollRegionUp(
                this.X,
                this.X + this.Width,
                this.Y,
                this.Y + maxYOffset + 1);
        }

        /// <summary>
        /// Draw a point on the graph
        /// </summary>
        /// <param name="x">The y offset of where to draw</param>
        /// <param name="i">The index into the dataPoints array to draw from</param>
        private void DrawPoint(int y, int i)
        {
            if (dataPoints[i] <= 0) return;
            if (this.fFill)
            {
                this.Page.DrawHorizontalLine(this.X, dataPoints[i], y, PointColor);
            }
            else
            {
                this.Page.SetPixel(dataPoints[i], y, PointColor);
            }
        }

        private DisplayVariable displayVariable;
        private int[] dataPoints;
        private double[] dataPointsOriginal;
        private int dataPointsIndex;
        private int yOffset = 0;
        private int maxYOffset;
        private bool fFill;
        private const byte PointColor = 0xf;
        private const int HashHeight = 5;
    }
}
