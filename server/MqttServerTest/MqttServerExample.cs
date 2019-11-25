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
    public class MqttServerExample
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

        public async Task SendAsync(Gener)
        {

        }


    }
}
