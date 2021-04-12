using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WyzeSenseCore;

namespace WyzeSenseBlazor.Data
{
    public class WyzeSenseService : IWyzeSenseService
    {
        private readonly WyzeSenseCore.IWyzeDongle _wyzeDongle;
        public WyzeSenseService(WyzeSenseCore.IWyzeDongle wyzeDongle)
        {
            Console.WriteLine("New wyzesenseservice");
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

        private void _wyzeDongle_OnDongleStateChange(object sender, WyzeDongleState e)
        {
            this.OnDongleStateChange?.Invoke(this, e);
        }

        private void _wyzeDongle_OnSensorAlarm(object sender, WyzeSenseEvent e)
        {
            Console.WriteLine("wyzesenseservice onsensoralarm");
            this.OnSensorAlarm?.Invoke(this, e);
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
