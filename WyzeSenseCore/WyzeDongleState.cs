using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WyzeSenseCore
{
    public class WyzeDongleState
    {
        public byte[] ENR;
        public string MAC;
        public string Version;
        public byte DeviceType;
        public bool LEDState = true;
        public bool IsInclusive = false;
        public byte AuthState = 0;
        public override string ToString()
        {
            string enrbyteString = "";
            for (int i = 0; i < ENR.Length; i++)
            {
                enrbyteString += string.Format("{0:X2} ", ENR[i]);
            }
            return string.Format($"{MAC}: AuthState: {AuthState}, Version: {Version}, DeviceType: {DeviceType}, LED State:{LEDState}, IsInclusive: {IsInclusive} ENR: {enrbyteString}");
        }
    }
}
