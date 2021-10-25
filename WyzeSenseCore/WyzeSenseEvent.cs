using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Binary;

namespace WyzeSenseCore
{


    public sealed class WyzeSenseEvent
    {
        public WyzeSensor Sensor;

        public DateTime ServerTime;

        public WyzeEventType EventType;

        public Dictionary<string, object> Data;

        public override string ToString()
        {
            return string.Format("{0} - {1}: {2}", Sensor.MAC, ServerTime, Sensor.Type);
        }
    }

}
