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

namespace WyzeSenseBlazor.DataServices
{
    public class WyzeSenseService : BackgroundService, IWyzeSenseService
    {
        private readonly ILogger _logger;
        private readonly WyzeSenseCore.IWyzeDongle _wyzeDongle;
        private readonly IDbContextFactory<WyzeDbContext> _dbContextFactory;

        private Channel<string> configChannel;

        private string devicePath;

        private Task processTask;

        private WyzeDongleState dongleState;
        public WyzeSenseService(ILogger<WyzeSenseService> logger, WyzeSenseCore.IWyzeDongle wyzeDongle, IDbContextFactory<DatabaseProvider.WyzeDbContext> dbContextFactory)
        {
            _logger = logger;
            _logger.LogInformation($"Creating WyzeSenseService");

            _dbContextFactory = dbContextFactory;
            _wyzeDongle = wyzeDongle;

            configChannel = Channel.CreateBounded<string>(capacity: 5);

            //Lets read config from DB, if it exists. 
            using (var dbContext = dbContextFactory.CreateDbContext())
            {
                dbContext.Database.EnsureCreated();
                var usbPair = dbContext.Configuation.Single(c => c.Setting == "usbPath");
                if (usbPair == null)
                    _logger.LogInformation($"No valid usbPath device defined in database");
                else
                    devicePath = usbPair.Value;
            }
            _wyzeDongle.OnAddSensor += _wyzeDongle_OnAddSensor;
            _wyzeDongle.OnRemoveSensor += _wyzeDongle_OnRemoveSensor;
            _wyzeDongle.OnSensorAlarm += _wyzeDongle_OnSensorAlarm;
            _wyzeDongle.OnDongleStateChange += _wyzeDongle_OnDongleStateChange;
        }

        public event EventHandler<WyzeSensorModel> OnAddSensor;
        public event EventHandler<string> OnRemoveSensor;
        public event EventHandler<WyzeEventModel> OnSensorAlarm;
        public event EventHandler<WyzeDongleState> OnDongleStateChange;
        public event EventHandler<string> OnFailStart;

        public bool Running { get=> running; }
        private bool running = false;
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
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

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _wyzeDongle.Stop();
            return Task.CompletedTask;
        }

        private void _wyzeDongle_OnDongleStateChange(object sender, WyzeDongleState e)
        {
            dongleState = e;
            this.OnDongleStateChange?.Invoke(this, e);
        }

        private async void _wyzeDongle_OnSensorAlarm(object sender, WyzeSenseEvent e)
        {

            using (var dbContext = _dbContextFactory.CreateDbContext()) {


                //Lets load up related data
                //TODO: Explore different options
                var sensorModel = await GetOrCreateSensor(e.MAC, (int)e.Sensor);
                var eventTypeModel = await dbContext.EventTypes
                    .Where(p => p.Id == (int)e.EventType).FirstOrDefaultAsync();
                var stateModel = await dbContext.SensorStates
                    .Where(p => p.SensorType == sensorModel.SensorType && p.State == e.State).FirstOrDefaultAsync();



                if (eventTypeModel == null)
                {
                    eventTypeModel = new WyzeEventTypeModel()
                    {
                        Id = (int)e.EventType,
                        Type = "Unknown"
                    };
                    //TODO: Do i need this call?
                    //await dbContext.EventTypes.AddAsync(eventTypeModel);
                }

                var eventdbModel = new WyzeEventModel()
                {
                    EventId = e.EventNumber,
                    Battery = e.BatteryLevel,
                    Signal = e.SignalStrength,
                    Time = e.ServerTime,
                };
                eventdbModel.EventType = eventTypeModel;
                eventdbModel.Sensor = sensorModel;
                eventdbModel.State = stateModel;
                eventdbModel.Sensor.LastActive = e.ServerTime;

                await dbContext.Events.AddAsync(eventdbModel);
                await dbContext.SaveChangesAsync();

                OnSensorAlarm?.Invoke(this, eventdbModel);
            }
        }

        private async void _wyzeDongle_OnRemoveSensor(object sender, WyzeSensor e)
        {

            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                dbContext.Sensors.Remove(new WyzeSensorModel()
                {
                    MAC = e.MAC
                });
                //TODO:Verify this deletes events and not model type
                await dbContext.SaveChangesAsync();
                OnRemoveSensor?.Invoke(this, e.MAC);
            }
        }
        
        private async void _wyzeDongle_OnAddSensor(object sender, WyzeSensor e)
        {
            var sensorModel = await GetOrCreateSensor(e.MAC, (int)e.Type);
            OnAddSensor?.Invoke(this, sensorModel);
        }

        private async Task<WyzeSensorModel> GetOrCreateSensor(string MAC, int SensorType)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var sensorModel = await dbContext.Sensors
                    .Include(p => p.SensorType)
                    .Where(p => p.MAC == MAC)
                    .FirstOrDefaultAsync();

                if (sensorModel == null)
                {
                    //Sensor is not in the database, need to enter a new record
                    _logger.LogInformation($"Adding a new sensor to database {MAC}");
                    sensorModel = new WyzeSensorModel()
                    {
                        MAC = MAC,
                        Alias = "",
                        Description = "",
                        LastActive = DateTime.Now,
                    };
                    var sensorType = sensorModel.SensorType;
                    if (sensorType == null)
                    {
                        _logger.LogInformation($"Creating a new sensor type: {SensorType}");
                        //Lets create  a new sensor type with a generic name.
                        WyzeSensorTypeModel newSensorModel = new()
                        {
                            Id = SensorType,
                            Type = "Unknown",
                            States = new List<WyzeSensorStateModel>()
                            {
                                new WyzeSensorStateModel{ SensorTypeId = SensorType, State = true, Type = "One" },
                                new WyzeSensorStateModel{ SensorTypeId = SensorType, State = false, Type = "Zero" },
                            }
                        };
                        sensorType = newSensorModel;
                    }
                    sensorModel.SensorType = sensorType;
                    await dbContext.Sensors.AddAsync(sensorModel);
                    await dbContext.SaveChangesAsync();
                }

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

        //Currently Returns all events with each sensor.
        public async Task<WyzeSensorModel[]> GetSensorAsync()
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var query = dbContext.Sensors
                    .Include(p => p.Events)
                        .ThenInclude(p => p.EventType)
                    .Include(p => p.Events)
                        .ThenInclude(p => p.State)
                    .Include(p => p.SensorType);
                _logger.LogInformation(query.ToQueryString());
                return await query.ToArrayAsync();
            }
        }
        public async Task<WyzeEventModel[]> GetWyzeEventsAsync(string MAC = null, int? count = null, int? page = null)
        {
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                IQueryable<WyzeEventModel> query = dbContext.Events
                    .Include(p => p.EventType)
                    .Include(p => p.State);

                if (!string.IsNullOrEmpty(MAC))
                    query = query.Where(p => p.SensorMAC == MAC);

                query = query.OrderByDescending(p => p.Time);

                if (page != null && count != null)
                    query = query.Skip(page.Value * count.Value);

                if (count != null)
                    query = query.Take(count.Value);

                var events = await query.ToArrayAsync();
                return events;
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
