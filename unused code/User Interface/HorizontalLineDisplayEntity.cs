using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    class HorizontalLineDisplayEntity : DisplayEntity
    {
        public HorizontalLineDisplayEntity(int x, int y, int width, int height, byte color)
            : base(x, y, width, height)
        {
            if (height != 1)
            {
                throw new ArgumentException("height must be 1");
            }
            this.color = color;
        }

        public override void Refresh()
        {
            this.Page.DrawHorizontalLine(this.X, this.X + this.Width, this.Y, this.color);
        }

        byte color;
    }
}
