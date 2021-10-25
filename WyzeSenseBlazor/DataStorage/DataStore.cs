using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WyzeSenseBlazor.DataStorage.Models;

namespace WyzeSenseBlazor.DataStorage
{
    public class DataStore
    {
        public Dictionary<string, WyzeSensorModel> Sensors { get; set; } = new();
        public Dictionary<string, Template> Templates { get; set; } = new();
    }
}
