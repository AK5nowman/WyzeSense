using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WyzeSenseBlazor.DataStorage
{
    public interface IDataStoreOptions
    {
        public string Path { get; set; }
    }
}
