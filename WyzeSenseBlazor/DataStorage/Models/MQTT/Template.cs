using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WyzeSenseBlazor.DataStorage.Models
{
    public class Template
    {
        public string Name { get; set; }
        public int SensorType { get; set; }
        public List<PayloadPackage> PayloadPackages { get; set; }
    }
}

