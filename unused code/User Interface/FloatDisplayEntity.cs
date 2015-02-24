using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    class FloatDisplayEntity : TextDisplayEntity
    {
        public FloatDisplayEntity(int x, int y, int width, int height, OledFont font, DisplayVariable displayVariable, double maxDecimal)
            : base(x, y, width, height, font, displayVariable)
        {
            this.maxDecimal = maxDecimal;
        }

        public override void Update()
        {
            if (this.displayVariable.ObjectType != typeof(double))
            {
                base.value = "..";
                base.Refresh();
            }
            else
            {
                double d = (double)this.displayVariable.Value;
                base.value = d.ToString((d >= this.maxDecimal) ? "N0" : "N1");
                base.Refresh();
            }
        }

        readonly private double maxDecimal;
    }
}
