using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace WyzeSenseBlazor.DatabaseProvider.Models
{
    public class WyzeEventModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        //FKey
        public string SensorMAC { get; set; }
        //FKey
        public int EventTypeId { get; set; }
        public bool State { get; set; }
        public int EventId { get; set; }
        public int Battery { get; set; }
        public int Signal { get; set; }
        public DateTime Time { get; set; }


        //Navigation
        public WyzeSensorModel Sensor { get; set; }
        public WyzeEventTypeModel EventType { get; set; }
    }
}
