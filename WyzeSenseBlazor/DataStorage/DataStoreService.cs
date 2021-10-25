using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WyzeSenseBlazor.DataStorage
{
    public class DataStoreService : IDataStoreService
    {
        private readonly IDataStoreOptions _dsOptions;
        private readonly ILogger<DataStoreService> _logger;

        public DataStore DataStore { get; }

        public DataStoreService(IDataStoreOptions dataStoreOptions, ILogger<DataStoreService> logger)
        {
            _dsOptions = dataStoreOptions;
            _logger = logger;

            if (File.Exists(dataStoreOptions.Path))
            {
                DataStore = JsonSerializer.Deserialize<DataStore>(File.ReadAllText(dataStoreOptions.Path));
            }
            else
            {
                DataStore = new();
                _logger.LogDebug("Data Store file not found");
            }
        }

        public async Task Save()
        {
            try
            {
                await File.WriteAllTextAsync(_dsOptions.Path, JsonSerializer.Serialize(DataStore));
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }
    }
}
