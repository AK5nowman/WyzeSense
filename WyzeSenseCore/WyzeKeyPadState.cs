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
}
