using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WyzeSenseBlazor.DataServices
{
    public class MqttClientServiceProvider
    {
        public readonly IMqttClientService MqttClientService;

        public MqttClientServiceProvider(IMqttClientService mqttClientService)
        {
            MqttClientService = mqttClientService;
        }
    }
}
