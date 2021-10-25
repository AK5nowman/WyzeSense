using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WyzeSenseBlazor.DataStorage.Models
{
    public class PayloadPackage
    {
        public string Topic { get; set; }
        public Dictionary<string, string> Payload { get; set; }

    }
}
