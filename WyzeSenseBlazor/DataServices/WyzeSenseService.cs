using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WyzeSenseBlazor.DatabaseProvider;
using WyzeSenseBlazor.DatabaseProvider.Models;
using WyzeSenseCore;
using System.Threading.Channels;
using WyzeSenseBlazor.Settings;

namespace WyzeSenseBlazor.DataServices
{
    public class WyzeSenseService : IHostedService, IWyzeSenseService
    {
        private readonly ILogger _logger;
        private readonly WyzeSenseCore.IWyzeDongle _wyzeDongle;
        private readonly IDbContextFactory<WyzeDbContext> _dbContextFactory;

        private string devicePath;

        private Task processTask;

        private WyzeDongleState dongleState;
        public WyzeSenseService(ILogger<WyzeSenseService> logger, WyzeSenseCore.IWyzeDongle wyzeDongle, IDbContextFactory<DatabaseProvider.WyzeDbContext> dbContextFactory)
        {
            _logger = logger;
            _logger.LogInformation($"Creating WyzeSenseService");

            _dbContextFactory = dbContextFactory;
            _wyzeDongle = wyzeDongle;

            devicePath = AppSettingsProvider.WyzeSettings.UsbPath;

            _wyzeDongle.OnAddSensor += _wyzeDongle_OnAddSensor;
            _wyzeDongle.OnRemoveSensor += _wyzeDongle_OnRemoveSensor;
            _wyzeDongle.OnDongleStateChange += _wyzeDongle_OnDongleStateChange;
            _wyzeDongle.OnSensorEvent += _wyzeDongle_OnSensorEvent;
        }



        public event EventHandler<WyzeSensorModel> OnAddSensor;
        public event EventHandler<string> OnRemoveSensor;
        public event EventHandler<WyzeSenseEvent> OnEvent;
        public event EventHandler<WyzeDongleState> OnDongleStateChange;
        public event EventHandler<string> OnFailStart;

        public bool Running { get=> running; }
        private bool running = false;
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //Let us try to start
            if (!_wyzeDongle.OpenDevice(devicePath))
            {
                OnFailStart?.Invoke(this, $"Failed to open device: {devicePath}");
            }

            //At this point we should have succesfully opened.
            running = true;
            _logger.LogInformation("[ExecuteAsync] Starting dongle start");
            await _wyzeDongle.StartAsync(cancellationToken);
            _logger.LogInformation("[ExecuteAsync] Finished dongle start");
            running = false;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _wyzeDongle.Stop();
            return Task.CompletedTask;
        }

        private void _wyzeDongle_OnDongleStateChange(object sender, WyzeDongleState e)
        {
            dongleState = e;
            this.OnDongleStateChange?.Invoke(this, e);
        }
        private async void _wyzeDongle_OnSensorEvent(object sender, WyzeSenseEvent e)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var sensorModel = await GetOrCreateSensor(e.Sensor);
                
                sensorModel.LastActive = e.ServerTime;

                OnEvent?.Invoke(this, e);
            }
        }


        private async void _wyzeDongle_OnRemoveSensor(object sender, WyzeSensor e)
        {

            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                //Need to delete all items starting with the same mac, supports climate sensors and keypad event. 
                var sensorsToRemove = dbContext.Sensors.Where(w => w.MAC.StartsWith(e.MAC)).Select(s => s);
                dbContext.Sensors.RemoveRange(sensorsToRemove);

                //TODO:Verify this deletes events and not model type
                await dbContext.SaveChangesAsync();
                OnRemoveSensor?.Invoke(this, e.MAC);
            }
        }
        
        private async void _wyzeDongle_OnAddSensor(object sender, WyzeSensor e)
        {
            //TODO: Generate sub sensors for climate and keypad. 
            var sensorModel = await GetOrCreateSensor(e);
            OnAddSensor?.Invoke(this, sensorModel);
        }
        private async Task<WyzeSensorModel> GetOrCreateSensor(WyzeSensor Sensor)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                //Gets existing sensor model if it exists.
                var sensorModel = await dbContext.Sensors
                    .Where(p => p.MAC == Sensor.MAC)
                    .FirstOrDefaultAsync();
                if (sensorModel != null)
                    return sensorModel;

                sensorModel = new WyzeSensorModel()
                {
                    Alias = "",
                    Description = "",
                    LastActive = DateTime.Now,
                    MAC = Sensor.MAC,
                    SensorType = (int)Sensor.Type
                };
                await dbContext.Sensors.AddAsync(sensorModel);
                return sensorModel;
            }
        }


        public void RequestRefreshSensorListAsync()
        {
             _wyzeDongle.RequestRefreshSensorListAsync();
        }

        public void SetLEDOff()
        {
            _wyzeDongle.SetLedAsync(false);
        }

        public void SetLEDOn()
        {
            _wyzeDongle.SetLedAsync(true);
        }

        public async Task StartScanAsync(int Timeout = 60 * 1000)
        {
            _logger.LogTrace("[StartScanAsync] Called");
            await _wyzeDongle.StartScanAsync(Timeout);
        }

        public void Stop()
        {
            _wyzeDongle.Stop();
        }

        public async Task StopScanAsync()
        {
            _logger.LogTrace("[StartScanAsync] Called");
            await _wyzeDongle.StopScanAsync();
        }

        public async Task<WyzeSensorModel[]> GetSensorAsync()
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var query = dbContext.Sensors;
                _logger.LogInformation(query.ToQueryString());
                return await query.ToArrayAsync();
            }
        }

        public async Task RequestDeleteSensor(string MAC)
        {
            await _wyzeDongle.DeleteSensorAsync(MAC);
        }


        public WyzeDongleState GetDongleState()
        {
            return _wyzeDongle.GetDongleState();
        }


    }
}
