using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

//
// The hardware for the user interface consists of the following basic elements:
// * A 256x64 pixel OLED that uses the bottom ~10 rows to describe two buttons and the rest is available
//   for display.
// * Two buttons
//
//
// This is a simplified reimplementation of the user interface
// * The left button is used to switch screens
// * Long press the left button to change contrast
// * The right button is used on some screens for further action
//
namespace SailboatComputer.UI
{
    class SimpleUserInterface
    {
        /// <summary>
        /// Create the User Interface components.
        /// </summary>
        /// <param name="oled">The OLED driver for the display</param>
        /// <param name="leftButton">The pin wired to the left button</param>
        /// <param name="rightButton">The pin wired to the right button</param>
        public SimpleUserInterface(
            Newhaven25664OledDriver oled,
            Cpu.Pin leftButton,
            Cpu.Pin rightButton)
        {
            this.oled = oled;
            this.oled.SetContrast(this.oledContrast[this.oledContrastIndex]);

            // wire up the buttons
            this.leftButton = new AutoRepeatInputPort(leftButton, Port.ResistorMode.PullUp, false);
            this.rightButton = new AutoRepeatInputPort(rightButton, Port.ResistorMode.PullUp, false);

            this.leftButton.StateChanged += new AutoRepeatEventHandler(leftButton_OnInterrupt);
            this.rightButton.StateChanged += new AutoRepeatEventHandler(rightButton_OnInterrupt);

            // setup the pages
            this.pages = new Page[] {
                CreateSailingPage(),
                //CreateSpeedPage(),
                CreateWindPage(),
                CreateStopwatchPage()
            };

            this.currentPageIndex = 0;
            this.pages[this.currentPageIndex].Visible();

            // refresh the display
            this.Refresh();

            /*
            Random r = new Random();
            double windSpeed = 10;
            double speedOverGround = 4;
            double depth = 20;
            int windDir = 0;
            while (true)
            {
                windSpeed += r.NextDouble() - 0.5;
                speedOverGround += r.NextDouble() - 0.5;
                depth += (r.NextDouble() * 12) - 5;
                windDir = (windDir + r.Next(4) - 2) % 360;
                if (windDir < 0) windDir += 360;
                DisplayVariables.WindSpeed.Value = windSpeed;
                DisplayVariables.SpeedOverGround.Value = speedOverGround;
                DisplayVariables.WindDirection.Value = windDir;
                DisplayVariables.Depth.Value = depth;
                DebugLog.WriteLine("hi");
                Thread.Sleep(100);
            }
            */
        }

        private Page CreateSailingPage()
        {
            BasicPageWithHelp page = new BasicPageWithHelp(this.oled);

            // normal display
            page.AddDisplayEntity(new FloatDisplayEntity(0, 24, 80, 40, Page.MediumFont, DisplayVariables.WindSpeed, 20));
            page.AddDisplayEntity(new FloatDisplayEntity(84, 24, 84, 40, Page.MediumFont, DisplayVariables.Depth, 20));
            page.AddDisplayEntity(new FloatDisplayEntity(172, 24, 84, 40, Page.MediumFont, DisplayVariables.SpeedOverGround, 10));
            page.AddDisplayEntity(new TextDisplayEntity(220, 0, 36, 20, Page.SmallFont, DisplayVariables.WindDirection));
            page.AddDisplayEntity(new LineGraphCompassDisplayEntity(0, 0, 216, 20, DisplayVariables.WindDirection));
            page.AddDisplayEntity(new HorizontalLineDisplayEntity(0, 21, 256, 1, 6));
            page.AddDisplayEntity(new VerticalLineDisplayEntity(83, 22, 1, 40, 6));
            page.AddDisplayEntity(new VerticalLineDisplayEntity(171, 22, 1, 40, 6));

            // help display
            page.AddHelpDisplayEntity(new LabelDisplayEntity(0, 6, 256, 20, Page.SmallFont, "Wind Direction"));
            page.AddHelpDisplayEntity(new LabelDisplayEntity(0, 34, 80, 40, Page.SmallFont, "Wind Speed"));
            page.AddHelpDisplayEntity(new LabelDisplayEntity(84, 34, 84, 40, Page.SmallFont, "Depth"));
            page.AddHelpDisplayEntity(new LabelDisplayEntity(172, 34, 84, 40, Page.SmallFont, "Speed over"));
            page.AddHelpDisplayEntity(new LabelDisplayEntity(172, 44, 84, 40, Page.SmallFont, "Ground"));
            page.AddHelpDisplayEntity(new HorizontalLineDisplayEntity(0, 21, 256, 1, 6));
            page.AddHelpDisplayEntity(new VerticalLineDisplayEntity(83, 22, 1, 40, 6));
            page.AddHelpDisplayEntity(new VerticalLineDisplayEntity(171, 22, 1, 40, 6));

            return page;
        }

        private Page CreateSpeedPage()
        {
            VerticalLineGraphDisplayEntity speedOverGroundGraph =
                new VerticalLineGraphDisplayEntity(60, 0, 68, 63, 0, 10, DisplayVariables.SpeedOverGround, false, 5);
            VerticalLineGraphDisplayEntity windSpeedGraph =
                new VerticalLineGraphDisplayEntity(188, 0, 68, 63, 0, 25, DisplayVariables.WindSpeed, false, 5);

            BasicPageWithHelp page = new BasicPageWithHelp(this.oled);
            page.AddDisplayEntity(new FloatDisplayEntity(0, 0, 60, 48, Page.MediumFont, DisplayVariables.SpeedOverGround, 10));
            page.AddDisplayEntity(new LabelDisplayEntity(0, 48, 60, 16, Page.SmallFont, "SpeedOG"));
            page.AddDisplayEntity(speedOverGroundGraph);
            page.AddDisplayEntity(new FloatDisplayEntity(128, 0, 60, 48, Page.MediumFont, DisplayVariables.WindSpeed, 10));
            page.AddDisplayEntity(new LabelDisplayEntity(128, 48, 60, 16, Page.SmallFont, "WindSpd"));
            page.AddDisplayEntity(windSpeedGraph);

            return page;
        }

