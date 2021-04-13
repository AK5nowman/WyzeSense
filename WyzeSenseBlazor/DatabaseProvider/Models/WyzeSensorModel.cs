using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WyzeSenseBlazor.DatabaseProvider.Models
{
    public class WyzeSensorModel
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(8)]
        public string MAC { get; set; }

        public string Description { get; set; }

        public DateTime LastActive { get; set; }

        public int SensorTypeId { get; set; }
        //Navigation
        public WyzeSensorTypeModel SensorType { get; set; }

        //Relation
        public List<WyzeEventModel> Events { get; set; } = new();

    }
}
