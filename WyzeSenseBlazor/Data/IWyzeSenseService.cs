using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WyzeSenseCore;

namespace WyzeSenseBlazor.Data
{
    interface IWyzeSenseService
    {
        event EventHandler<WyzeSensor> OnAddSensor;
        event EventHandler<WyzeSensor> OnRemoveSensor;
        event EventHandler<WyzeSenseEvent> OnSensorAlarm;
        event EventHandler<WyzeDongleState> OnDongleStateChange;


        void Stop();
        Task StartAsync();
        void SetLEDOn();
        void SetLEDOff();
        void StartScanAsync(int Timeout);
        Task StopScanAsync();
        void RequestRefreshSensorListAsync();
        Task<WyzeSensor[]> GetSensorAsync();
        WyzeDongleState GetDongleState();
    }
}
