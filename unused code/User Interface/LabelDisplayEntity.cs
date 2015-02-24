using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    class LabelDisplayEntity : DisplayEntity
    {
        public LabelDisplayEntity(int x, int y, int width, int height, OledFont font, string label)
            : base(x, y, width, height)
        {
            this.font = font;
            this.value = label;
        }

        public override void Refresh()
        {
            this.Page.DrawString(font, this.X, this.Y, this.Width, this.Height, Newhaven25664OledDriver.Justify.Center, this.value);
        }

        protected OledFont font;
        protected string value = String.Empty;
    }
}
