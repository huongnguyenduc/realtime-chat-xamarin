using System;
using System.Collections.Generic;
using SocketIOClient;
using Models;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xamarin.Forms;
using FreeChat.Helpers;
using Xamarin.Essentials;

namespace FreeChat.Services
{
    public class SocketManager
    {
        public SocketManager()
        {
            Debug.WriteLine(":>> created socketio");
            Console.WriteLine(":>> created socketio");
        }

        SocketIO client;

        public async void initSocketIO(string token)
        {
            Debug.WriteLine(":>> init socket io");
            Console.WriteLine(token);
            if (client != null && client.Connected)
            {
                Debug.WriteLine("disposed");
                Console.WriteLine(token);
                client.Dispose();
            }
            Debug.WriteLine(":>> init data");
            Console.WriteLine(":>> init data");
            Debug.WriteLine("token");
            Console.WriteLine(token);
            SocketIOOptions options = new SocketIOOptions();

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", $"Bearer {token}");
            options.ExtraHeaders = headers;
            Dictionary<string, string> queries = new Dictionary<string, string>();
            options.Query = queries;

            client = new SocketIO($"{Config.ApiUrl}/message", options);

            Debug.WriteLine(":>> post connect");
            Console.WriteLine(":>> post connect");

            await client.ConnectAsync();

            Debug.WriteLine(":>> connected");
            Console.WriteLine(":>> connected");

            client.OnConnected += (sender, e) =>
            {
                Debug.WriteLine(":>> initiated data");
                Console.WriteLine(":>> initiated data");
            };

            client.On("exception", (data) =>
            {
                Debug.WriteLine("SocketIO error :((");
                Debug.WriteLine(data);
            });

            client.On(Constants.MessageEvent, async (data) =>
            {
                Debug.WriteLine("receive message");
                Debug.WriteLine(data);
                Console.WriteLine(data);
                var response = data.GetValue().ToString();
                var message = await Task.Run(() => JsonConvert.DeserializeObject<IncomeMessage>(response));
                MessagingCenter.Send<SocketManager, IncomeMessage>(this, Constants.ReceiveMessage, message);
            });

            client.On(Constants.OnlineEvent, async (data) =>
            {
                Debug.WriteLine("receive online response");
                Debug.WriteLine("2 ne hehe");
                Debug.WriteLine(data);
                Console.WriteLine(data);
                var response = data.GetValue().ToString();
                var message = await Task.Run(() => JsonConvert.DeserializeObject<OnlineModel>(response));
                Debug.WriteLine("3 ne hehe");
                MessagingCenter.Send<SocketManager, OnlineModel>(this, Constants.ReceiveOnline, message);
                Debug.WriteLine("4 ne hehe");
            });

            client.On(Constants.TypingEvent, async (data) =>
            {
                Debug.WriteLine("receive message");
                Debug.WriteLine(data);
                Console.WriteLine(data);
                var response = data.GetValue().ToString();
                var message = await Task.Run(() => JsonConvert.DeserializeObject<IncomingTypingModel>(response));
                MessagingCenter.Send<SocketManager, IncomingTypingModel>(this, Constants.ReceiveTyping, message);
            });
            Debug.WriteLine(":>> init done");
            Console.WriteLine(":>> init done");
        }

        public async Task sendMessage(OutgoingMessage message)
        {
            if (client == null || client.Disconnected) return;
            Debug.WriteLine(":>> send message");
            Console.WriteLine(":>> send message");
            Debug.WriteLine(message.content);
            Console.WriteLine(message.conversationId);
            Console.WriteLine(message.replyToId);
            Console.WriteLine(":>> send message");
            await client.EmitAsync(Constants.MessageEvent, message);
            Debug.WriteLine(":>> sent message");
            Console.WriteLine(":>> sent message");
        }

        public async Task sendTyping(OutgoingTypingModel message)
        {
            if (client == null || client.Disconnected) return;
            Debug.WriteLine(":>> send typing");
            Console.WriteLine(":>> send typing");
            Debug.WriteLine(message.isTyping);
            Console.WriteLine(message.conversationId);
            Console.WriteLine(":>> send typing");
            await client.EmitAsync(Constants.TypingEvent, message);
            Debug.WriteLine(":>> sent typing");
        }

        async public void dispose()
        {
            Debug.WriteLine(":>> dispose");
            Console.WriteLine(":>> dispose");
            if (client != null && client.Connected)
            {
                await client.DisconnectAsync();
            }
            Console.WriteLine(":>> disconnected");
            if (client != null)
            {
                client.Dispose();
            }
        }
    }
}

