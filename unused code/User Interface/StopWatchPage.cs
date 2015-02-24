using System;
using System.Threading;

namespace SailboatComputer.UI
{
    /// <summary>
    /// Stopwatch is used to implement a timer (stopwatch)
    /// </summary>
    class StopwatchPage : Page
    {
        public StopwatchPage(Newhaven25664OledDriver oled) : base(oled)
        {
            this.m_timer = new Timer(new TimerCallback(UpdateDisplayCallback), null, Timeout.Infinite, Timeout.Infinite);
            this.DisplayTime();
            this.UpdateRightLabel();
        }

        protected override void ButtonPress()
        {
            this.RightLabel = "hold to reset";
        }

        protected override void ButtonTick()
        {
            this.RightLabel = "will reset";
        }

        protected override void ButtonRelease(bool fLongPress)
        {
            if (fLongPress)
            {
                this.Reset();
                UpdateRightLabel();
            }
            else if (this.m_active)
            {
                Stop();
            }
            else
            {
                Start();
            }
        }

        private void Start()
        {
            this.m_active = true;
            UpdateRightLabel();
            this.m_resetPoint = DateTime.Now.Subtract(this.m_cumulatedTime);
            this.m_timer.Change(0, RefreshInterval);
        }

        private void Stop()
        {
            this.m_timer.Change(Timeout.Infinite, Timeout.Infinite);
            this.m_active = false;
            UpdateRightLabel();
            this.m_cumulatedTime = DateTime.Now.Subtract(this.m_resetPoint);
            DisplayTime();
        }

        private void UpdateRightLabel()
        {
            this.RightLabel = (this.m_active) ? "stop/(reset)" : "start/(reset)";
        }

        private void Reset()
        {
            this.m_cumulatedTime = TimeSpan.Zero;
            this.m_resetPoint = DateTime.Now;
            DisplayTime();
        }

        private void DisplayTime()
        {
            TimeSpan displayTime = (this.m_active) ? DateTime.Now.Subtract(this.m_resetPoint) : this.m_cumulatedTime;
            // NETMF doesn't support the "D2" format string, or any format string with leading zeros
            this.DisplayValue = displayTime.Hours.ToString() + ":" +
                                ((displayTime.Minutes < 10) ? "0" : "") +
                                displayTime.Minutes.ToString() + ":" +
                                ((displayTime.Seconds < 10) ? "0" : "") +
                                displayTime.Seconds.ToString();
        }

        private void UpdateDisplayCallback(object ignored)
        {
            if (this.IsVisible)
            {
                DisplayTime();
            }
        }

        protected string DisplayValue
        {
            set
            {
                this.timeValue = value;
                if (this.IsVisible)
                {
                    this.DrawString(Page.BigSkinnyFont, 0, 0, 256, 48, Newhaven25664OledDriver.Justify.Center, value);
                }
            }
        }

        private string RightLabel
        {
            set
            {
                this.rightLabel = value;
                if (this.IsVisible)
                {
                    this.DrawString(Page.SmallFont, 128, 50, 128, 14, Newhaven25664OledDriver.Justify.Right, value);
                }
            }
        }

        public override void Refresh()
        {
            if (this.IsVisible)
            {
                this.DrawString(Page.SmallFont, 0, 50, 128, 14, Newhaven25664OledDriver.Justify.Left, "Stopwatch");
                this.DrawString(Page.SmallFont, 128, 50, 128, 14, Newhaven25664OledDriver.Justify.Right, this.rightLabel);
                this.DrawString(Page.BigSkinnyFont, 0, 0, 256, 48, Newhaven25664OledDriver.Justify.Center, this.timeValue);
            }
        }

        private string timeValue = "";
        private string rightLabel;
        private DateTime m_resetPoint;
        private bool m_active;
        private TimeSpan m_cumulatedTime;
        private readonly Timer m_timer;
        private const int RefreshInterval = 100;
    }
}