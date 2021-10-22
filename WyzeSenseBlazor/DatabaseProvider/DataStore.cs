using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WyzeSenseBlazor.DatabaseProvider.Models;

namespace WyzeSenseBlazor.DatabaseProvider
{
    public class DataStore
    {
        public Dictionary<string, WyzeSensorModel> Sensors { get; set; }
        public Dictionary<int, Template> Templates { get; set; }
    }
}
