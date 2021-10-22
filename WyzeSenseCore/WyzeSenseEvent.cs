using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Binary;

namespace WyzeSenseCore
{
    public enum WyzeSensorType : byte
    {
        Switch = 0x01,
        Motion = 0x02,
        Water = 0x03,
        KeyPad = 0x05,
        Climate = 0x07,
        SwitchV2 = 0x0E,
        Motionv2 = 0x0F,
        Unknown
    }
    public enum WyzeSensorState
    {
        Closed,
        Open,
        Active,
        Inactive,
        Wet,
        Dry,
        One,
        Zero
    }
    public enum WyzeEventType : byte
    {
        Alarm = 0xA2,
        Status = 0xA1,
        Climate = 0xE8,
        UserAction = 0x99,
        Unknown
    }
    public sealed class WyzeSenseEvent
    {
        public WyzeSensor Sensor;

        public DateTime ServerTime;

        public WyzeEventType EventType;

        public Dictionary<string, object> Data;

        public override string ToString()
        {
            return string.Format("{0} - {1}: {2}", Sensor.MAC, ServerTime, Sensor.Type);
        }
    }

}