        private Page CreateWindPage()
        {
            VerticalLineGraphDisplayEntity windSpeedGraph =
                new VerticalLineGraphDisplayEntity(60, 0, 68, 63, 0, 25, DisplayVariables.WindSpeed, false, 5);
            CompassGraphDisplayEntity windDirGraph =
                new CompassGraphDisplayEntity(132, 0, 124, 63, DisplayVariables.WindDirection);

            BasicPageWithHelp page = new BasicPageWithHelp(this.oled);
            page.AddDisplayEntity(new FloatDisplayEntity(0, 0, 60, 32, Page.HalfHeightFont, DisplayVariables.SpeedOverGround, 10));
            page.AddDisplayEntity(new FloatDisplayEntity(0, 32, 60, 32, Page.HalfHeightFont, DisplayVariables.WindSpeed, 10));
            page.AddDisplayEntity(windSpeedGraph);
            page.AddDisplayEntity(new VerticalLineDisplayEntity(130, 0, 1, 63, 6));
            page.AddDisplayEntity(windDirGraph);

            page.AddHelpDisplayEntity(new LabelDisplayEntity(0, 8, 60, 10, Page.SmallFont, "Speed Over"));
            page.AddHelpDisplayEntity(new LabelDisplayEntity(0, 18, 60, 10, Page.SmallFont, "Ground"));
            page.AddHelpDisplayEntity(new LabelDisplayEntity(0, 40, 60, 10, Page.SmallFont, "Wind"));
            page.AddHelpDisplayEntity(new LabelDisplayEntity(0, 48, 60, 10, Page.SmallFont, "Speed"));
            page.AddHelpDisplayEntity(new LabelDisplayEntity(60, 22, 64, 10, Page.SmallFont, "Wind"));
            page.AddHelpDisplayEntity(new LabelDisplayEntity(60, 30, 64, 10, Page.SmallFont, "Speed"));
            page.AddHelpDisplayEntity(new LabelDisplayEntity(128, 22, 128, 10, Page.SmallFont, "Wind"));
            page.AddHelpDisplayEntity(new LabelDisplayEntity(128, 30, 128, 10, Page.SmallFont, "Direction"));
            page.AddHelpDisplayEntity(new VerticalLineDisplayEntity(130, 0, 1, 63, 6));
            page.AddHelpDisplayEntity(new VerticalLineDisplayEntity(60, 0, 1, 63, 6));
            page.AddHelpDisplayEntity(new HorizontalLineDisplayEntity(0, 32, 60, 1, 6));

            return page;
        }

        private Page CreateStopwatchPage()
        {
            return new StopwatchPage(this.oled);
        }

        /// <summary>
        /// This event is called when the left button is pressed.  The left button can only be used
        /// to cycle through display pages.  A long press is used to change the display contrast.
        /// </summary>
        /// <param name="sender">ignored</param>
        /// <param name="args">button state</param>
        private void leftButton_OnInterrupt(object sender, AutoRepeatEventArgs args)
        {
            switch (args.State)
            {
                case AutoRepeatInputPort.AutoRepeatState.Press:
                    this.longLeftPress = false;
                    break;
                case AutoRepeatInputPort.AutoRepeatState.Tick:
                    this.longLeftPress = true;
                    CycleContrast();
                    break;
                case AutoRepeatInputPort.AutoRepeatState.Release:
                    if (!this.longLeftPress)
                    {
                        lock (this.oled)
                        {
                            this.pages[this.currentPageIndex].Hidden();
                            this.currentPageIndex = (this.currentPageIndex + 1) % this.pages.Length;
                            this.oled.ClearDisplay();
                            this.pages[this.currentPageIndex].Visible();
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// This method is called when the right button is pressed.  It is passed to the display 
        /// page.
        /// </summary>
        /// <param name="sender">ignored</param>
        /// <param name="args">button state</param>
        void rightButton_OnInterrupt(object sender, AutoRepeatEventArgs args)
        {
            this.pages[this.currentPageIndex].ButtonEvent(sender, args);
        }

        public void Refresh()
        {
            this.pages[this.currentPageIndex].Refresh();
        }

        /// <summary>
        /// Change the contrast.
        /// </summary>
        private void CycleContrast()
        {
            this.oledContrastIndex = (this.oledContrastIndex + 1) % this.oledContrast.Length;
            if (this.oledContrast[this.oledContrastIndex] == 0)
            {
                this.oled.Off();
            }
            else
            {
                this.oled.SetContrast(this.oledContrast[this.oledContrastIndex]);
                this.oled.On();
            }
        }

        private Newhaven25664OledDriver oled;

        private Page[] pages;
        private int currentPageIndex;
        
        private AutoRepeatInputPort leftButton;
        private AutoRepeatInputPort rightButton;
        private bool longLeftPress;

        private int[] oledContrast = new int[] { 0, 0x01, 0x60, 0xff };
        private int oledContrastIndex = 3;
    }
}
