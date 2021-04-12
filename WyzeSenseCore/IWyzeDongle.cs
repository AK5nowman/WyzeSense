using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WyzeSenseCore
{
    public interface IWyzeDongle
    {
        event EventHandler<WyzeSensor> OnAddSensor;
        event EventHandler<WyzeSensor> OnRemoveSensor;
        event EventHandler<WyzeSenseEvent> OnSensorAlarm;
        event EventHandler<WyzeDongleState> OnDongleStateChange;

        void Stop();
        Task StartAsync();
        void SetLedAsync(bool On);
        void StartScanAsync(int Timeout);
        Task StopScanAsync();
        void RequestRefreshSensorListAsync();
        Task<WyzeSensor[]> GetSensorAsync();
        WyzeDongleState GetDongleState();
    }
}
