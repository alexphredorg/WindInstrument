using System;
using System.Threading;
using Microsoft.SPOT;

namespace SailboatComputer
{
    /// <summary>
    /// DebugLog is used to display a log of incoming NEMA commands and other debug data
    /// </summary>
    class DebugLog
    {
        public static void WriteLine(string s)
        {
            Debug.Print(s);
        }
    }
}