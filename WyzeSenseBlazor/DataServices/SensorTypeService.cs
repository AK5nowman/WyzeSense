using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WyzeSenseBlazor.DatabaseProvider.Models;

namespace WyzeSenseBlazor.DataServices
{
    public class SensorTypeService : ISensorTypeService
    {
        private readonly ILogger _logger;
        private readonly IDbContextFactory<DatabaseProvider.WyzeDbContext> _dbContextFactory;
        public SensorTypeService(ILogger<SensorTypeService> logger, IDbContextFactory<DatabaseProvider.WyzeDbContext> dbContextFactory)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<WyzeSensorTypeModel[]> GetSensorTypesAsync()
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                return await dbContext.SensorTypes.Include(p=> p.States).ToArrayAsync();
            }
        }
        public async Task UpdateSensorTypeAsync(WyzeSensorTypeModel wyzeSensorTypeModel)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                dbContext.Update(wyzeSensorTypeModel);
                await dbContext.SaveChangesAsync();
            }
        }
        public async Task UpdateStateAsync(WyzeSensorStateModel wyzeSensorStateModel)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                dbContext.Update(wyzeSensorStateModel);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
