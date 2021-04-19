using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WyzeSenseBlazor.DatabaseProvider.Models;

namespace WyzeSenseBlazor.DataServices
{
    public class ConfigService : IConfigService
    {
        private readonly ILogger<ConfigService> _logger;
        private readonly IDbContextFactory<DatabaseProvider.WyzeDbContext> _dbContextFactory;

        public ConfigService(ILogger<ConfigService> logger, IDbContextFactory<DatabaseProvider.WyzeDbContext> dbContextFactory)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
        }
        public async Task<ConfigurationModel[]> GetConfigAsync()
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                return await dbContext.Configuation.ToArrayAsync();
            }
        }
        public async Task UpdateConfigAsync(ConfigurationModel configurationModel)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                dbContext.Update(configurationModel);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
