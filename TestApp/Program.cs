﻿using System;
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
            if (File.Exists("wyzesensor.db"))
                File.Delete("wyzesensor.db");
            using (var db = new WyzeDbContext(new DbContextOptionsBuilder().UseSqlite("Data Source=wyzesensor.db")))
            {
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