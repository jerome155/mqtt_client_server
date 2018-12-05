using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;
using Newtonsoft.Json;

namespace MqttServerTest
{
    class RetainedMessageHandler : IMqttServerStorage
    {
        private readonly string _path = System.IO.Directory.GetCurrentDirectory() + "//MQTTStorage//";
        private readonly string _fileName = "tempStore.json";
        public async Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
        {
            CreateDestinationPath();
            IList<MqttApplicationMessage> msgs = await LoadRetainedMessagesAsync();
            foreach (MqttApplicationMessage msg in msgs)
            {
                messages.Add(msg);
            }
            File.WriteAllText(_path + _fileName, JsonConvert.SerializeObject(messages));
        }

        public Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
        {
            CreateDestinationPath();
            IList<MqttApplicationMessage> retainedMessages;
            if (File.Exists(_path + _fileName))
            {
                var json = File.ReadAllText(_path + _fileName);
                retainedMessages = JsonConvert.DeserializeObject<List<MqttApplicationMessage>>(json);
            }
            else
            {
                retainedMessages = new List<MqttApplicationMessage>();
            }

            return Task.FromResult(retainedMessages);
        }

        private void CreateDestinationPath()
        {
            if (!System.IO.Directory.Exists(_path))
            {
                System.IO.Directory.CreateDirectory(_path);
            }
        }
    }
}
