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

namespace WyzeSenseBlazor.Data
{
    public class WyzeSenseService : IWyzeSenseService, IHostedService
    {
        private readonly ILogger _logger;
        private readonly WyzeSenseCore.IWyzeDongle _wyzeDongle;
        private readonly IDbContextFactory<WyzeDbContext> _dbContextFactory;

        private Channel<string> configChannel;

        private string devicePath;

        private WyzeDongleState dongleState;
        public WyzeSenseService(ILogger<WyzeSenseService> logger, WyzeSenseCore.IWyzeDongle wyzeDongle, IDbContextFactory<DatabaseProvider.WyzeDbContext> dbContextFactory)
        {
            _logger.LogInformation($"Creating WyzeSenseService");
            _logger = logger;
            _dbContextFactory = dbContextFactory;
            _wyzeDongle = wyzeDongle;

            configChannel = Channel.CreateBounded<string>(capacity: 5);

            //Lets read config from DB, if it exists. 
            var dbContext = dbContextFactory.CreateDbContext();
            string usbPath = dbContext.Configuation.Single(c => c.Setting == "usbPath").Value;
            
            if(usbPath == null || usbPath.Length == 0)
                _logger.LogInformation($"No valid usbPath device defined in database");
            else
                devicePath = usbPath;

            _wyzeDongle.OnAddSensor += _wyzeDongle_OnAddSensor;
            _wyzeDongle.OnRemoveSensor += _wyzeDongle_OnRemoveSensor;
            _wyzeDongle.OnSensorAlarm += _wyzeDongle_OnSensorAlarm;
            _wyzeDongle.OnDongleStateChange += _wyzeDongle_OnDongleStateChange;
        }

        public event EventHandler<WyzeSensor> OnAddSensor;
        public event EventHandler<WyzeSensor> OnRemoveSensor;
        public event EventHandler<WyzeSenseEvent> OnSensorAlarm;
        public event EventHandler<WyzeDongleState> OnDongleStateChange;
        public event EventHandler<string> OnFailStart;

        public bool Running { get=> running; }
        private bool running = false;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                //Let us try to start
                if (devicePath == null || !_wyzeDongle.OpenDevice(devicePath))
                {
                    //Listen to channel
                    string inputPath = "";
                    do
                    {
                        if (inputPath != "")
                            OnFailStart?.Invoke(this, $"Failed to open device: {inputPath}");

                        inputPath = await configChannel.Reader.ReadAsync(cancellationToken);
                        _logger.LogDebug($"Received device path from UI: {inputPath}");

                    } while (!cancellationToken.IsCancellationRequested && !_wyzeDongle.OpenDevice(inputPath));
                }

                //At this point we should have succesfully opened.
                running = true;
                await _wyzeDongle.StartAsync(cancellationToken);
                running = false;
            }
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

        private void _wyzeDongle_OnSensorAlarm(object sender, WyzeSenseEvent e)
        {
            this.OnSensorAlarm?.Invoke(this, e);

            using (var dbContext = _dbContextFactory.CreateDbContext()) {

                var eventdbModel = new WyzeEventModel()
                {
                    State = e.State,
                    EventId = e.EventNumber,
                    Battery = e.BatteryLevel,
                    Signal = e.SignalStrength,
                    Time = e.ServerTime,
                };

                //Lets load up related data
                //TODO: Explore different options
                var sensorModel = dbContext.Sensors.Single(s => s.MAC == e.MAC);
                var eventTypeModel = dbContext.EventTypes.Single(et => et.Id == (int)e.EventType);


                if (sensorModel == null)
                {
                    //Sensor is not in the database, need to enter a new record
                    _logger.LogInformation($"Adding a new sensor to database {eventdbModel.SensorMAC}");
                    sensorModel = new WyzeSensorModel()
                    {
                        MAC = e.MAC,
                        Alias = "",
                        Description = "",
                        LastActive = e.ServerTime,
                    };
                    dbContext.Sensors.Add(sensorModel);
                }
                if (sensorModel.SensorType == null)
                {
                    //if sensor type is null, and the senseor type exists, set it, else create unknown sensor type
                    var sensorType = dbContext.SensorTypes.Single(st => st.Id == (int)e.Sensor);
                    if (sensorType == null)
                    {
                        _logger.LogInformation($"Creating a new sensor type: {(int)e.Sensor}");
                        //Lets create  a new sensor type with a generic name.
                        WyzeSensorTypeModel newSensorModel = new()
                        {
                            Id = (int)e.Sensor,
                            Type = "Unknown",
                            States = new List<WyzeSensorStateModel>()
                            {
                                new WyzeSensorStateModel{ SensorTypeId = (int)e.Sensor, State = true, Type = "One" },
                                new WyzeSensorStateModel{ SensorTypeId = (int)e.Sensor, State = false, Type = "Zero" },
                            }
                        };
                        sensorType = newSensorModel;
                        //dbContext.SensorTypes.Add(newSensorModel);
                    }

                    sensorModel.SensorType = sensorType;

                    _logger.LogInformation($"Adding previously unknown sensor type to sensor {eventdbModel.SensorMAC}");
                }

                if(eventTypeModel == null)
                {
                    eventTypeModel = new WyzeEventTypeModel()
                    {
                        Id = (int)e.EventType,
                        Type = "Unknown"
                    };
                    dbContext.EventTypes.Add(eventTypeModel);
                }

                eventdbModel.EventType = eventTypeModel;
                eventdbModel.Sensor = sensorModel;

                dbContext.SaveChanges();

            }
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
