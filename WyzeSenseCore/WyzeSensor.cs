using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WyzeSenseCore
{
    public sealed class WyzeSensor
    {
        public DateTime LastActive;
        public string MAC;
        public WyzeSenseEvent LastEvent;
        public WyzeSensor(ReadOnlySpan<byte> Data)
        {
            MAC = ASCIIEncoding.ASCII.GetString(Data.Slice(5, 8));
            LastActive = DateTime.MinValue;
        }
    }
}
