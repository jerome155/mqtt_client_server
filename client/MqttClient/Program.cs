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
using Newtonsoft.Json.Linq;
using System.IO;
#if MQTTNET3
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
#else
using MQTTnet.Serializer;
#endif

namespace MqttClientTest
{
    static class Program
    {
        private static IMqttClient MqttClient = new MqttFactory().CreateMqttClient();
        private static int _msgCounter;
        private static List<string> _subscriptionList = new List<string>();

        private static bool observerActive = false;
        private static bool checkActive = false;
        private static string observerVariable;
        private static string helpText = "Commands: Subscribe, Unsubscribe, Connect, Disconnect, SendMessage, SendInvalidMessage, Shutdown, StartObservation, StopObservation, StartCheck, StopCheck, Help";

        private static void Main(string[] args)
        {
            //ConnectClient();
            PrintAsciiArt();
            AttachEventDelegates();
            Console.WriteLine(helpText);
            RunClient();
            ShutdownClient().Wait();
        }

        private static void PrintAsciiArt()
        {
            Console.WriteLine(" /$$      /$$  /$$$$$$  /$$$$$$$$   /$$$$$$$$        /$$$$$$   /$$ /$$                       /$$        ");
            Console.WriteLine("| $$$    /$$$ /$$__  $$| __  $$__ /|__  $$__ /       /$$__  $$| $$| __/                    | $$         ");
            Console.WriteLine("| $$$$  /$$$$| $$  \\ $$    | $$       | $$         | $$  \\__ /| $$ /$$  /$$$$$$  /$$$$$$$  /$$$$$$    ");
            Console.WriteLine("| $$ $$/$$ $$| $$  | $$    | $$       | $$         | $$       | $$| $$ /$$__  $$| $$__  $$| _  $$_ /    ");
            Console.WriteLine("| $$  $$$| $$| $$  | $$    | $$       | $$         | $$       | $$| $$| $$$$$$$$| $$  \\ $$   | $$      ");
            Console.WriteLine("| $$\\  $ | $$| $$/$$ $$    | $$       | $$         | $$    $$ | $$| $$| $$_____/| $$  | $$   | $$ /$$  ");
            Console.WriteLine("| $$ \\/  | $$|  $$$$$$/    | $$       | $$         |  $$$$$$/ | $$| $$|  $$$$$$$| $$  | $$   |  $$$$   ");
            Console.WriteLine("|__/     |__/ \\____ $$$    |__/       |__/          \\______/  |__/|__/ \\_______/|__/  |__/    \\___/ ");                                                                                                                                      
        }

        private static void AttachEventDelegates()
        {
#if MQTTNET3

            MqttClient.UseApplicationMessageReceivedHandler(e => MqttClientOnApplicationMessageReceived(null, e));
            MqttClient.UseConnectedHandler(e => MqttClientOnConnected(null, e));
            MqttClient.UseDisconnectedHandler(e => MqttClientOnDisconnected(null, e));
#else
            MqttClient.ApplicationMessageReceived += MqttClientOnApplicationMessageReceived;
            MqttClient.Connected += MqttClientOnConnected;
            MqttClient.Disconnected += MqttClientOnDisconnected;
#endif

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
#if MQTTNET3
            Console.WriteLine("Client connected: " + e.AuthenticateResult.ResultCode);
#else
            Console.WriteLine("Client connected: " + e.IsSessionPresent);
#endif
        }

        private static async Task ShutdownClient()
        {
            await UnsubscribeClient();
            await DisconnectClient();
        }

        private static async Task UnsubscribeClient()
        {
            foreach (var topic in _subscriptionList)
            {
                await MqttClient.UnsubscribeAsync(new string[] { topic });
                Console.WriteLine("Unsubscribed from topic " + topic);
            }
            _subscriptionList.Clear();
        }

        private static async Task DisconnectClient()
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
                    case "subscribe":
                        string topic = Console.ReadLine();
                        SubscribeToTopic(topic).Wait();
                        break;
                    case "Unsubscribe":
                    case "unsubscribe":
                        UnsubscribeClient().Wait();
                        break;
                    case "Connect":
                    case "connect":
                        string address = Console.ReadLine();
                        ConnectClient(address).Wait();
                        break;
                    case "SendMessage":
                    case "sendmessage":
                    case "sendMessage":
                        SendMessage();
                        break;
                    case "SendInvalidMessage":
                    case "sendinvalidMessage":
                    case "sendInvalidMessage":
                        SendInvalidMessage();
                        break;
                    case "Disconnect":
                    case "disconnect":
                        DisconnectClient().Wait();
                        break;
                    case "Shutdown":
                    case "shutdown":
                        return;
                    case "StartObservation":
                    case "startobservation":
                    case "startObservation":
                        string variable = Console.ReadLine();
                        observerActive = true;
                        observerVariable = variable;
                        break;
                    case "StopObservation":
                    case "stopobservation":
                    case "stopObservation":
                        observerActive = false;
                        break;
                    case "StartCheck":
                    case "startCheck":
                    case "startcheck":
                        checkActive = true;
                        break;
                    case "StopCheck":
                    case "stopCheck":
                    case "stopcheck":
                        checkActive = false;
                        break;
                    case "Help":
                    case "help":
                        Console.WriteLine(helpText);
                        break;
                }

            }

        }
       

        private static void SendMessage()
        {
            _msgCounter++;
            MqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic("/MainTopic")
                .WithPayload(DateTime.Now + ": TestMessage " + _msgCounter)
                .WithExactlyOnceQoS()
                .Build()).Wait();
        }

        private static void SendInvalidMessage()
        {
            _msgCounter++;
            MqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic("/MainTopic")
                .WithPayload(Message.InvalidMessage)
                .WithExactlyOnceQoS()
                .Build()).Wait();
        }

        private static async Task SubscribeToTopic(string topic)
        {
            try
            {
                await MqttClient.SubscribeAsync(topic, MqttQualityOfServiceLevel.ExactlyOnce);
                _subscriptionList.Add(topic);

                Console.WriteLine("Subscribed to Topic.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
 
        }

        private static async Task ConnectClient(String address)
        {
            try
            {
                IMqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithClientId("RealClient")
                    .WithTcpServer(address, 1883)
                    .WithCleanSession(false)
                    .WithKeepAlivePeriod(TimeSpan.FromMilliseconds(-1))
                    .WithCommunicationTimeout(TimeSpan.FromSeconds(20))
                    .Build();

                await MqttClient.ConnectAsync(mqttClientOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void MqttClientOnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            if (observerActive)
            {
                if (message.Contains(observerVariable))
                {
                    string outString = message.Substring(message.IndexOf(observerVariable));
                    outString = outString.Substring(outString.IndexOf("\"value\":"));
                    outString = outString.Substring(outString.IndexOf(":")+1, outString.IndexOf("}")- outString.IndexOf(":")-1);
                    Console.WriteLine("Value of " + observerVariable + ": " + outString);
                }
            }
            else if (checkActive)
            {
                try
                {
                    JObject.Parse(message);
                }
                catch (Exception ex)
                {
                    string outString = ex.Message;
                    Console.WriteLine(DateTime.Now + ": Error detected in message: " + outString);
                    var write = new StreamWriter(Path.Combine(Environment.CurrentDirectory, DateTime.Now.Ticks + ".json"));
                    write.Write(message);
                    write.Close();
                }
            }
            else
            {
                Console.WriteLine(message);
            }
        }
    }
}
