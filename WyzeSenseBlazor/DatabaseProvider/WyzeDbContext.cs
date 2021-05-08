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
                new WyzeSensorStateModel{ Id = 1, SensorTypeId = sensorTypes[0].Id, Abnormal = true, State = 1, Type = "Open" },
                new WyzeSensorStateModel{ Id = 2, SensorTypeId = sensorTypes[0].Id, Abnormal = false, State = 0, Type = "Closed" },
                new WyzeSensorStateModel{ Id = 3, SensorTypeId = sensorTypes[1].Id, Abnormal = true, State = 1, Type = "Active" },
                new WyzeSensorStateModel{ Id = 4, SensorTypeId = sensorTypes[1].Id, Abnormal = false, State = 0, Type = "Inactive" },
                new WyzeSensorStateModel{ Id = 5, SensorTypeId = sensorTypes[2].Id, Abnormal = true, State = 1, Type = "Wet" },
                new WyzeSensorStateModel{ Id = 6, SensorTypeId = sensorTypes[2].Id, Abnormal = false, State = 0, Type = "Dry" }
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
                new WyzeEventModel { Id = 1, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, StateId = rand.Next(1, 7), Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 2, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, StateId = rand.Next(1, 7), Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 3, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, StateId = rand.Next(1, 7), Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 4, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, StateId = rand.Next(1, 7), Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 5, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, StateId = rand.Next(1, 7), Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 6, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, StateId = rand.Next(1, 7), Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 7, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, StateId = rand.Next(1, 7), Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
                new WyzeEventModel { Id = 8, SensorMAC=sensorFiller[rand.Next(0, 4)].MAC, EventTypeId = eventTypes[rand.Next(0,2)].Id, StateId = rand.Next(1, 7), Battery = rand.Next(0, 100), EventId = startID++, Signal = rand.Next(20, 70), Time = DateTime.Now.AddMinutes(rand.Next(-100, 0)) },
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

            var payloadFiller = new Payload[]
            {
                new Payload { Id = 1, PayloadPackageId =1,  Name="Testing", Property="Battery" },
                new Payload { Id = 2, PayloadPackageId =1,  Name="Testing", Property="Signal" },
                new Payload { Id = 3, PayloadPackageId =2,  Name="Testing", Property="Battery" },
                new Payload { Id = 4, PayloadPackageId =2,  Name="Testing", Property="State" },
                new Payload { Id = 5, PayloadPackageId =2,  Name="Testing", Property="Signal" },
                new Payload { Id = 6, PayloadPackageId =2,  Name="Testing", Property="Time" },
                new Payload { Id = 7, PayloadPackageId =3,  Name="Testing", Property="Battery" },
                new Payload { Id = 8, PayloadPackageId =3,  Name="Testing", Property="Signal" },
                new Payload { Id = 9, PayloadPackageId =4,  Name="Testing", Property="Battery" },
                new Payload { Id = 10, PayloadPackageId =4, Name="Testing",  Property="State" },
                new Payload { Id = 11, PayloadPackageId =4, Name="Testing",  Property="Signal" },
                new Payload { Id = 12, PayloadPackageId =4, Name="Testing",  Property="Time" }

            };
            var templatePayloadsFiller = new PayloadPackage[]
            {
                new PayloadPackage { Id = 1, TemplateId = 1, Topic="diag" },
                new PayloadPackage { Id = 2, TemplateId = 1, Topic="full" },
                new PayloadPackage { Id = 3, TemplateId = 2, Topic="diag" },
                new PayloadPackage { Id = 4, TemplateId = 2, Topic="full" }
            };
            var templateFiller = new Template[]
            {
                new Template { Id = 1, EventTypeId = 0xA1, Name="Switch Status", SensorTypeId = 0x01 },
                new Template { Id = 2, EventTypeId = 0xA2, Name="Motion Alarm", SensorTypeId = 0x02 }
            };

            var topicFiller = new Topics[]
            {
                new Topics { Id = 1, RootTopic ="switch1", SensorMAC="AABBCCDD", TemplateId=1 },
                new Topics { Id = 2, RootTopic ="switch1", SensorMAC="BBCCDDAA", TemplateId=2 }
            };

            modelBuilder.Entity<Payload>().HasData(payloadFiller);
            modelBuilder.Entity<PayloadPackage>().HasData(templatePayloadsFiller);
            modelBuilder.Entity<Template>().HasData(templateFiller);
            modelBuilder.Entity<Topics>().HasData(topicFiller);

            base.OnModelCreating(modelBuilder);
        }
        public DbSet<ConfigurationModel> Configuation { get; set; }
        public DbSet<WyzeSensorModel> Sensors { get; set; }
        public DbSet<WyzeEventModel> Events { get; set; }
        public DbSet<WyzeEventTypeModel> EventTypes { get; set; }
        public DbSet<WyzeSensorTypeModel> SensorTypes { get; set; }
        public DbSet<WyzeSensorStateModel> SensorStates { get; set; }

        public DbSet<Template> Templates { get; set; }
        public DbSet<Topics> Topics { get; set; }
        public DbSet<PayloadPackage> PayloadPackages { get; set; }
        public DbSet<Payload> Payloads { get; set; }

    }
}
