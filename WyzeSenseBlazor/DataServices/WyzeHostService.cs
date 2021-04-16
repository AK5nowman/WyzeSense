using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WyzeSenseCore;

namespace WyzeSenseBlazor.DataServices
{
    public class WyzeHostService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IWyzeDongle _dongle;
        private readonly IDbContextFactory<DatabaseProvider.WyzeDbContext> _dbContextFactory;
        public WyzeHostService(ILogger logger, IWyzeDongle dongle, IDbContextFactory<DatabaseProvider.WyzeDbContext> dbContextFactory)
        {
            //Validate the config
            _logger = logger;
            _dongle = dongle;
            _dbContextFactory = dbContextFactory;



        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
