using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WyzeSenseCore
{
    public sealed class WyzeKeyPadPin
    {
        public string MAC;
        public DateTime ServerTime;
        public int BatteryLevel;
        public int SignalStrength;
        public string Pin;
        public ReadOnlyMemory<byte> RawData;

        public WyzeKeyPadPin(ReadOnlySpan<byte> Data)
        {
            RawData = new Memory<byte>(Data.ToArray());

            RawData = new Memory<byte>(Data.ToArray());
            MAC = ASCIIEncoding.ASCII.GetString(Data.Slice(1, 8));
            SignalStrength = Data[Data[0xA] + 0xB];
            BatteryLevel = Data[0xC];
            ServerTime = DateTime.Now;

            ReadOnlySpan<byte> pinRaw = Data.Slice(0xF, Data[0xA] - 6);
            Pin = "";
            foreach (byte b in pinRaw)
                Pin += b.ToString();
        }
        public override string ToString()
        {
            return string.Format("{0} - {1}: Signal: {2} Battery: {3}", MAC, Pin, SignalStrength, BatteryLevel);
        }
    }
}
