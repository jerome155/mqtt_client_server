using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using MQTTnet.Serializer;

namespace MqttClientTest
{
    static class Program
    {
        private static readonly IMqttClient MqttClient = new MqttFactory().CreateMqttClient();

        private static void Main(string[] args)
        {
            ConnectClient();
            AttachEventDelegates();
            RunClient();
            ShutdownClient();
        }

        private static void AttachEventDelegates()
        {
            MqttClient.ApplicationMessageReceived += MqttClientOnApplicationMessageReceived;
            MqttClient.Connected += MqttClientOnConnected;
            MqttClient.Disconnected += MqttClientOnDisconnected;
        }

        

    private static async void MqttClientOnDisconnected(object sender, MqttClientDisconnectedEventArgs e) 
        {
            if (e.Exception == null)
            {
                Console.WriteLine("Client disconnected.");
            }
            else
            {
                Console.WriteLine("Client disconnected: " + e.Exception.Message);
            }
            
            Console.WriteLine("Retrying in 5...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            ConnectClient();
        }

        private static void MqttClientOnConnected(object sender, MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("Client connected: " + e.IsSessionPresent);
            //SubscribeToTopic();
        }

        private static void ShutdownClient()
        {
            UnsubscribeClient();
            DisconnectClient();
        }

        private static async void UnsubscribeClient()
        {
            await MqttClient.UnsubscribeAsync(new List<string> { "MainTopic" });
            Console.WriteLine("Unsubscribed Client.");
        }

        private static async void DisconnectClient()
        {
            await MqttClient.DisconnectAsync();
            Console.WriteLine("Disconnected Client.");
        }

        private static void RunClient()
        {
            while (true)
            {
                string input = Console.ReadLine();
                switch (input)
                {
                    case "Subscribe":
                        SubscribeToTopic();
                        break;
                    case "Unsubscribe":
                        UnsubscribeClient();
                        break;
                    case "Connect":
                        ConnectClient();
                        break;
                    case "Disconnect":
                        DisconnectClient();
                        break;
                    case "Shutdown":
                        return;
                }
            }

        }

        private static void SubscribeToTopic()
        {
            MqttClient.SubscribeAsync(new List<TopicFilter> { new TopicFilterBuilder().WithTopic("MainTopic").WithExactlyOnceQoS().Build()});
            Console.WriteLine("Subscribed to Topic.");
        }

        private static async void ConnectClient()
        {
            IMqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId("RealClient")
                .WithTcpServer("localhost", 1884)
                .WithKeepAlivePeriod(TimeSpan.FromMilliseconds(-1))
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .Build();

            IManagedMqttClientOptions managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(mqttClientOptions)
                .Build();
            

            try
            {
                await MqttClient.ConnectAsync(mqttClientOptions);
            }
            catch
            {
            }
        }

        private static void MqttClientOnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            Console.WriteLine("Message received: " + Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
        }
    }
}
