using System;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
#if MQTTNET3
using MQTTnet.Server.Status;
#endif

namespace MqttServerTest
{
    static class Program
    {
        private static IMqttServer _mqttServer;
        private const string MqttTopic = "MainTopic";

        private static void Main(string[] args)
        {
            StartServer();
            AttachAlarmDelegates();
            RunServer();
            ShutdownServer();
        }

        private static void AttachAlarmDelegates()
        {
#if MQTTNET3
            _mqttServer.UseClientConnectedHandler(e => MqttServerOnClientConnected(null, e));
            _mqttServer.UseClientDisconnectedHandler(e => MqttServerOnClientDisconnected(null, e));
#else
            _mqttServer.ClientConnected += MqttServerOnClientConnected;
            _mqttServer.ClientDisconnected += MqttServerOnClientDisconnected;
            _mqttServer.ClientSubscribedTopic += MqttServerOnClientSubscribedTopic;
            _mqttServer.ClientUnsubscribedTopic += MqttServerOnClientUnsubscribedTopic;
#endif
        }

#if MQTTNET3

        private static void MqttServerOnClientUnsubscribedTopic(object sender, MqttServerClientUnsubscribedTopicEventArgs e)
        {
            Console.WriteLine("Client unsubscribed from topic: " + e.ClientId + ", " + e.TopicFilter);
        }


        private static void MqttServerOnClientDisconnected(object sender, MqttServerClientDisconnectedEventArgs e)
        {
            Console.WriteLine("Client disconnected: " + e.ClientId + ", Gracefull shutdown? " + e.DisconnectType);
        }


        private static void MqttServerOnClientSubscribedTopic(object sender, MQTTnet.Server.MqttServerClientSubscribedTopicEventArgs e)
        {
            Console.WriteLine("Client subscribed to topic: " + e.ClientId + ", " + e.TopicFilter);
        }

        private static void MqttServerOnClientConnected(object sender, MQTTnet.Server.MqttServerClientConnectedEventArgs e)
        {
            Console.WriteLine("Client connected: " + e.ClientId);
        }

#else

        private static void MqttServerOnClientUnsubscribedTopic(object sender, MqttClientUnsubscribedTopicEventArgs e)
        {
            Console.WriteLine("Client unsubscribed from topic: " + e.ClientId + ", " + e.TopicFilter);
        }


        private static void MqttServerOnClientDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
#if MQTTNET3
            Console.WriteLine("Client disconnected: " + e.ClientId + ", Gracefull shutdown? " + e.DisconnectType);
#else
            Console.WriteLine("Client disconnected: " + e.ClientId + ", Gracefull shutdown? " + e.WasCleanDisconnect);
#endif
        }


        private static void MqttServerOnClientSubscribedTopic(object sender, MqttClientSubscribedTopicEventArgs e)
        {
            Console.WriteLine("Client subscribed to topic: " + e.ClientId + ", " + e.TopicFilter);
        }

        private static void MqttServerOnClientConnected(object sender, MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("Client connected: " + e.ClientId);
        }

#endif

        private static async void ShutdownServer()
        {
            try
            {
#if MQTTNET3
                await _mqttServer.ClearRetainedApplicationMessagesAsync();
#else
                await _mqttServer.ClearRetainedMessagesAsync();
#endif
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            await _mqttServer.StopAsync();
        }

        private static async void StartServer()
        {
            if (_mqttServer == null) _mqttServer = new MqttFactory().CreateMqttServer();

#if MQTTNET3
            MqttServerOptions opt = (MqttServerOptions)new MqttServerOptionsBuilder()
                .WithConnectionValidator(c => c.ReturnCode = MqttConnectReturnCode.ConnectionAccepted).Build();
#else
            MqttServerOptions opt = new MqttServerOptions();
            opt.ConnectionValidator = c => c.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
#endif
            opt.PendingMessagesOverflowStrategy = MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage;
            opt.EnablePersistentSessions = true;
            opt.MaxPendingMessagesPerClient = 1000;
            opt.DefaultEndpointOptions.Port = 1883;
            opt.DefaultEndpointOptions.IsEnabled = true;

            await _mqttServer.StartAsync(opt);

            Console.WriteLine("Server Pending Messages Overflow Strategy: " + _mqttServer.Options.PendingMessagesOverflowStrategy.ToString());
            Console.WriteLine("Server Persistent Sessions: " + _mqttServer.Options.EnablePersistentSessions);

            Console.WriteLine("Server started.");
        }


        private static void RunServer()
        {

            while (true)
            {
                string input = Console.ReadLine();
                switch (input)
                {
                    case "SendMessage":
                        SendMessage();
                        break;
                    case "Shutdown":
                        return;
                }
            }

        }

        private static int _msgCounter = 0;

        private static async void SendMessage()
        {
            try
            {
                _msgCounter++;
                await _mqttServer.PublishAsync(new MqttApplicationMessageBuilder()
                    .WithTopic(MqttTopic)
                    .WithPayload(DateTime.Now + ": TestMessage " + _msgCounter)
                    .WithExactlyOnceQoS()
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                    .Build());

                Console.WriteLine("Message sent (Counter = " + _msgCounter + ")");

#if MQTTNET3
                Console.WriteLine(_mqttServer.GetSessionStatusAsync().Result.Count);

                foreach (IMqttSessionStatus status in _mqttServer.GetSessionStatusAsync().Result)
                {
                    Console.WriteLine("Current retained message count per Client: " + status.ClientId + ", " + status.PendingApplicationMessagesCount);
                }
#else
                Console.WriteLine(_mqttServer.GetClientSessionsStatusAsync().Result.Count);

                foreach (IMqttClientSessionStatus status in _mqttServer.GetClientSessionsStatusAsync().Result)
                {
                    Console.WriteLine("Current retained message count per Client: " + status.ClientId + ", " + status.PendingApplicationMessagesCount);
                }
#endif


            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
