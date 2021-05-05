using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WyzeSenseCore
{

    public enum WyzeKeyPadState: byte
    {
        Inactive = 0x00,
        Active = 0x01,
        Disarmed = 0x02,
        Home = 0x03,
        Away = 0x04,
        Alarm = 0x05,
        Unknown
    }
    public sealed class WyzeKeyPadEvent
    {
        public string MAC;
        public DateTime ServerTime;
        public int BatteryLevel;
        public int SignalStrength;
        public ReadOnlyMemory<byte> RawData;
        public WyzeKeyPadState State;

        public WyzeKeyPadEvent(ReadOnlySpan<byte> Data)
        {
            RawData = new Memory<byte>(Data.ToArray());
            MAC = ASCIIEncoding.ASCII.GetString(Data.Slice(1, 8));
            SignalStrength = Data[Data[0xA] + 0xB];
            BatteryLevel = Data[0xC];
            ServerTime = DateTime.Now;
            
            State = (WyzeKeyPadState)(Data[0xF] + (Data[0xE] == 0x2 ? 1 : 0)); // Add one if its a button press, otherwise motion.
        }
        public override string ToString()
        {
            return string.Format("{0} - {1}: Signal: {2} Battery: {3}", MAC, State, SignalStrength, BatteryLevel);
        }
    }
}
