using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using WyzeSenseBlazor.Settings;
using WyzeSenseBlazor.DataStorage;
using WyzeSenseBlazor.DataStorage.Models;
using System.Collections.Generic;
using System.Linq;

namespace WyzeSenseBlazor.DataServices
{
    public class MqttClientService : IMqttClientService
    {
        private readonly IWyzeSenseService _wyzeSenseService;
        private readonly IDataStoreService _dataStore;
        private readonly IMqttClient _mqttClient;
        private readonly IMqttClientOptions _options;

        public MqttClientService(IMqttClientOptions options, IWyzeSenseService wyzeSenseService, IDataStoreService dataStore)
        {
            _options = options;

            _dataStore = dataStore;

            _wyzeSenseService = wyzeSenseService;
            _wyzeSenseService.OnEvent += _wyzeSenseService_OnEvent;

            _mqttClient = new MqttFactory().CreateMqttClient();
            ConfigureMqttClient();
        }

        private void _wyzeSenseService_OnEvent(object sender, WyzeSenseCore.WyzeSenseEvent e)
        {
            e.Data.Add("timestamp", e.ServerTime.ToString());

            bool hasPublished = false;

            //Topic should always start with the root topic.
            string topic = AppSettingsProvider.ClientSettings.Topic;

            if (_dataStore.DataStore.Sensors.TryGetValue(e.Sensor.MAC, out var sensor))
            {
                List<Topics> toRemove = new();
                if (sensor.Topics?.Count() > 0)
                {
                    foreach (var topicTemplate in sensor.Topics)
                    {
                        if (_dataStore.DataStore.Templates.TryGetValue(topicTemplate.TemplateName, out var template))
                        {
                            Dictionary<string, object> payloadData = new();

                            //Template does exist, need to publish a message for each package.
                            foreach (var payloadPack in template.PayloadPackages)
                            {
                                payloadData.Clear();

                                topic = string.Join('/', topic, sensor.Alias, topicTemplate.RootTopic, payloadPack.Topic);
                                //Replace double slash to accomodate blank root topic.
                                topic = System.Text.RegularExpressions.Regex.Replace(topic, @"/+", @"/");
                                //Remove trailing slash to accomdate blank payload topic.
                                topic = topic.TrimEnd('/');

                                foreach (var pair in payloadPack.Payload)
                                {
                                    if (e.Data.TryGetValue(pair.Value, out var value))
                                        payloadData.Add(pair.Key, value);
                                }
                                if (payloadData.Count() > 0)
                                {
                                    //If event data contained any of the payload packet data lets add time and publish.
                                    payloadData.Add("timestamp", e.ServerTime.ToString());
                                    PublishMessageAsync(topic, JsonSerializer.Serialize(payloadData));
                                    hasPublished = true;
                                }
                            }
                        }
                        else
                        {
                            //Template doesn't exist
                            toRemove.Add(topicTemplate);
                        }
                    }
                    //Remove the topic templates that didn't have a valid template associated.
                    if(toRemove.Count() > 0)
                        toRemove.ForEach(p => sensor.Topics.Remove(p));
                }
                else if(sensor.Alias.Length > 0)
                {
                    //Sensor with no topics, publish with alias. 
                    PublishMessageAsync(string.Join('/', topic, sensor.Alias), JsonSerializer.Serialize(e.Data));
                    hasPublished = true;
                }
            }
            if (!hasPublished)
            {
                //No database sensor, publish all data to MAC
                PublishMessageAsync(string.Join('/', topic, e.Sensor.MAC), JsonSerializer.Serialize(e.Data));
            }
            
        }

        private async Task PublishMessageAsync(string topic, string payload)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithExactlyOnceQoS()
                .Build();
            await _mqttClient.PublishAsync(message);
        }


        private void ConfigureMqttClient()
        {
            _mqttClient.ConnectedHandler = this;
            _mqttClient.DisconnectedHandler = this;
        }

        public async Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            await _mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(AppSettingsProvider.ClientSettings.Topic)
                .WithPayload("Online")
                .WithExactlyOnceQoS()
                .Build());
        }

        public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            //throw new System.NotImplementedException();
            return Task.CompletedTask;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _mqttClient.ConnectAsync(_options);
            if (!_mqttClient.IsConnected)
            {
                await _mqttClient.ReconnectAsync();
            }
            System.Console.WriteLine("Finishing starting MQTT service");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                var disconnectOption = new MqttClientDisconnectOptions
                {
                    ReasonCode = MqttClientDisconnectReason.NormalDisconnection,
                    ReasonString = "NormalDiconnection"
                };
                await _mqttClient.DisconnectAsync(disconnectOption, cancellationToken);
            }
            await _mqttClient.DisconnectAsync();
        }
    }
}
