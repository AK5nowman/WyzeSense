using System;
using System.Linq;
using WyzeSenseBlazor.DatabaseProvider;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;

namespace TestApp
{
    public static class TaskExtensionsss
    {

    }
    class Program
    {
        static void Main(string[] args)
        {
            var options = new DbContextOptionsBuilder().UseSqlite("Data Source=wyzesensor.db");
            using (var db = new WyzeDbContext( options.Options))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                Console.WriteLine("getting events");
                var events = db.Events
                    .Include(x => x.Sensor)
                    .Include(x => x.EventType);

                foreach (var e in events)
                {
                    Console.WriteLine($"Event: {e.Sensor.MAC} Type: {e.EventType.Type} ");
                }
            }
            Console.WriteLine("Hello World!");
        }

    }
}
