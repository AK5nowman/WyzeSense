using System;
using System.IO;
using System.Threading.Tasks;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace WyzeSenseUpgrade
{
    class Program
    {
        const int SBL_CC2650_MAX_MEMREAD_WORDS = 63;
        const int SBL_CC2650_ACCESS_WIDTH_32B = 1;
        const uint MAX_PACK_SIZE = 0x3A;

        static FileStream dongleStream;

        static uint DeviceID;
        static uint DeviceVersion;

        static async Task Main(string[] args)
        {
            List<string> argList = new List<string>(args);
            if (args.Length > 0)
            {
                if (File.Exists(args[0]))
                {
                    using (dongleStream = new FileStream(args[0], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, true))
                    {
                        Console.WriteLine("Requesting Upgrade Mode ");
                        Memory<byte> requestcc13010Up = new byte[] { 0x07, 0xAA, 0x55, 0x43, 0x03, 0x12, 0x01, 0x57 };
                        dongleStream.Write(requestcc13010Up.Span);

                        Memory<byte> buffer = new byte[0x10];
                        int readSize = await dongleStream.ReadAsync(buffer);

                        Console.WriteLine($"Read raw data: {DataToString(buffer.Span)}");
                        buffer = buffer.Slice(1, buffer.Span[0]);
                        Console.WriteLine($"Read data: {DataToString(buffer.Span)}");

                        ////Something about verifying connection
                        //await sendCmd(0, new byte[0]);
                        //var bootResp = await getCmdResponse();
                        //Console.WriteLine($"boot resp = ({bootResp.Item1}, {bootResp.Item2})");

                        //Auto Baud
                        Console.WriteLine($"Requesting Auto Baud");
                        Memory<byte> requestBootMode = new byte[] { 0x55, 0x55 };
                        dongleStream.Write(requestBootMode.Span);

                        var resp = await getCmdResponse();
                        Console.WriteLine($"auto baud resp = ({resp.Item1}, {resp.Item2})");


                        //Testing Ping
                        Console.WriteLine($"Requesting Ping");
                        Memory<byte> requestPing = new byte[] { 0x03, 0x20, 0x20 };
                        dongleStream.Write(requestPing.Span);
                        resp = await getCmdResponse();
                        Console.WriteLine($"Ping resp = ({resp.Item1}, {resp.Item2})");

                        //Get Device ID
                        Console.WriteLine($"Requesting Chip ID");
                        Memory<byte> resqID = new byte[] { 0x03, 0x28, 0x28 };
                        dongleStream.Write(resqID.Span);
                        resp = await getCmdResponse();
                        Console.WriteLine($"Chip ID resp = ({resp.Item1}, {resp.Item2})");
                        if (resp.Item2)
                        {
                            var cmdRespData = await getCmdResponseData(resp.Item3);
                            if (cmdRespData.Item1)
                            {
                                Console.WriteLine($"Raw Chip ID: {DataToString(cmdRespData.Item2.Span)}");
                                DeviceID = BinaryPrimitives.ReadUInt32BigEndian(cmdRespData.Item2.Span);
                                DeviceVersion = (uint)((DeviceID >> 0x1C) < 2 ? 1 : 2);
                                Console.WriteLine($"Chip ID: {DeviceID:X4} Version: {DeviceVersion}");
                                sendCmdAck(true);
                            }
                            else
                                sendCmdAck(false);
                        }
                        else
                            sendCmdAck(false);

                        //Get Flash Size
                        Console.WriteLine("Requesting RAM Size");
                        var ramResp = await readMemory32(0x40082250, 1);


                        //Get Flash Size
                        Console.WriteLine("Requesting Flash Size");
                        var flashResp = await readMemory32(0x4003002c, 1);

                        Console.WriteLine("Requesting Device ID");
                        var ipDeviceID = await readMemory32(0x50001318, 1);

                        //DUMP FLASH FIRMWARE
                        string rwResp;
                        while (true)
                        {
                            Console.WriteLine("Read or Write? <r,w, exit>");
                            rwResp = Console.ReadLine();
                            rwResp = rwResp.ToLower();
                            if (rwResp == "w" || rwResp == "r")
                                break;
                            if (rwResp == "exit")
                                return;
                        }

                        if (rwResp == "r")
                        {

                            Console.WriteLine("Output name:");
                            string outputPath = Console.ReadLine();

                            var flashMem = await readMemory32(0x0, 32768); //32768 * 4 = size of firmware file pulled from my v2 came (and the HMS).
                            Console.WriteLine("Recv all Flash");

                            using (var writer = new BinaryWriter(File.OpenWrite(outputPath)))
                            {
                                writer.Write(flashMem.Item2.ToArray());
                            }
                            Console.WriteLine("File saved");

                        }
                        else if (rwResp == "w")
                        {
                            string firmwarefile;
                            while (true)
                            {
                                Console.WriteLine("Firmare file?:");
                                firmwarefile = Console.ReadLine();
                                if (File.Exists(firmwarefile))
                                    break;
                                Console.WriteLine("Firmware file not found");
                            }

                            Console.WriteLine("***********************");
                            Console.WriteLine("***STARTING TO FLASH***");
                            Memory<byte> newFirmware = new Memory<byte>(File.ReadAllBytes(firmwarefile));

                            (bool, uint) chipCrc32;
                            uint crc32File = calcCRC32LikeChip(newFirmware.Span);
                            Console.WriteLine($"File CRC32 (Len: {newFirmware.Length})= {crc32File:X4}");

                            if (await eraseFlash(0x0, (uint)newFirmware.Length))
                            {
                                if (await writeFlashRange(0x0, newFirmware))
                                {

                                    Console.WriteLine("Requesting PostDownload CRC32");
                                    chipCrc32 = await getCRC32(0x0, 32768 * 4);
                                    if (chipCrc32.Item1)
                                        Console.WriteLine($"PostDownload CRC32 = {chipCrc32.Item2:X4}");
                                    else
                                        Console.WriteLine("Failed to get PostDownload crc32");

                                    Console.WriteLine("*********************");
                                    Console.WriteLine("***Reseting Device***");
                                    Console.WriteLine("*********************");
                                    await cmdReset();
                                }
                                else
                                {
                                    Console.WriteLine("####Failed to download firmware####");
                                }
                            }
                            else
                            {
                                Console.WriteLine("####Failed to erase flash####");
                            }

                            Console.WriteLine("Requesting CRC32");
                            chipCrc32 = await getCRC32(0x0, 32768 * 4);
                            if (chipCrc32.Item1)
                                Console.WriteLine($"CRC32 = {chipCrc32.Item2:X4}");
                            else
                                Console.WriteLine("Failed to get crc32");

                            ////CRC Calc isn't correct. Need to address.
                            //Console.WriteLine("Testing File CRC32 Calc");
                            //Memory<byte> fileBytes = new Memory<byte>(File.ReadAllBytes(@"mycc1310dump.bin"));
                            //uint crc32File = calcCRC32LikeChip(fileBytes.Span);
                            //Console.WriteLine($"File CRC32 (Len: {fileBytes.Length})= {crc32File:X4}");
                        }

                    }
                }
                else
                    Console.WriteLine($"Device doesn't exist at path: {args[0]}");
            }
            else
                Console.WriteLine($"No device path supplied './WyzeSenseUpgrade [device path]");
        }
        private static async Task<bool> cmdReset()
        {
            await sendCmd(0x25, null);
            return true;
        }
        private static async Task<bool> writeFlashRange(uint StartAddress, ReadOnlyMemory<byte> Data)
        {
            uint ByteCount = (uint)Data.Length;
            uint bytesLeft, dataIdx, bytesInTransfer;
            uint transferNumber = 1;
            bool bIsRetry = false;

            //tTransfer pvTransfer[2];


            uint ui32TotChunks = (ByteCount / MAX_PACK_SIZE);
            if (ByteCount % 252 > 0) ui32TotChunks++;

            uint ui32CurrChunk = 0;

            uint ui32BLCfgAddr = 0x0 + 0x20000 - 0x1000 + 0xFDB;// FlashAddr + FlashSize - Page Size + BL Config Offset.
            uint ui32BLCfgDataIdx = ui32BLCfgAddr - StartAddress; // This should be 0xC5 otherwise bootloader is disabled.

            if(ui32BLCfgDataIdx <= ByteCount)
            {
                if(Data.Span[(int)ui32BLCfgDataIdx] != 0xC5)
                {
                    Console.WriteLine("[writeFlashRange] Bootloader being disabled is unsupported at this time.");
                }
            }


            //Send Download Command.
            if(!await cmdDownload(StartAddress, ByteCount))
            {
                Console.WriteLine("[writeFlashRange] Failed to initiate download");
                return false;
            }

            var statusResp = await readStatus();
            if(!statusResp.Item1)
            {
                Console.WriteLine("[writeFlashRange] Failed to read status after download command");
                return false;
            }
            if(statusResp.Item2 != 0x40)
            {
                Console.WriteLine($"[writeFlashRange] Error after download command {statusResp.Item2:X2}");
                return false;
            }
            Console.WriteLine("[writeFlashRange] Starting to write data to flash");
            //Send Data in chunks
            bytesLeft = ByteCount;
            dataIdx = 0;
            while(bytesLeft > 0)
            {
                bytesInTransfer = Math.Min(MAX_PACK_SIZE, bytesLeft);

                if (!await cmdSendData(Data.Slice((int)dataIdx, (int)bytesInTransfer)))
                {
                    Console.WriteLine($"[writeFlashRange] Error during flash download. Addr: {StartAddress + dataIdx} xfer#: {transferNumber}");
                    return false;
                }

                statusResp = await readStatus();
                if(!statusResp.Item1)
                {
                    Console.WriteLine($"[writeFlashRange] Error during flash download. Addr: {StartAddress + dataIdx} xfer#: {transferNumber}");
                    return false;
                }
                if(statusResp.Item2 != 0x40)
                {
                    Console.WriteLine($"[writeFlashRange] Device Returns Status {statusResp.Item2} xfer#: {transferNumber}");
                    if(bIsRetry)
                    {
                        Console.WriteLine($"[writeFlashRange] Error retrying flash download. Addr: {StartAddress + dataIdx} xfer#: {transferNumber}");
                        return false;
                    }
                    bIsRetry = true;
                    continue;
                }

                bytesLeft -= bytesInTransfer;
                dataIdx += bytesInTransfer;
                transferNumber++;
                bIsRetry = false;
                
            }

            Console.WriteLine("[writeFlashRange] Successfully Wrote flash");
            return true;
        }
        private static async Task<bool>  cmdSendData(ReadOnlyMemory<byte> DataChunk)
        {
            if(DataChunk.Length > MAX_PACK_SIZE)
            {
                Console.WriteLine($"[cmdSendData] DataChunk Size {DataChunk.Length} exceeds max transfer of {MAX_PACK_SIZE}");
                return false;
            }

            await sendCmd(0x24, DataChunk);

            var cmdResp = await getCmdResponse();
            if (!cmdResp.Item1 || !cmdResp.Item2) return false;

            return true;

        }
        private static async Task<bool> cmdDownload(uint StartAddress, uint ByteCount)
        {
            Memory<byte> packet = new byte[8];
            BinaryPrimitives.WriteUInt32BigEndian(packet.Slice(0).Span, StartAddress);
            BinaryPrimitives.WriteUInt32BigEndian(packet.Slice(4).Span, ByteCount);


            await sendCmd(0x21, packet);

            Console.WriteLine("[cmdDownload] Getting Resp");
            var resp = await getCmdResponse();
            if (!resp.Item1 || !resp.Item2) return false;

            return true;
        }
        private static async Task<bool> eraseFlash(uint StartAddress, uint ByteCount)
        {
            Memory<byte> packet = new byte[4];


            uint pageCount = ByteCount / 0x1000;
            if (ByteCount % 0x1000 > 0) pageCount++;

            for(int i = 0; i < pageCount; i++)
            {
                BinaryPrimitives.WriteUInt32BigEndian(packet.Span, (uint)(StartAddress + i * 0x1000));

                await sendCmd(0x26, packet);

                var cmdResp = await getCmdResponse();
                if (!cmdResp.Item1 || !cmdResp.Item2) return false;

                var statusResp = await readStatus();
                if (statusResp.Item2 != 0x40)
                {
                    Console.WriteLine($"[eraseFlash] Failed to Erase Flash. Page may be locked Addr: {StartAddress + i * 0x1000}");
                    return false;
                }
            }
            return true;
        }

        private static async Task<(bool, Memory<byte>)> readMemory32(uint StartAddress, uint UnitCount)
        {
            if((StartAddress & 0x03) == 0x03)
            {
                Console.WriteLine("Address needs to be a multiple of 4");
                return (false, null);
            }

            Memory<byte> cmdPayload = new byte[6];
            Memory<byte> responseData = new byte[UnitCount * 4];

            uint chunkCount = UnitCount / SBL_CC2650_MAX_MEMREAD_WORDS;
            if (UnitCount % SBL_CC2650_MAX_MEMREAD_WORDS > 0) chunkCount++;

            uint remainingCount = UnitCount;

            Console.WriteLine($"[readMemory32] Attempting to read {chunkCount} chunks");

            for(int i = 0; i < chunkCount; i++)
            {
                uint dataOffset = (uint)(i * SBL_CC2650_MAX_MEMREAD_WORDS);
                uint chunkStart = (uint)(StartAddress + dataOffset);
                uint chunkSize = Math.Min(remainingCount, SBL_CC2650_MAX_MEMREAD_WORDS);
                remainingCount -= chunkSize;

                BinaryPrimitives.WriteUInt32BigEndian(cmdPayload.Span, chunkStart);
                cmdPayload.Span[4] = SBL_CC2650_ACCESS_WIDTH_32B;
                cmdPayload.Span[5] = (byte)chunkSize;

                await sendCmd(0x2A, cmdPayload);

                //await readStatus();


                Console.WriteLine("[readMemory32] Getting Resp");
                var resp = await getCmdResponse();
                if (!resp.Item1 || !resp.Item2) return (false, null);

                Console.WriteLine("[readMemory32] Getting Resp Data");


                var dataResp = await getCmdResponseData(resp.Item3);
                if (!dataResp.Item1)
                {
                    sendCmdAck(false);
                    return (false, null);
                }

                Console.WriteLine($"[readMemory32] Raw Resp Data: {DataToString(dataResp.Item2.Span)}");

                dataResp.Item2.CopyTo(responseData.Slice((int)(dataOffset * 4)));

                sendCmdAck(true);
                
            }
            return (true, responseData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Value1 = cmdResponse success. Value2 = previousCmdAck Value3 = data length</returns>
        private static async Task<(bool, bool,int)> getCmdResponse()
        {
            Memory<byte> buffer = new byte[3];
            try
            {
                Console.WriteLine($"[getCmdResponse] Attempting to read 3 bytes");
                await dongleStream.ReadAsync(buffer);
                Console.WriteLine($"[getCmdResponse] Read raw data: {DataToString(buffer.Span)}");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            if (buffer.Span[0] < 2)
                return (false,false, buffer.Span[0]);
            else
            {
                if(buffer.Span[2] == 0xCC)
                    return (true, true, buffer.Span[0]);
                else if(buffer.Span[2] == 0x33)
                    return (true, false, buffer.Span[0]);
            }
            return (false, false, buffer.Span[0]);
        }

        private static async Task<(bool, Memory<byte>)> getCmdResponseData(int DataLength)
        {
            Memory<byte> buffer = new byte[DataLength - 2];
            int readCount = await dongleStream.ReadAsync(buffer);
            Console.WriteLine($"[getCmdResponseData] Read raw data: {DataToString(buffer.Span)} ({readCount}/{buffer.Length})");

            int expectedPackLength = buffer.Span[0] - 2; // sub 2 for packet len and checksum.
            byte expectedChecksum = buffer.Span[1];
            int buffIdx = 0;
            Memory<byte> finalBuff = new byte[expectedPackLength];
            buffer.Slice(2).CopyTo(finalBuff);
            buffIdx += readCount - 2; //Sub 2 for packet len and check sum.

            while(expectedPackLength != buffIdx)
            {
                int remaining = expectedPackLength - buffIdx;
                Console.WriteLine($"[getCmdResponseData] Reading additional data: {remaining} remaining");
                int addBufLen = remaining > 0x3E ? 0x3F : (remaining + 1); 

                Memory<byte> additionalBuff = new byte[addBufLen];
                int readCountAddtl = await dongleStream.ReadAsync(additionalBuff);

                additionalBuff.Slice(1).CopyTo(finalBuff.Slice(buffIdx));
                buffIdx += readCountAddtl - 1; //Sub 1 for data len.

                Console.WriteLine($"Read raw {readCountAddtl} data2: {DataToString(additionalBuff.Span)}");
            }


            byte ourcheckSum = generateCheckSum(0, finalBuff);

            if(expectedChecksum != ourcheckSum)
            {
                Console.WriteLine($"[getCmdresponseData] Checksum mismatch {expectedChecksum} != {ourcheckSum} (ours)");
                return (false, null);
            }

            return (true, finalBuff);
        }

        private static async Task<bool> sendCmd(uint Command, ReadOnlyMemory<byte> Data)
        {
            Memory<byte> buff = new byte[Data.Length + 3];
            byte checkSum = generateCheckSum(Command, Data);

            buff.Span[0] = (byte)buff.Length;
            buff.Span[1] = checkSum;
            buff.Span[2] = (byte)Command;

            if (Data.Length > 0)
                Data.CopyTo(buff.Slice(3));

            Console.WriteLine($"[sendCmd] Write Raw: {DataToString(buff.Span)}");
            await dongleStream.WriteAsync(buff);
            return true;
        }

        private static async Task<(bool, uint)> readStatus()
        {
            Console.WriteLine($"[readStatus] Sending Read Status");
            await sendCmd(0x23, null);

            Console.WriteLine($"[readStatus] getting Cmd Response");
            var resp = await getCmdResponse();
            Console.WriteLine($"read status resp = ({resp.Item1}, {resp.Item2})");
            if (!resp.Item1 || !resp.Item2) return (false, 0);

            var dataResp = await getCmdResponseData(resp.Item3);

            Console.WriteLine($"[readStatus] raw resp data {DataToString(dataResp.Item2.Span)}");

            if (!dataResp.Item1)
            {
                sendCmdAck(false);
                return (false, 0);
            }

            sendCmdAck(true);
            return (true, dataResp.Item2.Span[0]);
        }

        private static async Task<(bool, uint)> getCRC32(uint Address, uint Length)
        {
            Memory<byte> packet = new byte[12];
            BinaryPrimitives.WriteUInt32BigEndian(packet.Slice(0).Span, Address);
            BinaryPrimitives.WriteUInt32BigEndian(packet.Slice(4).Span, Length);
            BinaryPrimitives.WriteUInt32BigEndian(packet.Slice(8).Span, 0);


            await sendCmd(0x27, packet);

            Console.WriteLine("[getCRC32] Getting Resp");
            var resp = await getCmdResponse();
            if (!resp.Item1 || !resp.Item2) return (false, 0);

            Console.WriteLine("[getCRC32] Getting Resp Data");


            var dataResp = await getCmdResponseData(resp.Item3);

            if (!dataResp.Item1)
            {
                sendCmdAck(false);
                return (false, BinaryPrimitives.ReadUInt32BigEndian(dataResp.Item2.Span));
            }

            Console.WriteLine($"[getCRC32] Raw Resp Data: {DataToString(dataResp.Item2.Span)}");

            sendCmdAck(true);
            return(true, BinaryPrimitives.ReadUInt32BigEndian(dataResp.Item2.Span));
        }

        private static uint calcCRC32LikeChip(ReadOnlySpan<byte> Data)
        {
            uint d, ind;
            uint acc = 0xFFFFFFFF;
            uint[] ulCrcRand32Lut = new uint[16]
            {
                 0x00000000, 0x1db71064, 0x3b6e20c8, 0x26d930ac,
                0x76dc4190, 0x6b6b51f4, 0x4db26158, 0x5005713c,
                0xedb88320, 0xf00f9344, 0xd6d6a3e8, 0xcb61b38c,
                0x9b64c2b0, 0x86d3d2d4, 0xa00ae278, 0xbdbdf21c
            };
            int byteCnt = Data.Length;
            //for(int idx = 0; idx < Data.Length; idx++)
            int idx = 0;
            while(byteCnt-- != 0)
            {
                d = Data[idx++];
                ind = (acc & 0x0F) ^ (d & 0x0F);
                acc = (acc >> 4) ^ ulCrcRand32Lut[ind];
                ind = (acc & 0x0F) ^ (d >> 4);
                acc = (acc >> 4) ^ ulCrcRand32Lut[ind];
            }

            return (acc ^ 0xFFFFFFFF);
        }
        private static byte generateCheckSum(uint Command, ReadOnlyMemory<byte> Data)
        {
            int count = (int)Command;
            ReadOnlySpan<byte> dataSpan = Data.Span;

            for(int i = 0; i < dataSpan.Length; i++)
            {
                count += dataSpan[i];
            }
            return (byte)count;
        }

        private static void sendCmdAck(bool Success)
        {
            Console.WriteLine($"[sendCmdAck] Sending {(Success ? "Ack" : "Nack")}");
            Span<byte> buff = new byte[2];
            //buff[0] = 3;
            buff[0] = 0;
            buff[1] =(byte)(Success ? 0xCC : 0x33);
            
            dongleStream.Write(buff);

        }
        private static string DataToString(ReadOnlySpan<byte> data)
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
