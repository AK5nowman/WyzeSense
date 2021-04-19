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
        [MaxLength(8)]
        public string MAC { get; set; }
        public string Alias { get; set; }
        public string Description { get; set; }
        public DateTime LastActive { get; set; }
        public int SensorTypeId { get; set; }
        
        //Navigation
        public WyzeSensorTypeModel SensorType { get; set; }

        //Relation
        public List<WyzeEventModel> Events { get; set; } = new();
        public List<Topics> Topics { get; set; }

    }
}
