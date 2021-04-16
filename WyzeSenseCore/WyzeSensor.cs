using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WyzeSenseCore
{
    public sealed class WyzeSensor
    {
        public string MAC;
        public WyzeSensorType Type;
        public byte Version;
        public WyzeSensor(ReadOnlySpan<byte> Data)
        {
            MAC = ASCIIEncoding.ASCII.GetString(Data.Slice(5, 8));
        }
        public WyzeSensor() { }
    }
}
