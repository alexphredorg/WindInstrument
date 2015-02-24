using System;

namespace SailboatComputer.UI
{
    public class DisplayVariable
    {
        /// <summary>
        /// The constructor for a simple display-only variable that operates in two-up mode.
        /// </summary>
        /// <param name="name">The variable name</param>
        /// <param name="objectType">The type of object stored.  Not really used, good for documentation.</param>
        public DisplayVariable(string name, Type objectType)
        {
            this.name = name;
            this.objectType = objectType;
            this.value = String.Empty;
        }
        
        /// <summary>
        /// What is the visible name of this property?
        /// </summary>
        public string Name { get { return this.name; } }

        /// <summary>
        /// What is the value of this property?
        /// </summary>
        public object Value 
        { 
            get 
            { 
                return this.value; 
            }
            set
            {
                if (value.GetType() != this.objectType)
                {
                    throw new ArgumentException("invalid type");
                }
                this.value = value;
                if (this.DisplayVariableUpdateEvent != null)
                {
                    DisplayVariableUpdateEvent();
                }
            }
        }

        public override string ToString()
        {
            return value.ToString();
        }

        /// <summary>
        /// What is the type of this property?
        /// </summary>
        public Type ObjectType { get { return this.objectType; } }

        public delegate void DisplayVariableUpdateHandler();
        public event DisplayVariableUpdateHandler DisplayVariableUpdateEvent;

        private readonly string name;
        private object value;
        private Type objectType;
    }
}