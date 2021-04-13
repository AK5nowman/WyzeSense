using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WyzeSenseBlazor.DatabaseProvider.Models
{
    public class WyzeSensorStateModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public bool State { get; set; }

        [MaxLength(16)]
        public string Type { get; set; }
        
        //Foreign Keys
        public int SensorTypeId { get; set; }

        //Navigation
        public WyzeSensorTypeModel SensorType { get; set; }
    }
}
