using System.Collections;

namespace SailboatComputer.UI
{
    /// <summary>
    /// Base class for a display page.
    /// </summary>
    abstract public class Page
    {
        public Page(Newhaven25664OledDriver oled)
        {
            this.oled = oled;
        }

        public void AddDisplayEntity(DisplayEntity displayEntity)
        {
            displayEntity.Page = this;
            displayEntities.Add(displayEntity);
        }

        public virtual void Refresh()
        {
            // lock the OLED to avoid having two pages write to it at the same time
            lock (this.Oled)
            {
                this.Oled.PauseRefresh();
                foreach (DisplayEntity displayEntity in this.displayEntities)
                {
                    displayEntity.Refresh();
                }
                this.Oled.ResumeRefresh();
            }
        }

        protected Newhaven25664OledDriver Oled
        {
            get { return this.oled; }
        }

        public void DrawHorizontalLine(
            int x0,
            int x1,
            int y,
            byte color)
        {
            lock (this.Oled)
            {
                if (!this.IsVisible) return;
                this.Oled.HorizontalLine(x0, x1, y, color);
            }
        }

        public void DrawVerticalLine(
            int x,
            int y0, 
            int y1,
            byte color)
        {
            lock (this.Oled)
            {
                if (!this.IsVisible) return;
                this.Oled.VerticalLine(x, y0, y1, color);
            }
        }

        public void DrawString(
            OledFont font,
            int x,
            int y,
            int width,
            int height,
            Newhaven25664OledDriver.Justify justification,
            string s)
        {
            lock (this.Oled)
            {
                if (!this.IsVisible) return;

                Oled.DrawString(font, x, width, y, justification, s);
            }
        }

        public void SetPixel(
            int x,
            int y,
            byte color)
        {
            lock (this.Oled)
            {
                if (!this.IsVisible) return;

                Oled.SetPixel(x, y, color);
            }
        }

        public void DrawBlock(
            int x0,
            int x1,
            int y0,
            int y1,
            byte color)
        {
            lock (this.Oled)
            {
                if (!this.IsVisible) return;

                Oled.DrawBlock(x0, x1, y0, y1, color);
            }
        }

        public void ScrollRegionUp(
            int x0,
            int x1,
            int y0,
            int y1)
        {
            lock (this.Oled)
            {
                if (!this.IsVisible) return;

                Oled.ScrollRegionUp(x0, x1, y0, y1);
            }
        }

        public void ButtonEvent(object sender, AutoRepeatEventArgs e)
        {
            switch (e.State)
            {
                case AutoRepeatInputPort.AutoRepeatState.Press:
                    this.longPress = false;
                    ButtonPress();
                    break;
                case AutoRepeatInputPort.AutoRepeatState.Tick:
                    this.longPress = true;
                    ButtonTick();
                    break;
                case AutoRepeatInputPort.AutoRepeatState.Release:
                    ButtonRelease(this.longPress);
                    break;
            }
        }

        /// <summary>
        ///     Called when a button is pressed, before release
        /// </summary>
        protected abstract void ButtonPress();

        /// <summary>
        ///     Called periodically as the button is held down
        /// </summary>
        protected abstract void ButtonTick();

        /// <summary>
        ///     Called on a short button press
        /// </summary>
        protected abstract void ButtonRelease(bool longPress);

        public void Visible()
        {
            this.visible = true;
            Refresh();
        }

        public void Hidden()
        {
            this.visible = false;
        }

        public bool IsVisible
        {
            get { return this.visible; }
        }

        private readonly Newhaven25664OledDriver oled;
        protected static readonly int DisplayWidth = 256;
        protected static readonly int HalfDisplayWidth = DisplayWidth / 2;
        protected static readonly int DisplayHeight = 64;
        protected bool visible = false;
        protected bool longPress = false;

        private ArrayList displayEntities = new ArrayList();

        static readonly public OledFont SmallFont = new OledFont(SailboatComputer.Properties.Resources.BinaryResources.fixed5x7);
        static readonly public OledFont BigSkinnyFont = new OledFont(SailboatComputer.Properties.Resources.BinaryResources.RockwellCondensed70);
        //static readonly public OledFont BigFont = new OledFont(SailboatComputer.Properties.Resources.BinaryResources.Rockwell70);
        static readonly public OledFont MediumFont = new OledFont(SailboatComputer.Properties.Resources.BinaryResources.Rockwell46);
        static readonly public OledFont HalfHeightFont = new OledFont(SailboatComputer.Properties.Resources.BinaryResources.Rockwell38);
    }
}
