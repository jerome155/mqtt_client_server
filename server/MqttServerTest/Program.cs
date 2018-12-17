using System;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MqttServerTest
{
    static class Program
    {
        private static IMqttServer _mqttServer;
        //private static IMqttServerOptions _mqttServerOptions;
        //private static readonly IMqttClient MqttClient = new MqttFactory().CreateMqttClient();
        private const string MqttTopic = "MainTopic";

        private static void Main(string[] args)
        {
            StartServer();
            //ConnectSendingClient();
            AttachAlarmDelegates();
            RunServer();
            ShutdownServer();
        }

        private static void AttachAlarmDelegates()
        {
            _mqttServer.ClientConnected += MqttServerOnClientConnected;
            _mqttServer.ClientDisconnected += MqttServerOnClientDisconnected;
            _mqttServer.ClientSubscribedTopic += MqttServerOnClientSubscribedTopic;
            _mqttServer.ClientUnsubscribedTopic += MqttServerOnClientUnsubscribedTopic;
        }

        private static void MqttServerOnClientUnsubscribedTopic(object sender, MQTTnet.Server.MqttClientUnsubscribedTopicEventArgs e)
        {
            Console.WriteLine("Client unsubscribed from topic: " + e.ClientId + ", " + e.TopicFilter);
        }


        private static void MqttServerOnClientDisconnected(object sender, MQTTnet.Server.MqttClientDisconnectedEventArgs e)
        {
            Console.WriteLine("Client disconnected: " + e.ClientId + ", Gracefull shutdown? " + e.WasCleanDisconnect);
        }

        //private static async void ConnectSendingClient()
        //{
        //    IMqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder()
        //        .WithClientId("MyClient")
        //        .WithTcpServer("localhost", 1884)
        //        .WithKeepAlivePeriod(TimeSpan.FromMilliseconds(-1))
        //        .Build();

        //    await MqttClient.ConnectAsync(mqttClientOptions);
        //}

        private static async void ShutdownServer()
        {
            try
            {
                await _mqttServer.ClearRetainedMessagesAsync();
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

            //_mqttServerOptions = new MqttServerOptionsBuilder()
            //    .WithMaxPendingMessagesPerClient(100)
            //    .WithConnectionBacklog(100)
            //    .WithDefaultEndpointPort(1884)
            //    .WithConnectionValidator(c => c.ReturnCode = MqttConnectReturnCode.ConnectionAccepted)
            //    .WithStorage(_retainedMsgHandler)
            //    .WithPersistentSessions()
            //    .Build();

            MqttServerOptions opt = new MqttServerOptions();
            opt.PendingMessagesOverflowStrategy = MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage;
            opt.EnablePersistentSessions = true;
            opt.MaxPendingMessagesPerClient = 1000;
            opt.DefaultEndpointOptions.Port = 1883;
            opt.DefaultEndpointOptions.IsEnabled = true;
            opt.ConnectionValidator = c => c.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;

            await _mqttServer.StartAsync(opt);

            Console.WriteLine("Server Pending Messages Overflow Strategy: " + _mqttServer.Options.PendingMessagesOverflowStrategy.ToString());
            Console.WriteLine("Server Persistent Sessions: " + _mqttServer.Options.EnablePersistentSessions);

            Console.WriteLine("Server started.");
        }

        private static void MqttServerOnClientSubscribedTopic(object sender, MQTTnet.Server.MqttClientSubscribedTopicEventArgs e)
        {
            Console.WriteLine("Client subscribed to topic: " + e.ClientId + ", " + e.TopicFilter);
        }

        private static void MqttServerOnClientConnected(object sender, MQTTnet.Server.MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("Client connected: " + e.ClientId);
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
                    .WithPayload(DateTime.Now + ": TestMessage "  + _msgCounter)
                    .WithExactlyOnceQoS()
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                    .Build());
                
                Console.WriteLine("Message sent (Counter = " + _msgCounter + ")");

                Console.WriteLine(_mqttServer.GetClientSessionsStatusAsync().Result.Count);

                foreach (IMqttClientSessionStatus status in _mqttServer.GetClientSessionsStatusAsync().Result)
                {
                    
                    Console.WriteLine("Current retained message count per Client: " + status.ClientId + ", " + status.PendingApplicationMessagesCount);                       
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
