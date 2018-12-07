using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Serializer;

namespace MqttClientTest
{
    using System.Runtime.CompilerServices;

    static class Program
    {
        private static IMqttClient MqttClient = new MqttFactory().CreateMqttClient();
        private static int _msgCounter;

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
            //MqttClient.Connected += MqttClientOnConnected;
            //MqttClient.Disconnected += MqttClientOnDisconnected;
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
            //Console.WriteLine("Client connected: " + e.IsSessionPresent);
            //SubscribeToTopic(string.Empty);
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
                        SubscribeToTopic().Wait();
                        break;
                    case "Unsubscribe":
                        UnsubscribeClient();
                        break;
                    case "Connect":
                        ConnectClient();
                        break;
                    case "SendMessage":
                        SendMessage();
                        break;
                    case "Disconnect":
                        DisconnectClient();
                        break;
                    case "Shutdown":
                        return;
                }

                //if (input.StartsWith("Subscribe"))
                //{
                //    SubscribeToTopic(input.Substring(input.Length - 1, 1));
                //}
            }

        }

        private static void SendMessage()
        {
            _msgCounter++;
            MqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic($"/MainTopic")
                .WithPayload(DateTime.Now + ": TestMessage " + _msgCounter)
                .WithExactlyOnceQoS()
                .Build()).Wait();
        }

        private static async Task SubscribeToTopic()
        {
            try
            {
                IMqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithClientId("RealClient")
                    .WithTcpServer("localhost", 1884)
                    .WithCleanSession(false)
                    .WithKeepAlivePeriod(TimeSpan.FromMilliseconds(-1))
                    .Build();

                await MqttClient.ConnectAsync(mqttClientOptions);
                await MqttClient.SubscribeAsync("MainTopic", MqttQualityOfServiceLevel.ExactlyOnce);

                Console.WriteLine("Subscribed to Topic.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
 
        }

        private static async void ConnectClient()
        {
        }

        private static async void MqttClientOnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            Console.WriteLine("Message received: " + Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
        }
    }
}
