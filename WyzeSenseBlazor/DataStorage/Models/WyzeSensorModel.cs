using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WyzeSenseBlazor.DataStorage.Models
{
    public class WyzeSensorModel
    {
        public string MAC { get; set; }
        public string Alias { get; set; }
        public string Description { get; set; }
        public int SensorType { get; set; }
        public DateTime LastActive {get; set; }
        public List<Topics> Topics { get; set; }

    }
}
