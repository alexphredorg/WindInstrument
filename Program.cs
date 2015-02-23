using Microsoft.SPOT;

namespace WindInstrumentToNMEA
{
    public class Program
    {
        public static void Main()
        {
            Debug.Print("starting");
            Debug.EnableGCMessages(false);

            var program = new SensorProgram();
            program.Go();
        }

        public static string Version = "WindToNMEA v1.0";
    }
}
