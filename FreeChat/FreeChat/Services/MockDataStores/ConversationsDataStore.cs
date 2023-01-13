using Models;
using NativeMedia;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace FreeChat.Services.MockDataStores
{
    public class ConversationsDataStore : IConversationsDataStore
    {
        List<Conversation> _conversations;
        IApiManager _apiManager;
        List<User> _users;
        User _currentUser;

        public ConversationsDataStore(User currentUser, List<User> users, IApiManager apiManager)
        {
            _apiManager = apiManager;
            _users = users;
            _currentUser = currentUser;
            var msgs = new string[]
            {
                "Hi, am ok and you ?",
                "Hi, what's up ?",
                "I was aware of that",
                "Get up.",
                "Hello how are you ?"
            };

            _conversations = new List<Conversation>();

            foreach (var user in _users)
            {
                int randomHours = new Random().Next(0, 24);
                _conversations.Add(new Conversation
                {
                    Id = Guid.NewGuid().ToString(),
                    LastMessage = new Message
                    {
                        Content = msgs[new Random().Next(0, msgs.Length - 1)],
                        ISent = true,
                        CreationDate = DateTime.UtcNow - TimeSpan.FromHours(randomHours),
                        Sender = user
                    },
                    Peer = user,
                    UserIds = new string[] { _currentUser.Id, user.Id }
                });
            }
            _conversations.OrderByDescending(c => c.LastMessage.CreationDate);
        }

        public async Task GetConversations()
        {
            _conversations = new List<Conversation>();

            string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
            var getConversationResponse = await _apiManager.GetConversations(token);
            var conversationResponse = await getConversationResponse.Content.ReadAsStringAsync();
            Debug.WriteLine("conversation response :>>");
            Console.WriteLine(conversationResponse);
            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            };
            var conversationJson = await Task.Run(() => JsonConvert.DeserializeObject<List<ConversationModel>>(conversationResponse, settings));
            Debug.WriteLine("conversation :>>>>>>>>>");
            Console.WriteLine(conversationJson);
            string userId = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.UserIdKey);
            for (int i = 0; i < conversationJson.Count; i++)
            {
                ConversationModel conversationModel = conversationJson[i];
                UserModel partner = conversationModel.Users.First(user => !user.Id.Equals(userId));
                int randomHours = new Random().Next(0, 24);
                Conversation conversation = new Conversation();
                conversation.Id = conversationModel.Id;
                conversation.UserIds = new string[] { conversationModel.Users[0].Id, conversationModel.Users[1].Id };
                MessageModel lastMessage = conversationModel.LastMessage;
                Debug.WriteLine("conversation content last message :>>>>>>>>>");
                Console.WriteLine(conversationModel.UpdatedAt);
                Console.WriteLine(lastMessage?.Content);
                Console.WriteLine(lastMessage?.UpdatedAt);
                Console.WriteLine(lastMessage?.ConversationId);
                String content = conversationModel?.LastMessage?.Content;
                int imageCount = 0;
                if (content != null)
                {
                    content = Helpers.StringHelper.ExtractContentFromMessage(content);
                    List<ImageModel> images = Helpers.StringHelper.ExtractImagesFromMessage(content);
                    imageCount = images.Count;
                }
                string imageString = imageCount > 0 ? "images" : "a image";
                conversation.LastMessage = new Message
                {
                    Content = conversationModel?.LastMessage?.Content != null ? (content.Equals("") ? $"Has sent {imageString}" : content) : "Say hello",
                    ISent = conversationModel?.LastMessage != null && conversationModel.LastMessage.SenderId.Equals(userId),
                    CreationDate = lastMessage != null ? DateTime.Parse(lastMessage.UpdatedAt, System.Globalization.CultureInfo.CurrentCulture) : DateTime.Parse(conversationModel.UpdatedAt, System.Globalization.CultureInfo.CurrentCulture),
                    Sender = new User(
                        partner.Profile?.Name ?? "Unknown",
                        partner.Profile?.Bio ?? "I like chatting online and making new friends on social media",
                        partner?.Profile?.ProfilePic ?? "john.jpg", 5, 230)
                    { Id = partner?.Id ?? Guid.NewGuid().ToString(), IsOnline = partner.IsOnline }
                };
                conversation.Peer = new User(
                    partner?.Profile?.Name ?? "Unknown",
                    partner.Profile?.Bio ?? "I like chatting online and making new friends on social media",
                partner?.Profile?.ProfilePic ?? "john.jpg", 5, 230)
                { Id = partner?.Id ?? Guid.NewGuid().ToString(), IsOnline = partner.IsOnline };
                _conversations.Add(conversation);
            }
        }

        public Task<bool> AddItemAsync(Conversation item)
        {
            _conversations.Add(item);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteItemAsync(string id)
        {
            var conversation = _conversations.Where(c => c.Id == id);
            if (!conversation.Any())
                return Task.FromResult(false);
            _conversations.Remove(conversation.FirstOrDefault());
            return Task.FromResult(true);
        }

        public async Task<IEnumerable<Conversation>> GetConversationsForUser(string userId)
        {
            await GetConversations();
            return await Task.FromResult(_conversations);
        }

        public Task<Conversation> GetItemAsync(string id)
        {
            return Task.FromResult(_conversations.Where(c => c.Id == id).FirstOrDefault());
        }

        public Task<IEnumerable<Conversation>> GetItemsAsync(bool forceRefresh = false)
        {
            return Task.FromResult((IEnumerable<Conversation>)_conversations);
        }

        public Task<bool> UpdateItemAsync(Conversation item)
        {
            var conv = _conversations.Where(c => c.Id == item.Id).FirstOrDefault();
            var i = _conversations.IndexOf(conv);
            _conversations[i] = conv;
            return Task.FromResult(true);
        }

        public Task<bool> AddItemAsync(Conversation item, IMediaFile[] files)
        {
            throw new NotImplementedException();
        }
    }
}
