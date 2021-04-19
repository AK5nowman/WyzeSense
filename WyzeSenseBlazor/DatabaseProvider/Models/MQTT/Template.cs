using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WyzeSenseBlazor.DatabaseProvider.Models
{
    public class Template
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(128)]
        public string Name { get; set; }
        public int SensorTypeId { get; set; }
        public int EventTypeId { get; set; }

        //Navigational
        public WyzeSensorTypeModel SensorType { get; set; }
        public WyzeEventTypeModel EventType { get; set; }
        public List<PayloadPackage> PayloadPackages { get; set; }
    }
}

