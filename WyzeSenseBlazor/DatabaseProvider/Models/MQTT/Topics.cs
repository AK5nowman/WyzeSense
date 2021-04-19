using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WyzeSenseBlazor.DatabaseProvider.Models
{
    public class Topics
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(128)]
        public string RootTopic { get; set; }
        public string SensorMAC { get; set; }
        public int TemplateId { get; set; }

        //Navigational
        public WyzeSensorModel Sensor { get; set; }
        public Template Template { get; set; }

    }
}

