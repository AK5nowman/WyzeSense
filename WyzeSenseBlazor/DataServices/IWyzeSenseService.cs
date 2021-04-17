using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WyzeSenseCore;
using WyzeSenseBlazor.DatabaseProvider.Models;

namespace WyzeSenseBlazor.Data
{
    interface IWyzeSenseService 
    {
        event EventHandler<WyzeSensorModel> OnAddSensor;
        event EventHandler<string> OnRemoveSensor;
        event EventHandler<WyzeEventModel> OnSensorAlarm;
        event EventHandler<WyzeDongleState> OnDongleStateChange;
        event EventHandler<string> OnFailStart;

        bool Running { get; }

        void Stop();
        void SetLEDOn();
        void SetLEDOff();
        void StartScanAsync(int Timeout);
        Task StopScanAsync();
        Task RequestDeleteSensor(string MAC);
        void RequestRefreshSensorListAsync();
        Task<WyzeSensorModel[]> GetSensorAsync();
        Task<WyzeEventModel[]> GetWyzeEventsAsync(string MAC, int count);
        WyzeDongleState GetDongleState();
    }
}
