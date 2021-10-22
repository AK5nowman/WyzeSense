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
           
            #region Filler Data
            Random rand = new Random(DateTime.Now.Millisecond);

            var sensorFiller = new WyzeSensorModel[]
            {
                new WyzeSensorModel { MAC="AABBCCDD", Description= "Downstairs bedroom window", LastActive = DateTime.Now.AddMinutes(rand.Next(-100, 0)), SensorType = 5 },
                new WyzeSensorModel { MAC="BBCCDDAA", Description= "Garage", LastActive = DateTime.Now.AddMinutes(rand.Next(-100, 0)), SensorType = 2 },
                new WyzeSensorModel { MAC="CCDDAABB", Description= "Crawlspace North", LastActive = DateTime.Now.AddMinutes(rand.Next(-100, 0)), SensorType = 1 },
                new WyzeSensorModel { MAC="DDAABBCC", Description= "Front door", LastActive = DateTime.Now.AddMinutes(rand.Next(-100, 0)), SensorType = 1 }
            };

            modelBuilder.Entity<WyzeSensorModel>().HasData(sensorFiller);
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
                new Template { Id = 1, Name="Switch Status", SensorType = 0x01 },
                new Template { Id = 2, Name="Motion Alarm", SensorType = 0x02 }
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

        public DbSet<WyzeSensorModel> Sensors { get; set; }
        
        public DbSet<Template> Templates { get; set; }
        public DbSet<Topics> Topics { get; set; }
        public DbSet<PayloadPackage> PayloadPackages { get; set; }
        public DbSet<Payload> Payloads { get; set; }

    }
}
