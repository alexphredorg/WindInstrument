using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    class BasicPageWithHelp : Page
    {
        public BasicPageWithHelp(Newhaven25664OledDriver oled) : base(oled)            
        {
            this.helpPage = new BasicPage(oled);
        }

        protected override void ButtonRelease(bool longPress)
        {
            lock (this.Oled)
            {
                this.helpPage.Hidden();
                this.Oled.ClearDisplay();
                this.Visible();
            }
        }

        protected override void ButtonPress()
        {
            lock (this.Oled)
            {
                this.Hidden();
                this.Oled.ClearDisplay();
                this.helpPage.Visible();
            }
        }

        protected override void ButtonTick()
        {
        }

        public override void Refresh()
        {
            base.Refresh();
        }

        public void AddHelpDisplayEntity(DisplayEntity displayEntity)
        {
            this.helpPage.AddDisplayEntity(displayEntity);
        }

        public void AddBothDisplayEntity(DisplayEntity displayEntity)
        {
            this.AddDisplayEntity(displayEntity);
            this.helpPage.AddDisplayEntity(displayEntity);
        }

        private readonly BasicPage helpPage;
    }
}
