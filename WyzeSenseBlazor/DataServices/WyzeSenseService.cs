using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WyzeSenseBlazor.DatabaseProvider;
using WyzeSenseBlazor.DatabaseProvider.Models;
using WyzeSenseCore;

namespace WyzeSenseBlazor.Data
{
    public class WyzeSenseService : IWyzeSenseService, IHostedService
    {
        private readonly ILogger _logger;
        private readonly WyzeSenseCore.IWyzeDongle _wyzeDongle;
        private readonly IDbContextFactory<WyzeDbContext> _dbContextFactory;
        public WyzeSenseService(ILogger logger, WyzeSenseCore.IWyzeDongle wyzeDongle, IDbContextFactory<DatabaseProvider.WyzeDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;

            _wyzeDongle = wyzeDongle;
            _wyzeDongle.OnAddSensor += _wyzeDongle_OnAddSensor;
            _wyzeDongle.OnRemoveSensor += _wyzeDongle_OnRemoveSensor;
            _wyzeDongle.OnSensorAlarm += _wyzeDongle_OnSensorAlarm;
            _wyzeDongle.OnDongleStateChange += _wyzeDongle_OnDongleStateChange;
        }

        public event EventHandler<WyzeSensor> OnAddSensor;
        public event EventHandler<WyzeSensor> OnRemoveSensor;
        public event EventHandler<WyzeSenseEvent> OnSensorAlarm;
        public event EventHandler<WyzeDongleState> OnDongleStateChange;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private void _wyzeDongle_OnDongleStateChange(object sender, WyzeDongleState e)
        {
            this.OnDongleStateChange?.Invoke(this, e);
        }

        private void _wyzeDongle_OnSensorAlarm(object sender, WyzeSenseEvent e)
        {
            this.OnSensorAlarm?.Invoke(this, e);

            var dbContext = _dbContextFactory.CreateDbContext();

            var eventdbModel = new WyzeEventModel()
            {
                SensorMAC = e.MAC,
                State = e.State,
                EventId = e.EventNumber,
                Battery = e.BatteryLevel,
                Signal = e.SignalStrength,
                Time = e.ServerTime,
                EventTypeId = (int)e.EventType
            };

            dbContext.Entry(eventdbModel)
                .Reference(x => x.Sensor)
                .Load();
            if(eventdbModel.Sensor == null)
            {
                //Sensor is not in the database, need to enter a new record
            }
            else if(eventdbModel.Sensor.SensorType == null)
            {
                //if it is null, and the sensortype is not set, set it with event info

            }
            var eventType = dbContext.EventTypes.Single(et => et.Id == (int)e.EventType);
        }

        private void _wyzeDongle_OnRemoveSensor(object sender, WyzeSensor e)
        {
            this.OnRemoveSensor?.Invoke(this, e);
        }

        private void _wyzeDongle_OnAddSensor(object sender, WyzeSensor e)
        {
            this.OnAddSensor?.Invoke(this, e);
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

        public async Task StartAsync()
        {
            await _wyzeDongle.StartAsync();
        }

        public void StartScanAsync(int Timeout)
        {
            _wyzeDongle.StartScanAsync(60 * 1000);
        }

        public void Stop()
        {
            _wyzeDongle.Stop();
        }

        public async Task StopScanAsync()
        {
            await _wyzeDongle.StopScanAsync();
        }

        public Task<WyzeSensor[]> GetSensorAsync()
        {
            return _wyzeDongle.GetSensorAsync();
        }

        public WyzeDongleState GetDongleState()
        {
            return _wyzeDongle.GetDongleState();
        }
    }
}
