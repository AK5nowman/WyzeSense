using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WyzeSenseCore
{
    public interface IWyzeDongle
    {
        event EventHandler<WyzeSensor> OnAddSensor;
        event EventHandler<WyzeSensor> OnRemoveSensor;
        event EventHandler<WyzeSenseEvent> OnSensorEvent;
        event EventHandler<WyzeDongleState> OnDongleStateChange;

        void Stop(); 
        bool OpenDevice(string devicePath);
        Task StartAsync(CancellationToken cancellationToken);
        void SetLedAsync(bool On);
        Task StartScanAsync(int Timeout);
        Task StopScanAsync();
        Task DeleteSensorAsync(string MAC);
        Task RequestRefreshSensorListAsync();
        Task<WyzeSensor[]> GetSensorAsync();
        WyzeDongleState GetDongleState();
    }
}
