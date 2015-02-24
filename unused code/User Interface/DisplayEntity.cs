using System;
using Microsoft.SPOT;

namespace SailboatComputer.UI
{
    public abstract class DisplayEntity
    {
        public DisplayEntity(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        abstract public void Refresh();

        public Page Page 
        { 
            get { return this.page; }
            set { this.page = value; }
        }

        protected int X { get { return this.x; } }
        protected int Y { get { return this.y; } }
        protected int Width { get { return this.width; } }
        protected int Height { get { return this.height; } } 

        int x;
        int y;
        int width;
        int height;

        Page page;
    }
}
