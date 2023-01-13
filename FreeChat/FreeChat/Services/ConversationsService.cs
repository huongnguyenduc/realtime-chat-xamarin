using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Models;
using NativeMedia;
using Newtonsoft.Json;
using Xamarin.Essentials;

namespace FreeChat.Services
{
    public class ConversationsService : IConversationsDataStore
    {
        IApiManager _apiManager;
        public ConversationsService(IApiManager apiManager)
        {
            _apiManager = apiManager;
        }

        public Task<bool> AddItemAsync(Conversation item)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AddItemAsync(Conversation item, IMediaFile[] files)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteItemAsync(string id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Conversation>> GetConversationsForUser(string userId)
        {
            List<Conversation> conversations = new List<Conversation>();
            string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
            var getConversationResponse = await _apiManager.GetConversations(token);
            var conversationResponse = await getConversationResponse.Content.ReadAsStringAsync();
            Debug.WriteLine("conversation response :>>");
            Console.WriteLine(conversationResponse);
            var conversationJson = await Task.Run(() => JsonConvert.DeserializeObject<List<ConversationModel>>(conversationResponse));
            Debug.WriteLine("conversation :>>");
            Console.WriteLine(conversationJson);
            for (int i = 0; i < conversationJson.Count; i++)
            {
                int randomHours = new Random().Next(0, 24);
                Conversation conversation = new Conversation();
                conversation.Id = conversationJson[i].Id;
                conversation.LastMessage = new Message
                {
                    Content = conversationJson[i].LastMessage.Content ?? "Say hello",
                    ISent = true,
                    CreationDate = DateTime.UtcNow - TimeSpan.FromHours(randomHours),
                    Sender = new User("Alfredo Stephano", "I like chatting online and making new friends on social media",
                "alfredo.jpg", 5, 230)
                };
                conversation.Peer = new User("Alfredo Stephano", "I like chatting online and making new friends on social media",
                "alfredo.jpg", 5, 230);
                conversations.Add(conversation);
            }

            return await Task.FromResult(conversations);
        }

        public Task<Conversation> GetItemAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Conversation>> GetItemsAsync(bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateItemAsync(Conversation item)
        {
            throw new NotImplementedException();
        }
    }
}

