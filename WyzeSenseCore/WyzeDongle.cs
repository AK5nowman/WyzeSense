using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace WyzeSenseCore
{
    public sealed class WyzeDongle : IWyzeDongle
    {
        private const int CMD_TIMEOUT = 5000;

        private ByteBuffer dataBuffer;

        private ManualResetEvent waitHandle = new ManualResetEvent(false);

        private CancellationToken token;
        private CancellationTokenSource dongleTokenSource;
        private CancellationTokenSource dongleScanTokenSource;

        private DataProcessor dataProcessor;
        
        private string donglePath;
        
        private FileStream dongleRead;
        private FileStream dongleWrite;

        private WyzeDongleState dongleState;
        private bool commandedLEDState;

        private Dictionary<string, WyzeSensor> sensors;
        private Dictionary<string, WyzeSensor> tempScanSensors;
        private int expectedSensorCount;
        private int actualSensorCount;

        public event EventHandler<WyzeSensor> OnAddSensor;
        public event EventHandler<WyzeSensor> OnRemoveSensor;
        public event EventHandler<WyzeSenseEvent> OnSensorAlarm;
        public event EventHandler<WyzeDongleState> OnDongleStateChange;


        

        public WyzeDongle(string devicePath, 
            List<WyzeSensor> KnownSensors = null, 
            CancellationToken Token = default(CancellationToken))
        {
            donglePath = devicePath;
            sensors = new();

            if(KnownSensors != null)
                foreach (var knownSensor in KnownSensors)
                    sensors.TryAdd(knownSensor.MAC, knownSensor);

            dongleState = new();
            dongleTokenSource = new();
            dongleScanTokenSource = new();
            token = CancellationTokenSource.CreateLinkedTokenSource(dongleTokenSource.Token, Token).Token;
            dataProcessor = new(token, token);
            dataBuffer = new(0x80, MaxCapacity: 1024);
        }
        public void Stop()
        {
            dongleTokenSource.Cancel();

            dongleWrite.Close();
            dongleWrite.Dispose();

            dongleRead.Close();
            dongleRead.Dispose();
        }
        public async Task StartAsync()
        {
            Logger.Trace($"[Dongle][StartAsync] Opening USB Device {donglePath}");
            try
            {
                dongleRead = new FileStream(donglePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, true);
                dongleWrite = new FileStream(donglePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, true);
            }
            catch (Exception e)
            {
                Logger.Error($"[Dongle][StartAsync] {e.ToString()}");
                return;
            }
            Logger.Debug("[Dongle][StartAsync] USB Device opened");

            Task process = UsbProcessingAsync();
            
            Logger.Trace("[Dongle][StartAsync] Requesting Device Type");
            await this.WriteCommandAsync(BasePacket.RequestDeviceType());

            //TODO: Research this feature to better understand what its doing.
            Logger.Trace("[Dongle][StartAsync] Requesting Enr");
            byte[] pack = new byte[16];
            for (int i = 0; i < pack.Length; i++)
                pack[i] = 30;
            await this.WriteCommandAsync(BasePacket.RequestEnr(pack));

            Logger.Trace("[Dongle][StartAsync] Requesting MAC");
            await this.WriteCommandAsync(BasePacket.RequestMAC());

            Logger.Trace("[Dongle][StartAsync] Requesting Version");
            await this.WriteCommandAsync(BasePacket.RequestVersion());

            Logger.Trace("[Dongle][StartAsync] Finishing Auth");
            await this.WriteCommandAsync(BasePacket.FinishAuth());

            //Request Sensor List
            RequestRefreshSensorListAsync();

            await process;
        }
        public async void SetLedAsync(bool On)
        {
            Logger.Debug($"[Dongle][SetLedAsync] Setting LED to: {On}");
            commandedLEDState = On;
            if (On)
                await this.WriteCommandAsync(BasePacket.SetLightOn());
            else
                await this.WriteCommandAsync(BasePacket.SetLightOff());
        }
        public async void StartScanAsync(int Timeout = 60 * 1000)
        {
            Logger.Debug($"[Dongle][StartScanAsync] Starting Inclusion for {Timeout / 1000} seconds");
            await this.WriteCommandAsync(BasePacket.RequestEnableScan());
            try
            {
                await Task.Delay(Timeout, dongleScanTokenSource.Token);
                await StopScanAsync();
            }
            catch (TaskCanceledException tce) 
            {
                dongleScanTokenSource = new();
            }
        }
        public async Task StopScanAsync()
        {
            Logger.Debug($"[Dongle][StopScanAsync] Stopping Inclusion Scan");
            await this.WriteCommandAsync(BasePacket.RequestDisableScan());
            dongleScanTokenSource.Cancel();
        }
        public async void RequestRefreshSensorListAsync()
        {
            Logger.Debug($"[Dongle][RequestRefreshSensorListAsync] Sending Sensor Count Request");
            await this.WriteCommandAsync(BasePacket.RequestSensorCount());
        }

        private async Task UsbProcessingAsync()
        {
            Logger.Trace("[Dongle][UsbProcessingAsync] Beginning to process USB data");
            Memory<byte> dataReadBuffer = new byte[0x40];
            Memory<byte> headerBuffer = new byte[5];
            while (!token.IsCancellationRequested)
            {
                while (dataBuffer.Peek(headerBuffer.Span))
                {
                    Logger.Trace($"[Dongle][UsbProcessingAsync] dataBuffer contains enough bytes for header {dataBuffer.Size}");
                    ushort magic = BitConverter.ToUInt16(headerBuffer.Slice(0, 2).Span);
                    if (magic != 0xAA55)
                    {
                        Logger.Error($"[Dongle][UsbProcessingAsync] Unexpected header bytes {magic:X4}");
                        dataBuffer.Burn(1);
                    }
                    if (headerBuffer.Span[4] == 0xff)//If this is the ack packet
                    {
                        dataBuffer.Burn(7);
                        Logger.Trace($"[Dongle][UsbProcessingAsync] Received dongle ack packet burning 7 bytes, {dataBuffer.Size} remain");
                        continue;
                    }

                    byte dataLen = headerBuffer.Span[3];

                    if (dataBuffer.Size >= (dataLen + headerBuffer.Length - 1))
                    {
                        //A complete packet has been received
                        Memory<byte> dataPack = new Memory<byte>(new byte[dataLen + headerBuffer.Length - 1]);
                        dataBuffer.Dequeue(dataPack.Span);


                        if (dataPack.Span[2] == (byte)Command.CommandTypes.TYPE_ASYNC)
                        {
                            await this.WriteAsync(BasePacket.AckPacket(dataPack.Span[4]));
                            Logger.Trace($"[Dongle][UsbProcessingAsync] Acknowledging packet type 0x{dataPack.Span[4]:X2}");
                        }
                        dataReceived(dataPack.Span);
                    }
                    else
                    {
                        Logger.Debug($"[Dongle][UsbProcessingAsync] Incomplete packet received {dataBuffer.Size}/{dataLen + headerBuffer.Length - 1}");
                        break;
                    }
                }

                Logger.Trace("[Dongle][UsbProcessingAsync] Preparing to receive bytes");
                int result = -1;
                try
                {
                    result = await dongleRead.ReadAsync(dataReadBuffer, token);
                }
                catch (TaskCanceledException tce)
                {
                    Logger.Trace("[Dongle][UsbProcessingAsync] ReadAsync was cancelled");
                    continue;
                }

                if (result == 0)
                {
                    Logger.Error($"[Dongle][UsbProcessingAsync] End of file stream reached");
                    break;
                }
                else if (result >= 0)
                {
                    Logger.Trace($"[Dongle][UsbProcessingAsync] {dataReadBuffer.Span[0]} Bytes Read Raw:  {DataToString(dataReadBuffer.Slice(1, dataReadBuffer.Span[0]).Span)}");
                    dataBuffer.Queue(dataReadBuffer.Slice(1, dataReadBuffer.Span[0]).Span);
                    Logger.Trace("[Dongle][UsbProcessingAsync] Finished processing received bytes");

                }
            }

            Logger.Trace("[Dongle][UsbProcessingAsync] Exiting Processing loop");
        }

        private async Task WriteCommandAsync(BasePacket Pack)
        {
            waitHandle.Reset();
            await this.WriteAsync(Pack);
            if (!waitHandle.WaitOne(CMD_TIMEOUT))
                Logger.Debug("[Dongle][WriteCommandAsync] Command Timed out - did not receive response in alloted time");
            else
                Logger.Trace("[Dongle][WriteCommandAsync] Successfully Wrote command");
        }
        private async Task WriteAsync(BasePacket Pack)
        {
            byte[] data = Pack.Write();
            byte[] preppedData = new byte[data.Length + 1];
            preppedData[0] = (byte)data.Length;
            Array.Copy(data, 0, preppedData, 1, data.Length);
            try
            {
                await dongleWrite.WriteAsync(preppedData, 0, preppedData.Length);
                await dongleWrite.FlushAsync();
                Logger.Trace($"[Dongle][WriteAsync] Successfully wrote {preppedData.Length} bytes: {DataToString(preppedData)}");
            }
            catch (Exception e)
            {
                Logger.Error($"[Dongle][WriteAsync] {e.ToString()}");
            }

        }

        private void refreshSensors()
        {
            List<WyzeSensor> toRem = new List<WyzeSensor>();
            //Look for existing sensors that aren't actually bound
            foreach (var sensor in sensors.Values)
            {
                if (!tempScanSensors.ContainsKey(sensor.MAC))
                    toRem.Add(sensor);
            }
            foreach (var sensor in toRem)
            {
                sensors.Remove(sensor.MAC);
                if (OnRemoveSensor != null)
                    this.dataProcessor.Queue(() => OnRemoveSensor.Invoke(this,sensor));
            }

            //Add sensors that don't already exist
            foreach (var sensor in tempScanSensors.Values)
            {
                if (!sensors.ContainsKey(sensor.MAC))
                {
                    sensors.Add(sensor.MAC, sensor);
                    if (OnAddSensor != null)
                        this.dataProcessor.Queue(() => OnAddSensor.Invoke(this,sensor));
                }
            }
        }

        private string DataToString(ReadOnlySpan<byte> data)
        {
            string byteString = "";
            for (int i = 0; i < data.Length; i++)
            {
                byteString += string.Format("{0:X2} ", data[i]);
            }
            return byteString.TrimEnd(' ');
        }

        private void dataReceived(ReadOnlySpan<byte> Data)
        {
            Logger.Trace($"[Dongle][dataReceived] {Data.Length} Bytes received: {DataToString(Data)}");

            switch ((Command.CommandIDs)Data[4])
            {
                case Command.CommandIDs.NotifyEventLog:
                    //timestamp is Slice(5,8);
                    eventReceived(Data.Slice(14, Data[13] + 1));
                    break;
                case Command.CommandIDs.NotifySensorAlarm:
                    WyzeSenseEvent wyzeevent = new(Data);
                    if (wyzeevent.EventType == WyzeEventType.Alarm)
                    {
                        if (OnSensorAlarm != null)
                        {
                            this.dataProcessor.Queue(() => OnSensorAlarm.Invoke(this,wyzeevent));
                        }
                    }
                    else
                        Logger.Debug($"[Dongle][dataReceived] Unhandled SensorAlarm {wyzeevent.EventType}\r\n\t{DataToString(Data)}");
                    break;
                case Command.CommandIDs.RequestSyncTime:
                    Logger.Debug("[Dongle][dataReceived] Dongle is requesting time");
                    break;
                case Command.CommandIDs.VerifySensorResp:
                    Logger.Debug("[Dongle][dataReceived] Verify Sensor Resp Ack");
                    break;
                case Command.CommandIDs.StartStopScanResp:
                case Command.CommandIDs.AuthResp:
                case Command.CommandIDs.SetLEDResp:
                case Command.CommandIDs.GetDeviceTypeResp:
                case Command.CommandIDs.RequestDongleVersionResp:
                case Command.CommandIDs.RequestEnrResp:
                case Command.CommandIDs.RequestMACResp:
                case Command.CommandIDs.GetSensorCountResp:
                case Command.CommandIDs.GetSensorListResp:
                    this.commandCallback(Data);
                    break;
                default:
                    Logger.Debug($"[Dongle][dataReceived] Command callback not assigned {(Command.CommandIDs)Data[4]} (Hex={string.Format("{0:X2}", Data[4])})\r\n\t{DataToString(Data)}");
                    break;

            }
        }
        private void eventReceived(ReadOnlySpan<byte> Data)
        {
            switch (Data[0])
            {
                case 0x14:
                    Logger.Debug($"[Dongle][eventReceived] Dongle Auth State: {Data[1]}");
                    this.dongleState.AuthState = Data[1];
                    if (this.OnDongleStateChange != null)
                        this.dataProcessor.Queue(() => this.OnDongleStateChange.Invoke(this, this.dongleState));
                    break;
                case 0x1C:
                    Logger.Debug($"[Dongle][eventReceived] Dongle Scan State Set: {Data[1]}");
                    this.dongleState.IsInclusive = Data[1] == 1 ? true : false;
                    if (this.OnDongleStateChange != null)
                        this.dataProcessor.Queue(() => this.OnDongleStateChange.Invoke(this, this.dongleState));
                    break;
                case 0xA2:
                    string mac = ASCIIEncoding.ASCII.GetString(Data.Slice(1, 8));
                    ushort eventID = BinaryPrimitives.ReadUInt16BigEndian(Data.Slice(11, 2));
                    Logger.Debug($"[Dongle][eventReceived] Event - {mac} SensorType:{(WyzeSensorType)Data[9]} State:{Data[10]} EventNumber:{eventID}");
                    break;
                default:
                    Logger.Error($"[Dongle][eventReceived] Unknown event ID: {string.Format("{0:X2}", Data[0])}\r\n\t{DataToString(Data)}");
                    break;
            }
        }
        private void commandCallback(ReadOnlySpan<byte> Data)
        {
            Logger.Trace("[Dongle][commandCallback] Receiving command response");

            Command.CommandIDs cmdID = (Command.CommandIDs)Data[4];
            switch (cmdID)
            {
                case Command.CommandIDs.GetSensorCountResp:
                    expectedSensorCount = Data[5];
                    Logger.Debug($"[Dongle][commandCallback] There are {expectedSensorCount} sensor bound to this dongle");
                    this.tempScanSensors = new Dictionary<string, WyzeSensor>();
                    actualSensorCount = 0;
                    this.WriteCommandAsync(BasePacket.RequestSensorList((byte)expectedSensorCount));
                    break;
                case Command.CommandIDs.GetSensorListResp:
                    Logger.Debug($"[Dongle][commandCallback] GetSensorResp");
                    WyzeSensor sensor = new WyzeSensor(Data);
                    tempScanSensors.Add(sensor.MAC, sensor);
                    actualSensorCount++;
                    if (actualSensorCount == expectedSensorCount)
                        refreshSensors();
                    break;
                case Command.CommandIDs.SetLEDResp:
                    if (Data[5] == 0xff) this.dongleState.LEDState = commandedLEDState;
                    Logger.Debug($"[Dongle][commandCallback] Dongle LED Feedback: {this.dongleState.LEDState}");
                    if (OnDongleStateChange != null)
                        this.dataProcessor.Queue(() => OnDongleStateChange.Invoke(this,this.dongleState));
                    break;
                case Command.CommandIDs.GetDeviceTypeResp:
                    this.dongleState.DeviceType = Data[5];
                    Logger.Debug($"[Dongle][commandCallback] Dongle Device Type: {this.dongleState.DeviceType}");
                    break;
                case Command.CommandIDs.RequestDongleVersionResp:
                    this.dongleState.Version = ASCIIEncoding.ASCII.GetString(Data.Slice(5, (Data[3] - 3)));
                    Logger.Debug($"[Dongle][commandCallback] Dongle Version: {this.dongleState.Version}");
                    break;
                case Command.CommandIDs.RequestEnrResp:
                    this.dongleState.ENR = Data.Slice(5, (Data[3] - 3)).ToArray();
                    Logger.Debug($"[Dongle][commandCallback] Dongle ENR: {this.DataToString(this.dongleState.ENR)}");
                    break;
                case Command.CommandIDs.RequestMACResp:
                    this.dongleState.MAC = ASCIIEncoding.ASCII.GetString(Data.Slice(5, (Data[3] - 3)));
                    Logger.Debug($"[Dongle][commandCallback] Dongle MAC: {this.dongleState.MAC}");
                    break;
                case Command.CommandIDs.AuthResp:
                    Logger.Debug("[Dongle][commandCallback] Dongle Auth Resp");
                    break;
                case Command.CommandIDs.StartStopScanResp:
                    Logger.Debug("[Dongle][commandCallback] Start/Stop Scan Resp");
                    break;
                default:
                    Logger.Debug($"[Dongle][commandCallback] Unknown Command ID {cmdID}\r\n\t{ DataToString(Data)}");
                    break;
            }

            this.waitHandle.Set();
        }

        public Task<WyzeSensor[]> GetSensorAsync()
        {
            return Task.FromResult(sensors.Select(index => index.Value).ToArray());
        }

        public WyzeDongleState GetDongleState()
        {
            return this.dongleState;
        }
    }
}
