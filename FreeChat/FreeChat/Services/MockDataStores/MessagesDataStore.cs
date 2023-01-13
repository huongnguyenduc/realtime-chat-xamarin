using FreeChat.Helpers;
using Models;
using NativeMedia;
using Newtonsoft.Json;
using Refit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace FreeChat.Services.MockDataStores
{
    public class MessagesDataStore : IMessageDataStore
    {
        List<Message> _messages;
        IApiManager _apiManager;
        SocketManager _socketManager;

        public MessagesDataStore(Conversation conversation, IApiManager apiManager, SocketManager socketManager)
        {
            _socketManager = socketManager;
            _apiManager = apiManager;
            _messages = new List<Message>();

            _messages.Add(new Message
            {
                ConversationId = conversation.Id,
                Id = Guid.NewGuid().ToString(),
                Content = "I was at your office yesterday.",
                CreationDate = DateTime.Now - TimeSpan.FromDays(1),
                ISent = false,
                SenderId = conversation.Peer.Id,
                Sender = conversation.Peer
            });
            _messages.Add(new Message
            {
                ConversationId = conversation.Id,
                Id = Guid.NewGuid().ToString(),
                Content = "Ooh really ?",
                CreationDate = DateTime.Now - TimeSpan.FromDays(1),
                ISent = true,
                SenderId = conversation.UserIds[0],
            });
            _messages.Add(new Message
            {
                ConversationId = conversation.Id,
                Id = Guid.NewGuid().ToString(),
                ISentPreviousMessage = true,
                Content = "Yeah. But you were not arround",
                CreationDate = DateTime.Now - TimeSpan.FromMinutes(5),
                ISent = false,
                SenderId = conversation.Peer.Id,
                Sender = conversation.Peer
            });
            _messages.Add(new Message
            {
                ConversationId = conversation.Id,
                Id = Guid.NewGuid().ToString(),
                ISentPreviousMessage = false,
                Content = "Yeah I was not arround I left early yesterday",
                CreationDate = DateTime.Now - TimeSpan.FromMinutes(2),
                ISent = true,
                SenderId = conversation.Peer.Id,
                Sender = conversation.Peer,
                ReplyTo = _messages[_messages.Count - 3]
            });
            _messages.Add(new Message
            {
                ConversationId = conversation.Id,
                Id = Guid.NewGuid().ToString(),
                ISentPreviousMessage = true,
                Content = "I sent this message",
                CreationDate = DateTime.Now - TimeSpan.FromMinutes(1),
                ISent = true,
                SenderId = conversation.Peer.Id,
                Sender = conversation.Peer,
                ReplyTo = _messages[_messages.Count - 2]
            });
            _messages.Add(new Message
            {
                ConversationId = conversation.Id,
                Id = Guid.NewGuid().ToString(),
                ISentPreviousMessage = true,
                Content = "I called you, and I left you a message did you see it ?",
                CreationDate = DateTime.Now - TimeSpan.FromMinutes(1),
                ISent = false,
                SenderId = conversation.Peer.Id,
                Sender = conversation.Peer,
                ReplyTo = _messages[_messages.Count - 2]
            });
            _messages.Add(new Message
            {
                ConversationId = conversation.Id,
                Id = Guid.NewGuid().ToString(),
                ISentPreviousMessage = false,
                Content = "I called you, and I left you a message did you see it ?",
                CreationDate = DateTime.Now,
                ISent = false,
                SenderId = conversation.Peer.Id,
                Sender = conversation.Peer,
                ReplyTo = _messages[_messages.Count - 2]
            });
            conversation.LastMessage = _messages.Last();
        }

        public async Task GetMessages(string conversationId)
        {
            _messages = new List<Message>();
            string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
            var getConversationResponse = await _apiManager.GetConversation(token, conversationId);
            var conversationResponse = await getConversationResponse.Content.ReadAsStringAsync();
            Debug.WriteLine("conversation response ne :>>");
            Console.WriteLine(conversationResponse);
            var conversationJson = await Task.Run(() => JsonConvert.DeserializeObject<ConversationModel>(conversationResponse));

            string userId = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.UserIdKey);
            var getMessageResponse = await _apiManager.GetMessages(token, conversationId);
            var messageResponse = await getMessageResponse.Content.ReadAsStringAsync();
            Debug.WriteLine("message response :>>");
            Console.WriteLine(messageResponse);
            var messageJson = await Task.Run(() => JsonConvert.DeserializeObject<List<MessageModel>>(messageResponse));
            Debug.WriteLine("message123 :>>>>>>>>>");
            Console.WriteLine(messageJson);
            for (int i = 0; i < messageJson.Count; i++)
            {
                MessageModel message = messageJson[i];
                UserModel sender = conversationJson.Users.First(user => user.Id.Equals(message.SenderId));
                Message replyMessage = null;
                if (message.ReplyToId != null)
                {
                    for (int j = 0; j < _messages.Count; j++)
                    {
                        if (_messages[j].Id.Equals(message.ReplyToId))
                        {
                            replyMessage = _messages[j];
                            break;
                        }
                    }
                }
                List<ImageModel> images = StringHelper.ExtractImagesFromMessage(messageJson[i].Content);
                String content = StringHelper.ExtractContentFromMessage(messageJson[i].Content);
                if (replyMessage != null)
                {
                    _messages.Add(new Message
                    {
                        ReplyTo = replyMessage,
                        ConversationId = conversationId,
                        Id = message.Id,
                        ISentPreviousMessage = i > 0 && _messages[i - 1].ISent,
                        Content = content,
                        CreationDate = DateTime.Parse(message.CreatedAt, System.Globalization.CultureInfo.CurrentCulture),
                        ISent = message.SenderId.Equals(userId),
                        SenderId = message.SenderId,
                        Images = images,
                        Sender = new User(
                        sender?.Profile?.Name ?? "Unknown",
                        sender?.Profile?.Bio ?? "I like chatting online and making new friends on social media",
                        sender?.Profile?.ProfilePic ?? "john.jpg", 5, 230)
                        { Id = sender.Id, IsOnline = sender.IsOnline }
                    });
                }
                else
                {
                    _messages.Add(new Message
                    {
                        ConversationId = conversationId,
                        Id = message.Id,
                        ISentPreviousMessage = i > 0 && _messages[i - 1].ISent,
                        Content = content,
                        CreationDate = DateTime.Parse(message.CreatedAt, System.Globalization.CultureInfo.CurrentCulture),
                        ISent = message.SenderId.Equals(userId),
                        SenderId = message.SenderId,
                        Images = images,
                        Sender = new User(
                        sender?.Profile?.Name ?? "Unknown",
                        sender?.Profile?.Bio ?? "I like chatting online and making new friends on social media",
                        sender?.Profile?.ProfilePic ?? "john.jpg", 5, 230)
                        { Id = sender.Id, IsOnline = sender.IsOnline }
                    });
                }

            }
        }

        public async Task<bool> AddItemAsync(Message item, IMediaFile[] files = null)
        {
            _messages.Add(item);
            //OutgoingMessage message = null;
            //if (item.ReplyTo != null)
            //{
            //    message = new OutgoingMessage { content = item.Content, conversationId = item.ConversationId, replyToId = item.ReplyTo.Id };
            //}
            //else
            //{
            //    message = new OutgoingMessage { content = item.Content, conversationId = item.ConversationId };
            //}
            //Console.WriteLine("Prepare send");
            //List<ImageModel> images = new List<ImageModel>();
            //if (item.ISent)
            //{
            //    if (files != null)
            //    {
            //        string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
            //        if (token == null || token == "")
            //        {
            //            await AppShell.Current.Navigation.PopToRootAsync();
            //            return false;
            //        }

            //        foreach (IMediaFile imageFile in files)
            //        {
            //            if (imageFile != null)
            //            {
            //                var fileName = imageFile.NameWithoutExtension;
            //                var contentType = imageFile.ContentType;
            //                var stream = await imageFile.OpenReadAsync();

            //                var uploadFileResponse = await _apiManager.UploadFile(new StreamPart(stream, fileName, contentType));

            //                if (uploadFileResponse.IsSuccessStatusCode)
            //                {

            //                    var fileResponse = await uploadFileResponse.Content.ReadAsStringAsync();
            //                    var fileJson = await Task.Run(() => JsonConvert.DeserializeObject<FileModel>(fileResponse));

            //                    ImageModel image = new ImageModel { Name = fileName, Url = fileJson.Url };
            //                    images.Add(image);
            //                }
            //            }
            //        }


            //    }
            //    if (images.Count > 0)
            //    {
            //        message.content = StringHelper.CreateMessageWithImageLink(message.content, images);
            //    }
            //    await _socketManager.sendMessage(message);
            //}
            Console.WriteLine(":>> sent from data store");
            return await Task.FromResult(true);
        }

        public async Task<bool> AddItemAsync(Message item)
        {
            _messages.Add(item);
            //OutgoingMessage message = null;
            //if (item.ReplyTo != null)
            //{
            //    message = new OutgoingMessage { content = item.Content, conversationId = item.ConversationId, replyToId = item.ReplyTo.Id };
            //}
            //else
            //{
            //    message = new OutgoingMessage { content = item.Content, conversationId = item.ConversationId };
            //}
            //Console.WriteLine("Prepare send");
            //if (item.ISent)
            //{
            //    await _socketManager.sendMessage(message);
            //}
            Console.WriteLine(":>> sent from data store");
            return await Task.FromResult(true);
        }


        public Task<bool> DeleteItemAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<Message> GetItemAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Message>> GetItemsAsync(bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Message>> GetMessagesForConversation(string conversationId)
        {
            await GetMessages(conversationId);
            return await Task.FromResult(_messages.Where(m => m.ConversationId == conversationId));
        }

        public Task<bool> UpdateItemAsync(Message item)
        {
            throw new NotImplementedException();
        }
    }
}
