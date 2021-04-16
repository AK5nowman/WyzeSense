using System;
using System.Threading;
using System.Threading.Tasks;

namespace WyzeSenseApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            WyzeSenseCore.WyzeDongle dongle = new WyzeSenseCore.WyzeDongle(args[0]);
            dongle.OnSensorAlarm += Dongle_OnSensorAlarm;
            dongle.OnAddSensor += Dongle_OnAddSensor;
            dongle.OnRemoveSensor += Dongle_OnRemoveSensor;
            dongle.OnDongleStateChange += Dongle_OnDongleStateChange;
            Task processDongle =  dongle.StartAsync(default(CancellationToken));

            Random rand = new Random();
            byte[] toB64 = new byte[8];
            rand.NextBytes(toB64);

            bool done = false;
            while (!done)
            {
                string line = Console.ReadLine();
                switch (line.Split(' ')[0])
                {
                    case "ledon":
                        dongle.SetLedAsync(true);
                        break;
                    case "ledoff":
                        dongle.SetLedAsync(false);
                        break;
                    case "list":
                        dongle.RequestRefreshSensorListAsync();
                        break;
                    case "scanon":
                        dongle.StartScanAsync();
                        break;
                    case "scanoff":
                        dongle.StopScanAsync();
                        break;
                    case "q":
                        dongle.Stop();
                        done = true;
                        break;
                    case "loglevel":
                        WyzeSenseCore.Logger.LogLevel = 4;
                        break;
                    case "del":
                        if (line.Split(' ').Length < 2) break;
                        string mac = line.Split(' ')[1];
                        if (mac.Length != 8) { Console.WriteLine("Invalid MAC"); break; }

                        dongle.DeleteSensor(mac);

                        break;
                    default:
                        Console.WriteLine("Type 'q' to exit");
                        break;
                }
            }
            await processDongle;
            
        }

        private static void Dongle_OnDongleStateChange(object sender, WyzeSenseCore.WyzeDongleState e)
        {
            Console.WriteLine($"Dongle Change: {e.ToString()}");
        }

        private static void Dongle_OnRemoveSensor(object sender, WyzeSenseCore.WyzeSensor e)
        {
            Console.WriteLine($"Sensor removed {e.MAC}");
        }

        private static void Dongle_OnAddSensor(object sender, WyzeSenseCore.WyzeSensor e)
        {
            Console.WriteLine($"Sensor added {e.MAC}");
        }

        private static void Dongle_OnSensorAlarm(object sender, WyzeSenseCore.WyzeSenseEvent e)
        {
            Console.WriteLine($"Alarm received {e.ToString()}");
        }
    }
}
