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
    static class Program
    {
        private static IMqttClient MqttClient = new MqttFactory().CreateMqttClient();
        private static int _msgCounter;

        private static void Main(string[] args)
        {
            //ConnectClient();
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



        private static void MqttClientOnDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            if (e.Exception == null)
            {
                Console.WriteLine("Client disconnected.");
            }
            else
            {
                Console.WriteLine("Client disconnected: " + e.Exception.Message);
            }
        }

        private static void MqttClientOnConnected(object sender, MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("Client connected: " + e.IsSessionPresent);
            //await SubscribeToTopic(string.Empty);
        }

        private static void ShutdownClient()
        {
            UnsubscribeClient();
            DisconnectClient();
        }

        private static async void UnsubscribeClient()
        {
            await MqttClient.UnsubscribeAsync(new List<string> { "/MainTopic" });
            Console.WriteLine("Unsubscribed Client.");
        }

        private static async void DisconnectClient()
        {
            await MqttClient.DisconnectAsync();
        }

        private static void RunClient()
        {
            while (true)
            {
                string input = Console.ReadLine();
                switch (input)
                {
                    case "Subscribe":
                        string topic = Console.ReadLine();
                        SubscribeToTopic(topic).Wait();
                        break;
                    case "Unsubscribe":
                        UnsubscribeClient();
                        break;
                    case "Connect":
                        string address = Console.ReadLine();
                        ConnectClient(address);
                        break;
                    case "SendMessage":
                        SendMessage();
                        break;
                    case "Disconnect":
                        DisconnectClient();
                        break;
                    case "Shutdown":
                        return;
                    case "StartObservation":
                        string variable = Console.ReadLine();
                        observerActive = true;
                        observerVariable = variable;
                        break;
                    case "StopObservation":
                        observerActive = false;
                        break;
                }

            }

        }

        private static bool observerActive = false;
        private static string observerVariable;
       

        private static void SendMessage()
        {
            _msgCounter++;
            MqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic("/MainTopic")
                .WithPayload(DateTime.Now + ": TestMessage " + _msgCounter)
                .WithExactlyOnceQoS()
                .Build()).Wait();
        }

        private static async Task SubscribeToTopic(string topic)
        {
            try
            {
                await MqttClient.SubscribeAsync(topic, MqttQualityOfServiceLevel.ExactlyOnce);

                Console.WriteLine("Subscribed to Topic.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
 
        }

        private static async void ConnectClient(String address)
        {
            try
            {
                IMqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithClientId("RealClient")
                    .WithTcpServer(address, 1884)
                    .WithCleanSession(false)
                    .WithKeepAlivePeriod(TimeSpan.FromMilliseconds(-1))
                    .Build();

                await MqttClient.ConnectAsync(mqttClientOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            //await SubscribeToTopic("/MainTopic");
        }

        private static void MqttClientOnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            if (observerActive)
            {
                string message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                if (message.Contains(observerVariable))
                {
                    string outString = message.Substring(message.IndexOf(observerVariable));
                    outString = outString.Substring(outString.IndexOf("\"value\":"));
                    outString = outString.Substring(outString.IndexOf(":"), outString.IndexOf("}")- outString.IndexOf(":"));
                    Console.WriteLine("Value of " + observerVariable + ": " + outString);
                }
            }
            else
            {
                Console.WriteLine("Message received: " + Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
            }
        }
    }
}
