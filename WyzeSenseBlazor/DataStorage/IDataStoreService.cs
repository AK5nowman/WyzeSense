using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WyzeSenseBlazor.DataStorage
{
    public interface IDataStoreService
    {
        public DataStore DataStore {get; }
        public Task Save();
    }
}
