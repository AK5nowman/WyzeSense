using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WyzeSenseBlazor.DatabaseProvider.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.Options;

namespace WyzeSenseBlazor.DatabaseProvider
{
    public class WyzeDbContext : DbContext
    {
        public WyzeDbContext(DbContextOptions options) : base(options) { }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Default Data
            var sensorTypes = new WyzeSensorTypeModel[] 
            {
                new WyzeSensorTypeModel { Id = 0x01, Type = "Switch" },
                new WyzeSensorTypeModel { Id = 0x02, Type = "Motion" },
                new WyzeSensorTypeModel { Id = 0x03, Type = "Water" }
            };
            var sensorStates = new WyzeSensorStateModel[]
            {
                new WyzeSensorStateModel{ Id = 1, SensorTypeId = sensorTypes[0].Id, State = true, Type = "Open" },
                new WyzeSensorStateModel{ Id = 2, SensorTypeId = sensorTypes[0].Id, State = false, Type = "Closed" },
                new WyzeSensorStateModel{ Id = 3, SensorTypeId = sensorTypes[1].Id, State = true, Type = "Active" },
                new WyzeSensorStateModel{ Id = 4, SensorTypeId = sensorTypes[1].Id, State = false, Type = "Inactive" },
                new WyzeSensorStateModel{ Id = 5, SensorTypeId = sensorTypes[2].Id, State = true, Type = "Wet" },
                new WyzeSensorStateModel{ Id = 6, SensorTypeId = sensorTypes[2].Id, State = false, Type = "Dry" }
            };
            var eventTypes = new WyzeEventTypeModel[]
            {
                new WyzeEventTypeModel { Id = 0xA1, Type = "Status"},
                new WyzeEventTypeModel { Id = 0xA2, Type = "Alarm"}
            };
            modelBuilder.Entity<WyzeSensorStateModel>().HasData(sensorStates);
            modelBuilder.Entity<WyzeSensorTypeModel>().HasData(sensorTypes);
            modelBuilder.Entity<WyzeEventTypeModel>().HasData(eventTypes);
            #endregion

            #region Filler Data
            Random rand = new Random(DateTime.Now.Millisecond);

            var sensorFiller = new WyzeSensorModel[]
            {
                new WyzeSensorModel { MAC="AABBCCDD", Description= "Downstairs bedroom window", LastActive = DateTime.Now.AddMinutes(rand.Next(-100, 0)), SensorTypeId = sensorTypes[0].Id },
                new WyzeSensorModel { MAC="BBCCDDAA", Description= "Garage", LastActive = DateTime.Now.AddMinutes(rand.Next(-100, 0)), SensorTypeId = sensorTypes[1].Id },
                new WyzeSensorModel { MAC="CCDDAABB", Description= "Crawlspace North", LastActive = DateTime.Now.AddMinutes(rand.Next(-100, 0)), SensorTypeId = sensorTypes[2].Id },
                new WyzeSensorModel { MAC="DDAABBCC", Description= "Front door", LastActive = DateTime.Now.AddMinutes(rand.Next(-100, 0)), SensorTypeId = sensorTypes[0].Id }
            };

            var bools = new bool[] { true, false };
            int startID = 1000;
            var eventFiller = new WyzeEventModel[]
            {
                new WyzeEventModel { Id = 1, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, State = bools[rand.Next(0, 2)], Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 2, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, State = bools[rand.Next(0, 2)], Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 3, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, State = bools[rand.Next(0, 2)], Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 4, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, State = bools[rand.Next(0, 2)], Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 5, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, State = bools[rand.Next(0, 2)], Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 6, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, State = bools[rand.Next(0, 2)], Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 7, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, State = bools[rand.Next(0, 2)], Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 8, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, State = bools[rand.Next(0, 2)], Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
            };

            var configFiller = new ConfigurationModel[]
            {
                new ConfigurationModel { Setting= "usbPath", Value = "/dev/hidraw1"},
                new ConfigurationModel { Setting= "mqttHost", Value = "192.168.1.10"},
                new ConfigurationModel { Setting= "mqttUsername", Value = "smarthome"},
                new ConfigurationModel { Setting= "mqttPassword", Value = "strongpass!23"},
            };

            modelBuilder.Entity<WyzeSensorModel>().HasData(sensorFiller);
            modelBuilder.Entity<WyzeEventModel>().HasData(eventFiller);
            modelBuilder.Entity<ConfigurationModel>().HasData(configFiller);
            #endregion

            base.OnModelCreating(modelBuilder);
        }
        public DbSet<ConfigurationModel> Configuation { get; set; }
        public DbSet<WyzeSensorModel> Sensors { get; set; }
        public DbSet<WyzeEventModel> Events { get; set; }
        public DbSet<WyzeEventTypeModel> EventTypes { get; set; }
        public DbSet<WyzeSensorTypeModel> SensorTypes { get; set; }
        public DbSet<WyzeSensorStateModel> SensorStates { get; set; }

        
    }
}
