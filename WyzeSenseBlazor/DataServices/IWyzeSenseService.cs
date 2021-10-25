using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WyzeSenseCore;
using WyzeSenseBlazor.DataStorage.Models;

namespace WyzeSenseBlazor.DataServices
{
    public interface IWyzeSenseService 
    {
        event EventHandler<WyzeSensorModel> OnAddSensor;
        event EventHandler<string> OnRemoveSensor;
        event EventHandler<WyzeSenseEvent> OnEvent;
        event EventHandler<WyzeDongleState> OnDongleStateChange;
        event EventHandler<string> OnFailStart;

        bool Running { get; }

        void Stop();
        void SetLEDOn();
        void SetLEDOff();
        Task StartScanAsync(int Timeout = 60 * 1000);
        Task StopScanAsync();
        Task RequestDeleteSensor(string MAC);
        void RequestRefreshSensorListAsync();
        Task<WyzeSensorModel[]> GetSensorAsync();
        WyzeDongleState GetDongleState();

        Task SetAlias(string MAC, string Alias);
        Task SetDescription(string MAC, string Description);
    }
}
