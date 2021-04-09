using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WyzeSense
{
    public class Command
    {
        public enum CommandTypes : byte
        {
            TYPE_SYNC = 0x43,
            TYPE_ASYNC = 0x53
        }
        public enum CommandIDs: byte
        {
            RequestEnr = 0x02,
            RequestEnrResp = 0x03,
            RequestMAC = 0x04,
            RequestMACResp = 0x05,
            RequestKey = 0x06,
            SetCh554Update = 0x0E,
            UpdateCC1310 = 0x12,
            FinishAuth = 0x14,
            AuthResp = 0x15,
            RequestDongleVersion = 0x16,
            RequestDongleVersionResp = 0x17,
            NotifySensorAlarm = 0x19,
            NotifySensorStart = 0x20,
            StartStopScan = 0x1C,
            GetSensorR1 = 0x21,
            VerifySensor = 0x23,
            VerifySensorResp = 0x24,
            DeleteSensor = 0x25,
            GetDeviceType = 0x27,
            GetDeviceTypeResp = 0x28,
            GetSensorCount = 0x2E,
            GetSensorList = 0x30,
            RequestSyncTime = 0x32,
            NotifyEventLog = 0x35,
            Unk1 = 0x37,
            SetLED = 0x3d,
            SetLEDResp = 0x3e,
            Ack = 0xFF

        }

        public readonly byte CommandType;
        public readonly byte CommandID;

        private Command(CommandTypes CommandType, CommandIDs CommandID)
        {
            this.CommandType = (byte)CommandType;
            this.CommandID = (byte)CommandID;
        }

        public static Command CMD_GET_ENR => new Command(CommandTypes.TYPE_SYNC, CommandIDs.RequestEnr);
        public static Command CMD_GET_MAC => new Command(CommandTypes.TYPE_SYNC, CommandIDs.RequestMAC);
        public static Command CMD_GET_KEY => new Command(CommandTypes.TYPE_SYNC, CommandIDs.RequestKey);
        public static Command CMD_GET_DEVICE_TYPE => new Command(CommandTypes.TYPE_SYNC, CommandIDs.GetDeviceType);
        public static Command CMD_UPDATE_CC1310 => new Command(CommandTypes.TYPE_SYNC, CommandIDs.UpdateCC1310);
        public static Command CMD_SET_CH554_UPGRADE => new Command(CommandTypes.TYPE_SYNC, CommandIDs.SetCh554Update);

        public static Command ASYNC_ACK => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.Ack);


        public static Command CMD_FINISH_AUTH => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.FinishAuth);
        public static Command CMD_GET_DONGLE_VERSION => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.RequestDongleVersion);
        public static Command CMD_START_STOP_SCAN => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.StartStopScan);
        public static Command CMD_GET_SENSOR_R1 => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.GetSensorR1);
        public static Command CMD_VERIFY_SENSOR => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.VerifySensor);
        public static Command CMD_DEL_SENSOR => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.DeleteSensor);
        public static Command CMD_GET_SENSOR_COUNT => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.GetSensorCount);
        public static Command CMD_GET_SENSOR_LIST => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.GetSensorList);


        public static Command NOTIFY_SENSOR_ALARM => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.NotifySensorAlarm);
        public static Command NOTIFY_SENSOR_START => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.NotifySensorStart);
        public static Command NOTIFY_SYNC_TIME => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.RequestSyncTime);
        public static Command NOTIFY_EVENT_LOG => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.NotifyEventLog);
        public static Command CMD_SET_LIGHT => new Command(CommandTypes.TYPE_ASYNC, CommandIDs.SetLED);
    }
    public class BasePacket
    {
        public Command PktCommand { get; set; }
        public byte Length { get; set; }
        public BasePacket(Command Cmd)
        {
            PktCommand = Cmd;
        }
        public virtual byte[] Write()
        {
            //This will append the header and the checksum
            using(BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                byte[] payload = Encode();
                //Write the header.
                writer.Write((ushort)0x55AA);
                writer.Write((byte)PktCommand.CommandType);

                Length = (byte)((payload == null ? 0 : payload.Length) + 3);//Add three to include checksum length + cmd ID
                writer.Write((byte)Length);

                writer.Write((byte)PktCommand.CommandID);

                //Write the payload
                if(payload != null)
                    writer.Write(payload);

                //Write the checksum
                writer.BaseStream.Flush();
                writer.Write(GetChecksum((writer.BaseStream as MemoryStream).ToArray()));

                writer.BaseStream.Flush();
                return (writer.BaseStream as MemoryStream).ToArray();
            }
        }
        public virtual byte[] Encode() { return null; }
        public virtual void Decode() { throw new NotImplementedException(); }

        internal ushort GetChecksum(byte[] data)
        {
            ushort res = (ushort)data.Sum<byte>(x => x);
            return (ushort)((ushort)((res & 0xff) << 8) | ((res >> 8) & 0xff));
        }

        public static BasePacket RequestEnableScan() => new BytePacket(Command.CMD_START_STOP_SCAN, 0x01);
        public static BasePacket RequestDisableScan() => new BytePacket(Command.CMD_START_STOP_SCAN, 0x00);
        public static BasePacket RequestSensorList(byte Amount) => new BytePacket(Command.CMD_GET_SENSOR_LIST, Amount);
        public static BasePacket FinishAuth() => new BytePacket(Command.CMD_FINISH_AUTH, 0xff);
        public static BasePacket AckPacket(byte CmdToAck) => new AckPacket(CmdToAck);
        public static BasePacket RequestDeviceType() => new BasePacket(Command.CMD_GET_DEVICE_TYPE);
        public static BasePacket RequestVersion() => new BasePacket(Command.CMD_GET_DONGLE_VERSION);
        public static BasePacket RequestMAC() => new BasePacket(Command.CMD_GET_MAC);
        public static BasePacket RequestKey() => new BasePacket(Command.CMD_GET_KEY);
        public static BasePacket RequestSensorCount() => new BasePacket(Command.CMD_GET_SENSOR_COUNT);
        public static BasePacket UpdateCC1310() => new BasePacket(Command.CMD_UPDATE_CC1310);
        public static BasePacket UpdateCH554() => new BasePacket(Command.CMD_SET_CH554_UPGRADE);
        public static BasePacket RequestEnr(byte[] RValue)
        {
            if (RValue.Length != 16) { Logger.Error("[Packet][RequestEnr] RValue length != 16"); return null; }
            return new ByteArrayPacket(Command.CMD_GET_ENR, RValue); ;
        }
        public static BasePacket DeleteSensor(string MAC)
        {
            if (MAC.Length != 8) { Logger.Error("[Packet][DeleteSensor] MAC length != 8"); return null; }
            return new ByteArrayPacket(Command.CMD_DEL_SENSOR, ASCIIEncoding.ASCII.GetBytes(MAC));
        }
        public static BasePacket VerifySensor(string MAC)
        {
            if (MAC.Length != 8) { Logger.Error("[Packet][VerifySensor] MAC length != 8"); return null; }
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                writer.Write(MAC);
                writer.Write(new byte[] { 0xff, 0x04 });
                return new ByteArrayPacket(Command.CMD_VERIFY_SENSOR, (writer.BaseStream as MemoryStream).ToArray());
            }
        }
        public static BasePacket RequestSensorR1(string MAC, byte[] RValue)
        {
            if (RValue.Length != 16) { Logger.Error("[Packet][RequestSensorR1] RValue length != 16"); return null; }
            if (MAC.Length != 8) { Logger.Error("[Packet][RequestSensorR1] MAC length != 8"); return null; }
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                writer.Write(MAC);
                writer.Write(RValue);
                return new ByteArrayPacket(Command.CMD_VERIFY_SENSOR, (writer.BaseStream as MemoryStream).ToArray());
            }
        }
        public static BasePacket SyncTimeAck() => new ByteArrayPacket(Command.NOTIFY_SYNC_TIME, BitConverter.GetBytes(DateTime.UtcNow.Ticks));
        public static BasePacket SetLightOn() => new BytePacket(Command.CMD_SET_LIGHT, 0xff);
        public static BasePacket SetLightOff() => new BytePacket(Command.CMD_SET_LIGHT, 0);
    }
    public class BytePacket : BasePacket
    {
        public byte Value { get; set; }
        public BytePacket(Command Cmd, byte Value) : base(Cmd)
        {
            this.Value = Value;
        }
        public override byte[] Encode()
        {
            return new byte[1] { Value };
        }
    }
    public class ByteArrayPacket : BasePacket
    {
        public byte [] Value { get; set; }
        public ByteArrayPacket(Command Cmd, byte [] Value) : base(Cmd)
        {
            this.Value = Value;
        }
        public override byte[] Encode()
        {
            return Value;
        }
    }
    public class AckPacket : BasePacket
    {
        public byte ToAcknowledge { get; set; }
        public AckPacket(byte CmdToAck) : base(Command.ASYNC_ACK)
        {
            ToAcknowledge = CmdToAck;
        }
        public override byte[] Write()
        {
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                //Write the header.
                writer.Write((ushort)0x55AA);
                writer.Write((byte)PktCommand.CommandType);
                writer.Write((byte)ToAcknowledge);
                writer.Write((byte)PktCommand.CommandID);

                //Write the checksum
                writer.BaseStream.Flush();
                writer.Write(GetChecksum((writer.BaseStream as MemoryStream).ToArray()));

                writer.BaseStream.Flush();
                return (writer.BaseStream as MemoryStream).ToArray();
            }
        }
        public override byte[] Encode()
        {
            throw new NotImplementedException();
        }
    }
}
