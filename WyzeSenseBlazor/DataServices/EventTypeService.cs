using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WyzeSenseBlazor.DatabaseProvider.Models;

namespace WyzeSenseBlazor.DataServices
{
    public class EventTypeService : IEventTypeService
    {
        private readonly ILogger _logger;
        private readonly IDbContextFactory<DatabaseProvider.WyzeDbContext> _dbContextFactory;
        public EventTypeService(ILogger<EventTypeService> logger, IDbContextFactory<DatabaseProvider.WyzeDbContext> dbContextFactory)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<WyzeEventTypeModel[]> GetEventTypesAsync()
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                return await dbContext.EventTypes.ToArrayAsync();
            }
        }
        public async Task UpdateEventyTypeAsync(WyzeEventTypeModel wyzeEventTypeModel)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                dbContext.Update(wyzeEventTypeModel);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
