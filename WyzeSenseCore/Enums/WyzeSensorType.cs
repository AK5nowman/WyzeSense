using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
