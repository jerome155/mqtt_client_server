using Buhler.IoT.GenericDataModel.V1;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqttServerTest
{
    public class MqttServerExample : IDisposable
    {
        private IMqttServer _mqttServer;

        public MqttServerExample()
        {
            if (_mqttServer == null) _mqttServer = new MqttFactory().CreateMqttServer();

            MqttServerOptions opt = new MqttServerOptions();
            opt.PendingMessagesOverflowStrategy = MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage;
            opt.EnablePersistentSessions = true;
            opt.MaxPendingMessagesPerClient = 1000;
            opt.DefaultEndpointOptions.Port = 1883;
            opt.DefaultEndpointOptions.IsEnabled = true;
            opt.ConnectionValidator = c => c.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;

            _mqttServer.StartAsync(opt);
        }

        public void Dispose()
        {
            _mqttServer.StopAsync().Wait();
        }

        public async Task SendAsync(GenericDataModelMessage gdmMsg)
        {
            await _mqttServer.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic("/Mario")
                .WithPayload(gdmMsg.ToString(Newtonsoft.Json.Formatting.None))
                .WithExactlyOnceQoS()
                .Build());
        }

    }
}
