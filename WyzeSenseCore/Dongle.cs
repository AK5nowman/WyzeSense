using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WyzeSense
{
    public class Dongle
    {
        ByteBuffer dataBuffer;

        private const int CMD_TIMEOUT = 5000;

        private int deviceHandle;

        private Task processingTask;
        CancellationTokenSource tokenSource;
        CancellationToken token;

        private ManualResetEvent waitHandle = new ManualResetEvent(false);
        private Action<byte[]> commandCallback;

        private Action<byte[]> OnDataReceive;

        public byte[] ENR { get; private set; }
        public string MAC { get; private set; }
        public string Version { get; private set; }
        public byte DeviceType { get; private set; }

        //test
        FileStream dongle;

        public Dongle(string devicePath, Action<byte[]> OnDataReceived = null)
        {
            //Setup handlers
            //Open USB 
            Logger.Trace($"[Dongle] Opening USB Device {devicePath}");
            try
            {
                dongle = File.Open(devicePath, FileMode.Open);
            }
            catch (Exception e)
            {
                Logger.Error($"[Dongle][Init] {e.ToString()}");
                return;
            }

            //Successfully opened the usb device.
            Logger.Trace("[Dongle] USB Device opened");

            OnDataReceive = OnDataReceived;

            dataBuffer = new ByteBuffer(0x80, MaxCapacity: 1024);

            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;

            processingTask = new Task(UsbProcessing, token);

            StartProcessing();
        }
        public void StopProcessing()
        {
            Logger.Trace("[Dongle][StopProcessing] Canceling task token");
            tokenSource.Cancel();
        }
        public void StartProcessing()
        {
            processingTask.Start();

            Logger.Trace("[Dongle][StartProcessing] Requesting Device Type");
            this.WriteCommand(BasePacket.RequestDeviceType());

            Logger.Trace("[Dongle][StartProcessing] Requesting Enr");
            this.GetEnr(30);//ASCII for numeral 0

            Logger.Trace("[Dongle][StartProcessing] Requesting MAC");
            this.WriteCommand(BasePacket.RequestMAC());

            Logger.Trace("[Dongle][StartProcessing] Requesting Version");
            this.WriteCommand(BasePacket.RequestVersion());

            Logger.Trace("[Dongle][StartProcessing] Finishing Auth");
            this.WriteCommand(BasePacket.FinishAuth());

        }

        private void UsbProcessing()
        {
            Logger.Trace("[Dongle][UsbProcessing] Beginning to process USB data");
            Span<byte> dataReadBuffer = stackalloc byte[0x40];
            Span<byte> headerBuffer = stackalloc byte[5];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    Logger.Trace("[Dongle][UsbProcessing] Preparing to receive bytes");
                    int result = dongle.Read(dataReadBuffer);
                    if (result == 0)
                    {
                        Logger.Error($"[Dongle][UsbProcessing] End of file stream reached");
                        break;
                    }
                    else if (result >= 0)
                    {
                        Logger.Trace($"[Dongle][UsbProcessing] {dataReadBuffer.Length} Bytes Read Raw:  {DataToString(dataReadBuffer)}");

                        dataBuffer.Queue(dataReadBuffer.Slice(1, dataReadBuffer[0]));

                        while (dataBuffer.Peek(headerBuffer))
                        {
                            Logger.Trace($"[Dongle][UsbProcessing] dataBuffer contains enough bytes for header {dataBuffer.Size}");
                            ushort magic = BitConverter.ToUInt16(headerBuffer.Slice(0, 2));
                            if (magic != 0xAA55)
                            {
                                Logger.Error($"[Dongle][UsbProcessing] Unexpected header bytes {magic:X4}");
                                tokenSource.Cancel();
                                break;
                            }
                            if (headerBuffer[4] == 0xff)//If this is the ack packet
                            {
                                dataBuffer.Burn(7);
                                Logger.Trace($"[Dongle][UsbProcessing] Received dongle ack packet burning 7 bytes, {dataBuffer.Size} remain");
                                continue;
                            }
                            byte dataLen = headerBuffer[3];

                            if (dataBuffer.Size >= (dataLen + headerBuffer.Length - 1))
                            {
                                //A complete packet has been received
                                Span<byte> dataPack = new Span<byte>(new byte[dataLen + headerBuffer.Length - 1]);
                                dataBuffer.Dequeue(dataPack);

                                OnDataReceived(dataPack.ToArray());
                            }
                            else
                            {
                                Logger.Debug($"[Dongle][UsbProcessing] Incomplete packet received {dataBuffer.Size}/{dataLen + headerBuffer.Length - 1}");
                                break;
                            }
                        }
                        Logger.Trace("[Dongle][UsbProcessing] Finished processing received bytes");

                    }
                    Task.Delay(1);
                }
            }
            catch (ThreadAbortException)
            {
                Logger.Trace("[Dongle][UsbProcessing] Processing Thread Aborted");
            }
            Logger.Trace("[Dongle][UsbProcessing] Exiting Processing loop");
        }
        private void OnDataReceived(byte[] Data)
        {
            //Queue up the packet to be handled in the handler.
            string byteString = "";
            for (int i = 0; i < Data.Length; i++)
            {
                byteString += string.Format("{0:X2} ", Data[i]);
            }
            Logger.Trace($"[Dongle][OnDataReceived] {Data.Length} Bytes received: {byteString}");


            if (Data[2] == (byte)Command.CommandTypes.TYPE_ASYNC)
            {
                this.Write(BasePacket.AckPacket(Data[4]));
                Logger.Trace($"[Dongle][OnDataReceived] Acknowledging packet type 0x{Data[4]:X2}");
            }

            switch ((Command.CommandIDs)Data[4])
            {
                case Command.CommandIDs.NotifyEventLog:
                    Logger.Debug("[Dongle][OnDataReceived] Packet  type 0x35 received, not handled");
                    break;
                case Command.CommandIDs.NotifySensorAlarm:
                    this.OnDataReceive?.Invoke(Data);
                    break;
                case Command.CommandIDs.RequestSyncTime:
                    Logger.Debug("[Dongle][OnDataReceived] Dongle is requesting time");
                    break;
                case Command.CommandIDs.VerifySensorResp:
                    Logger.Debug("[Dongle][OnDataReceived] Verify Sensor Resp Ack");
                    break;
                case Command.CommandIDs.AuthResp:
                case Command.CommandIDs.SetLEDResp:
                case Command.CommandIDs.GetDeviceTypeResp:
                case Command.CommandIDs.RequestDongleVersionResp:
                case Command.CommandIDs.RequestEnrResp:
                case Command.CommandIDs.RequestMACResp:
                    this.commandCallback?.Invoke(Data);
                    break;
                default:
                    Logger.Debug($"[Dongle][OnDataReceived] Command callback not assigned {(Command.CommandIDs)Data[4]} (Hex={string.Format("{0:X2}", Data[4])})");
                    break;

            }
        }
        private void OnCommandCallback(byte[] Data)
        {
            Logger.Trace("[Dongle][OnCommandCallback] Receiving command response");
            this.commandCallback = null;

            Command.CommandIDs cmdID = (Command.CommandIDs)Data[4];
            Span<byte> dataSpan = Data;
            switch (cmdID)
            {
                case Command.CommandIDs.GetDeviceTypeResp:
                    this.DeviceType = dataSpan[5];
                    Logger.Debug($"[Dongle][OnCommandCallback] Dongle Device Type: {this.DeviceType}");
                    break;
                case Command.CommandIDs.RequestDongleVersionResp:
                    this.Version = ASCIIEncoding.ASCII.GetString(dataSpan.Slice(5, (dataSpan[3] - 3)));
                    Logger.Debug($"[Dongle][OnCommandCallback] Dongle Version: {this.Version}");
                    break;
                case Command.CommandIDs.RequestEnrResp:
                    this.ENR = dataSpan.Slice(5, (dataSpan[3] - 3)).ToArray();
                    Logger.Debug($"[Dongle][OnCommandCallback] Dongle ENR: {this.DataToString(this.ENR)}");
                    break;
                case Command.CommandIDs.RequestMACResp:
                    this.MAC = ASCIIEncoding.ASCII.GetString(dataSpan.Slice(5, (dataSpan[3] - 3)));
                    Logger.Debug($"[Dongle][OnCommandCallback] Dongle MAC: {this.MAC}");
                    break;
                case Command.CommandIDs.FinishAuth:
                    byte authState = dataSpan[5];
                    if (authState == 0)
                        Logger.Error("[Dongle][OnCommandCallback] Dongle not in authorized state");
                    break;
                default:
                    Logger.Debug($"[Dongle][OnCommandCallback] Unknown Command ID {cmdID}");
                    break;
            }

            this.waitHandle.Set();
        }
        private void WriteCommand(BasePacket Pack)
        {
            waitHandle.Reset();
            this.commandCallback = OnCommandCallback;
            this.Write(Pack);
            if (!waitHandle.WaitOne(CMD_TIMEOUT))
                Logger.Debug("[Dongle][WriteCommand] Command Timed out - did not receive response in alloted time");
        }
        private void Write(BasePacket Pack)
        {

            byte[] data = Pack.Write();
            byte[] preppedData = new byte[data.Length + 1];
            preppedData[0] = (byte)data.Length;
            Array.Copy(data, 0, preppedData, 1, data.Length);
            try
            {
                dongle.Write(preppedData, 0, preppedData.Length);
                dongle.Flush();
                Logger.Trace($"[Dongle][Write] Successfully wrote {preppedData.Length} bytes: {DataToString(preppedData)}");
            }
            catch (Exception e)
            {
                Logger.Error($"[Dongle][Write] {e.ToString()}");
            }

        }

        private void GetEnr(byte rValue)
        {
            byte[] pack = new byte[16];
            for (int i = 0; i < pack.Length; i++)
                pack[i] = rValue;
            this.WriteCommand(BasePacket.RequestEnr(pack));
        }
        public void SetLed(bool On)
        {
            Logger.Debug($"[Dongle][SetLed] Setting LED to: {On}");
            if (On)
                this.WriteCommand(BasePacket.SetLightOn());
            else
                this.WriteCommand(BasePacket.SetLightOff());
        }
        public void List() { }
        public void Stop(object timeout) { }
        public void Scan(object timeout) { }

        public void Delete(object mac) { }

        private string DataToString(ReadOnlySpan<byte> data)
        {
            string byteString = "";
            for (int i = 0; i < data.Length; i++)
            {
                byteString += string.Format("{0:X2} ", data[i]);
            }
            return byteString.TrimEnd(' ');
        }

    }
}
