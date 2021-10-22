using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace WyzeSenseApp
{
    class Program
    {
        private static WyzeSenseCore.IWyzeSenseLogger mylogger;
        static async Task Main(string[] args)
        {
            mylogger = new Logger();
            //

            WyzeSenseCore.WyzeDongle dongle = new WyzeSenseCore.WyzeDongle(mylogger);
            dongle.OpenDevice(args[0]);
            dongle.OnSensorEvent += Dongle_OnSensorEvent;
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
                        if (mac.Length != 8) { mylogger.LogError("Invalid MAC"); break; }

                        dongle.DeleteSensorAsync(mac);

                        break;
                    case "cc1310up":
                        dongle.RequestCC1310Update();
                        break;
                    default:
                        mylogger.LogInformation("Type 'q' to exit");
                        break;
                }
            }
            await processDongle;
            
        }

        private static void Dongle_OnDongleStateChange(object sender, WyzeSenseCore.WyzeDongleState e)
        {
            mylogger.LogInformation($"Dongle Change: {e.ToString()}");
        }

        private static void Dongle_OnRemoveSensor(object sender, WyzeSenseCore.WyzeSensor e)
        {
            mylogger.LogInformation($"Sensor removed {e.MAC}");
        }

        private static void Dongle_OnAddSensor(object sender, WyzeSenseCore.WyzeSensor e)
        {
            mylogger.LogInformation($"Sensor added {e.MAC}");
        }

        private static void Dongle_OnSensorEvent(object sender, WyzeSenseCore.WyzeSenseEvent e)
        {
            mylogger.LogInformation($"Alarm received {e.ToString()}");
        }
    }
}
