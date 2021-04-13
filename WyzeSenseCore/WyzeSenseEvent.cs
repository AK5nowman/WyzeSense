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
        Unknown
    }
    public sealed class WyzeSenseEvent
    {
        public string MAC;
        public DateTime DongleTime;
        public DateTime ServerTime;
        public WyzeSensorType Sensor;
        public bool State;
        public WyzeEventType EventType;
        public ushort EventNumber;
        public int BatteryLevel;
        public int SignalStrength;
        public ReadOnlyMemory<byte> RawData;

        public WyzeSensorState GetState()
        {
            return Sensor switch
            {
                WyzeSensorType.Motion => State ? WyzeSensorState.Active : WyzeSensorState.Inactive,
                WyzeSensorType.Switch => State ? WyzeSensorState.Open : WyzeSensorState.Closed,
                WyzeSensorType.Water => State ? WyzeSensorState.Wet : WyzeSensorState.Dry,
                WyzeSensorType.Unknown => State ? WyzeSensorState.One : WyzeSensorState.Zero,
                _ => State ? WyzeSensorState.One : WyzeSensorState.Zero
            };
            
        }
        public override string ToString()
        {
            return string.Format("{0} - {1}: {2} EventNum: {3} Battery: {4}", MAC, Sensor, State, EventNumber, BatteryLevel);
        }
        public WyzeSenseEvent(ReadOnlySpan<byte> Data)
        {

            //TODO: Figure out formatting
            DongleTime = DateTime.Now;// new DateTime(BinaryPrimitives.ReadInt64LittleEndian(Data.Slice(5, 8)));
            ServerTime = DateTime.Now;
            EventType = (WyzeEventType)Data[13];
            MAC = ASCIIEncoding.ASCII.GetString(Data.Slice(14, 8));
            Sensor = (WyzeSensorType)Data[22];
            BatteryLevel = Data[24];
            State = Data[27] == 1 ? true : false;
            EventNumber = BinaryPrimitives.ReadUInt16BigEndian(Data.Slice(28, 2));
            SignalStrength = Data[30];
            RawData = new Memory<byte>(Data.ToArray());

        }
    }
}
