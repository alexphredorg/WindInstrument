using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    class TextDisplayEntity : DisplayEntity
    {
        public TextDisplayEntity(int x, int y, int width, int height, OledFont font, DisplayVariable displayVariable)
            : base(x, y, width, height)
        {
            this.font = font;
            this.displayVariable = displayVariable;
            this.displayVariable.DisplayVariableUpdateEvent += this.Update;
        }

        public virtual void Update()
        {
            this.value = displayVariable.ToString();
            if (this.Page.IsVisible)
            {
                Refresh();
            }
        }

        public override void Refresh()
        {
            this.Page.DrawString(font, this.X, this.Y, this.Width, this.Height, Newhaven25664OledDriver.Justify.Center, this.value);
        }

        protected OledFont font;
        protected DisplayVariable displayVariable;
        protected string value = String.Empty;
    }
}
