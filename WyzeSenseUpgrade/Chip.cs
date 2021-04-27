using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Binary;
using System.Threading;

namespace WyzeSenseUpgrade
{
    internal class Chip
    {
        private CommandInterface commandInterface;

        private int flash_start_addr = 0x00000000;
        private bool has_cmd_set_xosc = false;

        private const int MISC_CONF_1 = 0x500010A0;
        private const int PROTO_MASK_BLE = 0x01;
        private const int PROTO_MASK_IEEE = 0x04;
        private const int PROTO_MASK_BOTH = 0x05;

        private int bootloader_dis_val = 0x00000000;
        private string crc_cmd = "cmdCRC32CC26XX";
        private int page_size = 4096;


        int flashSize;
        int bootloader_address;
        int addr_ieee_address_secondary;

        public Chip(CommandInterface CommandInterface)
        {
            try
            {
                commandInterface = CommandInterface;

                //Write a "null" packet
                commandInterface.cmdConnect();
                if(commandInterface.cmdAutoBaud())
                    Console.WriteLine("[Chip] Succcessfully connected to cc1310");
                else
                    Console.WriteLine("[Chip] Succcessfully connected to cc1310");

                commandInterface.cmdPing();

                const int ICEPICK_DEVICE_ID = 0x50001318;
                const int FCFG_USER_ID = 0x50001294;
                const int PRCM_RAMHWOPT = 0x40082250;
                const int FLASH_SIZE = 0x4003002C;


                var addr_ieee_address_primary = 0x500012F0;
                var ccfg_len = 88;
                var ieee_address_secondary_offset = 0x20;
                var bootloader_dis_offset = 0x30;
                var sram = "Unknown";

                var device_id_buf = commandInterface.cmdMemReadCC26XX(ICEPICK_DEVICE_ID);
                var wafer_id = (((device_id_buf[3] & 0x0F) << 16) + (device_id_buf[2] << 8) + (device_id_buf[1] & 0xF0)) >> 4;
                var pg_rev = (device_id_buf[3] & 0xF0) >> 4;

                var user_id_buf = commandInterface.cmdMemReadCC26XX(FCFG_USER_ID);
                var package = user_id_buf[2] switch
                {
                    0x00 => "4x4mm",
                    0x01 => "5x5mm",
                    0x02 => "7x7mm",
                    0x03 => "Wafer",
                    0x04 => "2.7x2.7",
                    0x05 => "7x7mm Q1",
                    _ => "Unknown"
                };

                var protocols = user_id_buf[1] >> 4;
                string chip_str = "Unknown";
                if (wafer_id == 0xB9BE)
                {
                    chip_str = Identify_cc13xx(pg_rev, protocols);
                }
                else if (wafer_id == 0xBB41)
                {
                    chip_str = Identify_cc13xx(pg_rev, protocols);
                    page_size = 8192;
                }
                else

                    flashSize = commandInterface.cmdMemReadCC26XX(FLASH_SIZE)[0] * page_size;
                bootloader_address = flashSize - ccfg_len + bootloader_dis_offset;
                addr_ieee_address_secondary = flashSize - ccfg_len + ieee_address_secondary_offset;

                int ramphwopt_size = commandInterface.cmdMemReadCC26XX(PRCM_RAMHWOPT)[0] & 3;
                sram = ramphwopt_size switch
                {
                    3 => "20KB",
                    2 => "16KB",
                    _ => "Unknown"
                };

                Span<byte> ieee_addr_p1 = commandInterface.cmdMemReadCC26XX(addr_ieee_address_primary + 4);
                ieee_addr_p1.Reverse();
                Span<byte> ieee_addr_p2 = commandInterface.cmdMemReadCC26XX(addr_ieee_address_primary);
                ieee_addr_p2.Reverse();
                Span<byte> ieee_addr = new byte[ieee_addr_p1.Length + ieee_addr_p2.Length];
                ieee_addr_p1.CopyTo(ieee_addr);
                ieee_addr_p2.CopyTo(ieee_addr.Slice(ieee_addr_p1.Length));

                string ieee_addr_bstr = "";
                foreach (byte b in ieee_addr)
                    ieee_addr_bstr += $"{b:X2} ";
                ieee_addr_bstr.TrimEnd(' ');

                Console.WriteLine($"{chip_str} ({package}): {flashSize >> 10}KB Flash, {sram} SRAM, CCFG.BL_CONFIG at 0x{bootloader_address:X8}");
                Console.WriteLine($"Primary IEEE Address: {ieee_addr_bstr}");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private string Identify_cc13xx(int pg, int protocols)
        {
            string chip_str = "CC1310";
            string pg_str = "";
            if ((protocols & PROTO_MASK_IEEE) == PROTO_MASK_IEEE)
                chip_str = "CC1350";

            if (pg == 0)
                pg_str = "PG1.0";
            else if( pg == 2 || pg == 3)
            {
                byte rev_minor = commandInterface.cmdMemReadCC26XX(MISC_CONF_1)[0];
                if (rev_minor == 0xFF)
                    rev_minor = 0x00;

                pg_str = $"PG2.{rev_minor}";
            }
            return $"{chip_str} {pg_str}";
        }
    }
    internal class CommandInterface
    {
        private FileStream dongleFile;

        private const byte ACK_BYTE = 0xCC;
        private const byte NACK_BYTE = 0x33;

        enum COMMAND : byte
        {
            RET_SUCCESS = 0x40,
            RET_UNKNOWN_CMD = 0x41,
            RET_INVALID_CMD = 0x42,
            RET_INVALID_ADR = 0x43,
            RET_FLASH_FAIL = 0x44
        }
        public CommandInterface(string donglePath)
        {
            dongleFile = new FileStream(donglePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, true);
        }
        public bool cmdPing()
        {
            Console.WriteLine($"[CommandInterface][cmdPing] Sending ping");
            Span<byte> buf = new byte[3];
            buf[0] = 3;
            buf[1] = 0x20;
            buf[2] = 0x20;
            Write(buf);
            if (Wait_for_ack("cmdPing"))
                return CheckLastCmd();
            return false;
        }
        public bool cmdConnect()
        {
            Span<byte> buf = new byte[3];
            buf[0] = 3;
            buf[1] = 0;
            Write(buf);
            return true;
        }
        public bool cmdAutoBaud()
        {
            Span<byte> buf = new byte[2];
            buf[0] = 0x55;
            buf[1] = 0x55;
            dongleFile.Write(buf);
            dongleFile.Flush();
            return Wait_for_ack("cmdAutoBaud");
        }
        public Span<byte> cmdMemReadCC26XX(int addr)
        {
            Console.WriteLine("sending cmdMemReadCC26XX");
            byte cmd = 0x2A;
            byte length = 9;

            Span<byte> buf = new byte[length];
            buf[0] = length;
            buf[1] = CalcChecks(cmd, addr, length);
            buf[2] = cmd;
            BinaryPrimitives.WriteInt32BigEndian(buf.Slice(3), addr);
            buf[7] = (byte)1;
            buf[8] = (byte)1;


            Write(buf);
            Thread.Sleep(5);
            if(Wait_for_ack("cmdMemReadCC26XX"))
            {
                Span<byte> data = ReceivePacket();
                if (CheckLastCmd())
                    return data;
            }
            throw new Exception("Failed to recv ack");
        }

        /// <summary>
        /// Writes the byte[] without appending data length, it's expected that I append from the calling function.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="is_retry"></param>
        private void Write(ReadOnlySpan<byte> data, bool is_retry = false)
        {
            //TODO: DO I NEED TO APPEND LENGTH??
            if (data.Length > 254) throw new Exception("Data exceeds max length");

            Span<byte> buf = new byte[data.Length + 1];
            buf[0] = (byte)data.Length;
            data.CopyTo(buf.Slice(1));

            dongleFile.Write(buf);
            dongleFile.Flush();
            Console.WriteLine($"[Write] Wrote Raw Data: {DataToString(buf)}");
        }
        private byte CalcChecks(byte cmd, int addr, int length)
        {
            //check sum??
            int total = EncodeAddr(addr).Sum<byte>(x => x) + EncodeAddr(length).Sum<byte>(x => x) + cmd;
            return (byte)total;
        }
        private int DecodeAddr(byte b0, byte b1, byte b2, byte b3)
        {
            //big-endian
            return (int)((b3 << 24) | (b2 << 16) | (b1 << 8) | (b0 << 0));
        }
        private byte[] EncodeAddr(int addr)
        {
            //big-endian
            byte[] buf = new byte[4];
            buf[0] = (byte)(addr >> 24);
            buf[1] = (byte)(addr >> 16);
            buf[2] = (byte)(addr >> 8);
            buf[3] = (byte)(addr >> 0);
            return buf;
        }

        /// <summary>
        /// Returns true if there is additional data to be read.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="TimeoutSeconds"></param>
        /// <returns></returns>
        private bool Wait_for_ack(string scope, int TimeoutSeconds = 1)
        {
            //TODO:Track additional bytes?
            Span<byte> readbuff = new byte[3];
            try
            {
                Console.WriteLine($"Waiting on Ack for {scope}");


                int readSize = dongleFile.Read(readbuff);

                Console.WriteLine($"ReadSize: {readSize} : Raw - {DataToString(readbuff)}");


                //Got a good ack
                if (readbuff[2] == ACK_BYTE)
                {
                    Console.WriteLine($"Target replied with ACK for {scope}");
                    return true;
                }
                else if (readbuff[2] == NACK_BYTE)
                {
                    Console.WriteLine($"Target replied with NACK for {scope}");
                    return false;
                }
            }
            catch(Exception e)
            { 
                Console.WriteLine(e.ToString());  
            }
            Console.WriteLine($"Target replied with ({readbuff[0]}) unknown response for {scope}");
            return false;
        }

        public Span<byte> ReceivePacket()
        {
            Span<byte> header = new byte[0x100];
            Console.WriteLine($"[Chip][ReceviePacket] Stream Length: {dongleFile.Length}");
            int readSize = dongleFile.Read(header);
            Console.WriteLine($"[Chip][ReceviePacket] Stream Length: {dongleFile.Length}");
            var dataLen = header[0];
            var checkSum = header[1];

            Console.WriteLine($"[Chip][ReceivePacket] Head Raw: {DataToString(header)}");
            
            Span<byte> data = new byte[dataLen - 2];

            Console.WriteLine($"[Chip][ReceivePacket] Trying to read remaining: {data.Length}");
            
           // dongleFile.Read(data);
            
            Console.WriteLine($"[Chip][ReceivePacket] Read Raw: {DataToString(data)}");
            Console.WriteLine($"[ReceivePacket] Receiving [{data.Length:X2}] bytes");

            byte ourCheckSum = (byte)data.ToArray().Sum<byte>(x => x);
            if(ourCheckSum == checkSum)
            {
                SendAck();
                return data;
            }
            else
            {
                Console.WriteLine($"[ReceivePacket] packet checksum error: expecting [{checkSum:X2}], got [{ourCheckSum:X2}]");
                throw new Exception("Checksum mismatch");
            }
        }

        public void SendAck()
        {
            byte[] buf = new byte[2];
            buf[0] = 0;
            buf[1] = ACK_BYTE;
            Write(buf);
        }
        public void SendNack()
        {
            byte[] buf = new byte[2];
            buf[0] = 0;
            buf[1] = NACK_BYTE;
            Write(buf);
        }

        public bool CheckLastCmd()
        {
            var stat = cmdGetStatus();
            if (stat == null)
                throw new Exception("No response from target on status request (did you disable the bootloader?)");

            if(stat[0] == (byte)COMMAND.RET_SUCCESS)
            {
                Console.WriteLine("[CheckLastCmd] Command Successful");
                return true;
            }
            else
            {
                Console.WriteLine($"[CheckLastCmd] Target Returned {(COMMAND)stat[0]}");
                return false;
            }
        }

        private Span<byte> cmdGetStatus()
        {
            byte cmd = 0x23;
            byte length = 3;

            Span<byte> buf = new byte[length];
            buf[0] = length;
            buf[1] = cmd;
            buf[2] = cmd;
            Write(buf);

            if(Wait_for_ack("cmdGetStatus"))
                return ReceivePacket();
            return null;
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
    }
}
