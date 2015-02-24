using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    class LineGraphCompassDisplayEntity : DisplayEntity
    {
        public LineGraphCompassDisplayEntity(int x, int y, int width, int height, DisplayVariable displayVariable)
            : base(x, y, width, height)
        {
            this.displayVariable = displayVariable;
            this.displayVariable.DisplayVariableUpdateEvent += this.Update;
            this.midpoint = this.X + (this.Width / 2);
            this.pos30 = midpoint + (this.Width / 6);
            this.neg30 = midpoint - (this.Width / 6);
            this.pos60 = midpoint + (this.Width / 3);
            this.neg60 = midpoint - (this.Width / 3);
            this.scaling = this.Width / 180;
        }

        public virtual void Update()
        {
            this.heading = (int) displayVariable.Value;
            // make heading -180 to 180
            if (this.heading > 180) this.heading -= 360;
            if (this.Page.IsVisible)
            {
                Refresh();
            }
        }

        public override void Refresh()
        {
            this.Page.DrawVerticalLine(lastHeadingMark - 1, this.Y, this.Height, 0x0);
            this.Page.DrawVerticalLine(lastHeadingMark, this.Y, this.Height, 0x0);
            this.Page.DrawVerticalLine(lastHeadingMark + 1, this.Y, this.Height, 0x0);
            this.Page.DrawVerticalLine(midpoint - 1, this.Y, this.Height, BigHashColor);
            this.Page.DrawVerticalLine(midpoint + 1, this.Y, this.Height, BigHashColor);
            this.Page.DrawVerticalLine(pos30, this.Y + 2, this.Height - 4, SmallHashColor);
            this.Page.DrawVerticalLine(neg30, this.Y + 2, this.Height - 4, SmallHashColor);
            this.Page.DrawVerticalLine(pos60, this.Y + 2, this.Height - 4, SmallHashColor);
            this.Page.DrawVerticalLine(neg60, this.Y + 2, this.Height - 4, SmallHashColor);
            if (heading <= -90)
            {
                heading = -180 - heading;
            }
            else if (heading >= 90)
            {
                heading = 180 - heading;
            }

            int headingMark = (int)(this.X + midpoint + (heading * scaling));
            this.Page.DrawVerticalLine(headingMark - 1, this.Y, this.Height, PointerColor);
            this.Page.DrawVerticalLine(headingMark, this.Y, this.Height, PointerColor);
            this.Page.DrawVerticalLine(headingMark + 1, this.Y, this.Height, PointerColor);
            lastHeadingMark = headingMark;
        }

        private DisplayVariable displayVariable;
        private int heading;
        private int lastHeadingMark;
        private int midpoint;
        private int neg30;
        private int pos30;
        private int neg60;
        private int pos60;
        const byte SmallHashColor = 0x8;
        const byte BigHashColor = 0xf;
        const byte PointerColor = 0xf;
        private double scaling;
    }
}
