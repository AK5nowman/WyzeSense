using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WyzeSenseCore
{
    public enum WyzeEventType : byte
    {
        Alarm = 0xA2,
        Status = 0xA1,
        Climate = 0xE8,
        UserAction = 0x99,
        Unknown
    }
}
