using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WyzeSenseBlazor.DatabaseProvider.Models;

namespace WyzeSenseBlazor.DataServices
{
    public class MQTTTemplateService : IMQTTTemplateService
    {
        private readonly ILogger _logger;
        private readonly IDbContextFactory<DatabaseProvider.WyzeDbContext> _dbContextFactory;
        public MQTTTemplateService(ILogger<MQTTTemplateService> logger, IDbContextFactory<DatabaseProvider.WyzeDbContext> dbContextFactory)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Template[]> GetTemplatesAsync()
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                return await dbContext.Templates
                    .Include(p => p.PayloadPackages)
                        .ThenInclude(p => p.Payloads)
                    .ToArrayAsync();
            }
        }
    }
}
