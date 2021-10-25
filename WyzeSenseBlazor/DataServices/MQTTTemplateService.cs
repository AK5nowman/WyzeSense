using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WyzeSenseBlazor.DataStorage.Models;
using WyzeSenseBlazor.DataStorage;

namespace WyzeSenseBlazor.DataServices
{
    public class MQTTTemplateService : IMQTTTemplateService
    {
        private readonly ILogger _logger;
        private readonly IDataStoreService _dataStore;
        public MQTTTemplateService(ILogger<MQTTTemplateService> logger, IDataStoreService dataStore)
        {
            _logger = logger;
            _dataStore = dataStore;
        }

        public async Task<Template[]> GetTemplatesAsync()
        {
            return _dataStore.DataStore.Templates.Values.ToArray();
        }
    }
}
