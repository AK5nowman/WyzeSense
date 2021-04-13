using System;
using System.Linq;
using WyzeSenseBlazor.DatabaseProvider;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (File.Exists("wyzesensordata.db"))
                File.Delete("wyzesensordata.db");
            using (var db = new WyzeDbContext())
            {
                db.Database.EnsureCreated();
                Console.WriteLine("getting events");
                var events = db.Events
                    .Include(x => x.Sensor)
                    .Include(x => x.EventType);

                foreach(var e in events)
                {
                    Console.WriteLine($"Event: {e.Sensor.MAC} Type: {e.EventType.Type} ");
                }
            }
            Console.WriteLine("Hello World!");
        }
    }
}
