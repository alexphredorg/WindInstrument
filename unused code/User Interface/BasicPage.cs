using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    class BasicPage : Page
    {
        public BasicPage(Newhaven25664OledDriver oled) : base(oled)            
        {
        }

        protected override void ButtonRelease(bool longPress)
        {
        }

        protected override void ButtonPress()
        {
        }

        protected override void ButtonTick()
        {
        }

        public override void Refresh()
        {
            base.Refresh();
        }
    }
}
