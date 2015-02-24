using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    class VerticalLineDisplayEntity : DisplayEntity
    {
        public VerticalLineDisplayEntity(int x, int y, int width, int height, byte color)
            : base(x, y, width, height)
        {
            if (width != 1)
            {
                throw new ArgumentException("width must be 1");
            }
            this.color = color;
        }

        public override void Refresh()
        {
            this.Page.DrawVerticalLine(this.X, this.Y, this.Y + this.Height, this.color);
        }

        byte color;
    }
}
