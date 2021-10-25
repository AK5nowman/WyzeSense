using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WyzeSenseBlazor.DataStorage
{
    public class DataStoreOptions: IDataStoreOptions
    {
        public string Path { get; set; }
    }
}
