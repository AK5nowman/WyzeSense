using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WyzeSenseCore;
using WyzeSenseBlazor.Settings;
using WyzeSenseBlazor.DataStorage;
using WyzeSenseBlazor.DataStorage.Models;
using System.Collections.Generic;

namespace WyzeSenseBlazor.DataServices
{
    public class WyzeSenseService : IHostedService, IWyzeSenseService
    {
        private readonly ILogger _logger;
        private readonly WyzeSenseCore.IWyzeDongle _wyzeDongle;
        private readonly IDataStoreService _dataStore;

        private string devicePath;

        private Task processTask;

        private WyzeDongleState dongleState;
        public WyzeSenseService(ILogger<WyzeSenseService> logger, WyzeSenseCore.IWyzeDongle wyzeDongle, IDataStoreService dataStore)
        {
            _logger = logger;
            _logger.LogInformation($"Creating WyzeSenseService");

            _dataStore = dataStore;
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
            var dbSensor = await GetOrCreateSensor(e.Sensor);
            dbSensor.LastActive = DateTime.Now;
            OnEvent?.Invoke(this, e);
        }


        private async void _wyzeDongle_OnRemoveSensor(object sender, WyzeSensor e)
        {
            if(_dataStore.DataStore.Sensors.Remove(e.MAC))
            {
                OnRemoveSensor?.Invoke(this, e.MAC);
                await _dataStore.Save();
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
            if(_dataStore.DataStore.Sensors.TryGetValue(Sensor.MAC, out var existSensor))
            {
                return existSensor;
            }

            var sensorModel = new WyzeSensorModel()
            {
                Alias = "",
                Description = "",
                MAC = Sensor.MAC,
                SensorType = (int)Sensor.Type,
                LastActive = DateTime.Now
            };
            _dataStore.DataStore.Sensors.TryAdd(sensorModel.MAC, sensorModel);
            await _dataStore.Save();
            return sensorModel;
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
            return _dataStore.DataStore.Sensors.Values.ToArray();
        }

        public async Task RequestDeleteSensor(string MAC)
        {
            await _wyzeDongle.DeleteSensorAsync(MAC);
        }


        public WyzeDongleState GetDongleState()
        {
            return _wyzeDongle.GetDongleState();
        }

        public async Task SetAlias(string MAC, string Alias)
        {
            if (_dataStore.DataStore.Sensors.TryGetValue(MAC, out var sensor))
            {
                sensor.Alias = Alias;
                await _dataStore.Save();
            }
        }

        public async Task SetDescription(string MAC, string Description)
        {
            if (_dataStore.DataStore.Sensors.TryGetValue(MAC, out var sensor))
            {
                sensor.Description = Description;
                await _dataStore.Save();
            }
        }
    }
}
