using FreeChat.Helpers;
using FreeChat.Helpers.MyEventArgs;
using FreeChat.Resources;
using FreeChat.Services;
using FreeChat.ViewModels.Helpers;
using FreeChat.Views.Pages;
using Humanizer;
using Models;
using NativeMedia;
using Newtonsoft.Json;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using ReactiveUI;
using Refit;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace FreeChat.ViewModels
{
    [QueryProperty("ConversationId", "conversation_id")]
    public class MessagesViewModel : BaseViewModel
    {
        private CancellationTokenSource _throttleCts = new CancellationTokenSource();
        public string ConversationId { get; set; }
        Conversation _conversation;
        Services.Navigation.INavigationService _navigationService;
        public Conversation CurrentConversation
        {
            get => _conversation;
            set => this.RaiseAndSetIfChanged(ref _conversation, value);
        }
        public ICommand SendMessageCommand { get; private set; }
        public ICommand MessageSwippedCommand { get; private set; }
        public ICommand CancelReplyCommand { get; private set; }
        public ICommand ReplyMessageSelectedCommand { get; private set; }
        public ICommand ImageMessageSelectedCommand { get; private set; }
        public ICommand PickImagesCommand { get; private set; }
        public ICommand CallCommand { get; private set; }


        private bool _isTyping;

        public bool IsTyping
        {
            get { return _isTyping; }
            set { this.RaiseAndSetIfChanged(ref _isTyping, value); }
        }

        private bool _isSending;

        public bool IsSending
        {
            get { return _isSending; }
            set { this.RaiseAndSetIfChanged(ref _isSending, value); }
        }

        private bool _isMeTyping;

        public bool IsMeTyping
        {
            get { return _isMeTyping; }
            set { this.RaiseAndSetIfChanged(ref _isMeTyping, value); }
        }

        Message _replyMessage;
        public Message ReplyMessage
        {
            get => _replyMessage;
            set => this.RaiseAndSetIfChanged(ref _replyMessage, value);
        }
        ObservableCollection<MessagesGroup> _messagesGroups;
        public ObservableCollection<MessagesGroup> Messages
        {
            get => _messagesGroups;
            set => this.RaiseAndSetIfChanged(ref _messagesGroups, value);
        }
        private List<Message> _messages;

        private string _currentMessage;
        public string CurrentMessage
        {
            get { return _currentMessage; }
            set
            {
                DebouncedTyping(ConversationId);
                this.RaiseAndSetIfChanged(ref _currentMessage, value);
            }
        }

        public MessagesViewModel(IDataStore<User> userDataStore, IConversationsDataStore convDataStore,
            IMessageDataStore messageDataStore, IApiManager apiManager, SocketManager socketManager, Services.Navigation.INavigationService navigationService) : base(userDataStore, convDataStore, messageDataStore, apiManager, socketManager)
        {
            _navigationService = navigationService;
            ReplyMessageSelectedCommand = ReactiveCommand.Create<Message>(ReplyMessageSelected);
            ImageMessageSelectedCommand = ReactiveCommand.Create<ImageModel>(ImageMessageSelected);
            MessageSwippedCommand = ReactiveCommand.Create<Message>(MessageSwiped);
            SendMessageCommand = ReactiveCommand.CreateFromTask(SendMeessage, this.WhenAnyValue(vm => vm.CurrentMessage, curm => !String.IsNullOrEmpty(curm)));
            CancelReplyCommand = ReactiveCommand.Create(CancelReply);
            PickImagesCommand = ReactiveCommand.Create(ExecutePickImagesCommand);
            CallCommand = ReactiveCommand.Create(ExecuteCallCommand);
            _messages = new List<Message>();
        }

        private async Task DebouncedTyping(string conversationId)
        {
            try
            {
                if (!IsMeTyping)
                {
                    IsMeTyping = true;
                    OutgoingTypingModel startTypingData = new OutgoingTypingModel { conversationId = conversationId, isTyping = true };
                    await SocketManager.sendTyping(startTypingData);
                }

                Interlocked.Exchange(ref _throttleCts, new CancellationTokenSource()).Cancel();
                //NOTE THE 500 HERE - WHICH IS THE TIME TO WAIT
                await Task.Delay(TimeSpan.FromMilliseconds(3000), _throttleCts.Token)

                    //NOTICE THE "ACTUAL" SEARCH METHOD HERE
                    .ContinueWith(async task =>
                    {
                        IsMeTyping = false;
                        OutgoingTypingModel stopTypingData = new OutgoingTypingModel { conversationId = conversationId, isTyping = false };
                        await SocketManager.sendTyping(stopTypingData);
                    },
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnRanToCompletion,
                        TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch
            {
                //Ignore any Threading errors
            }
        }

        public void CancelReply()
        {
            ReplyMessage = null;
            MessagingCenter.Send<IViewModel, MyFocusEventArgs>(this, Constants.ShowKeyboard, new MyFocusEventArgs { IsFocused = false });
        }

        private void ReplyMessageSelected(Message message)
        {
            ScrollToMessage(message);
        }

        private async void ImageMessageSelected(ImageModel image)
        {
            Console.WriteLine("hellio");
            Console.WriteLine(image?.Url);
            var page = new ImagePage();
            page.BindingContext = image;

            await PopupNavigation.Instance.PushAsync(page);
        }

        public async override Task Initialize()
        {
            CurrentConversation = await _conversationDataStore.GetItemAsync(ConversationId);
            var messages = await _messageDataStore.GetMessagesForConversation(ConversationId);

            _messages.AddRange(messages);
            var messagesGroups = _messages.GroupBy(m => m.CreationDate.Day)
                .Select(grp =>
                {
                    var messagesGrp = grp.ToList().OrderBy(m => m.CreationDate);
                    var msg = messagesGrp.First();
                    var date = msg.CreationDate.Date;
                    var dayDiff = DateTime.Now.Day - date.Day;
                    string groupHeader = string.Empty;

                    if (dayDiff == 0)
                        groupHeader = TextResources.Today;
                    else if (dayDiff == 1)
                        groupHeader = TextResources.Yesterday;
                    else groupHeader = date.ToString("dd-MM-yyyy");

                    return new MessagesGroup
                    (
                        dateTime: date,
                        groupHeader: groupHeader,
                        messages: new ObservableCollection<Message>(messagesGrp)
                    );
                })
                .OrderBy(m => m.DateTime.Date)
                .ToList();

            Messages = new ObservableCollection<MessagesGroup>(messagesGroups);

            if (Messages.Any())
                ScrollToMessage(Messages?.Last()?.Last());

            MessagingCenter.Subscribe<SocketManager, IncomeMessage>(SocketManager, Constants.ReceiveMessage, async (s, args) =>
                {
                    await ReceiveMessage(args);
                });
            MessagingCenter.Subscribe<SocketManager, IncomingTypingModel>(SocketManager, Constants.ReceiveTyping, async (s, args) =>
            {
                ReceiveTyping(args);
            });

            MessagingCenter.Subscribe<SocketManager, OnlineModel>(SocketManager, Constants.ReceiveOnline, async (s, args) =>
            {
                UpdateOnline(args);
            });
        }

        void UpdateOnline(OnlineModel model)
        {
            if (model.UserId.Equals(CurrentConversation.Peer.Id))
            {
                Conversation conversation = new Conversation
                {
                    Id = CurrentConversation.Id,
                    LastMessage = CurrentConversation.LastMessage,
                    UserIds = CurrentConversation.UserIds,
                    CreationDate = CurrentConversation.CreationDate,
                    Peer = CurrentConversation.Peer
                };
                conversation.Peer.IsOnline = model.IsOnline;
                CurrentConversation = conversation;
            }
        }

        void MessageSwiped(Message message)
        {
            ReplyMessage = message;
            MessagingCenter.Send<IViewModel, MyFocusEventArgs>(this, Constants.ShowKeyboard, new MyFocusEventArgs { IsFocused = true });
        }

        void ScrollToMessage(Message message)
        {
            MessagingCenter.Send<IViewModel, ScrollToItemEventArgs>(this, Constants.ScrollToItem, new ScrollToItemEventArgs { Item = message });
        }

        private async Task CheckPermissionsAndStart()
        {
            if (Device.RuntimePlatform != Device.macOS)
            {
                var permissionsToRequest = new List<Permission>();
                var cameraPermissionState = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Camera);
                if (cameraPermissionState != Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                    permissionsToRequest.Add(Permission.Camera);

                var microphonePermissionState = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Microphone);
                if (microphonePermissionState != Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                    permissionsToRequest.Add(Permission.Microphone);

                if (permissionsToRequest.Count > 0)
                    await CrossPermissions.Current.RequestPermissionsAsync(permissionsToRequest.ToArray());
            }
        }

        public async Task ExecuteCallCommand()
        {
            Console.WriteLine("hellio11111");
            await CheckPermissionsAndStart();
            Console.WriteLine("hellio22222");
            //await AppShell.Current.Navigation.PushModalAsync(new RoomPage());
            await _navigationService.GotoPage($"{FreeChat.Services.Navigation.Constants.CallPageUrl}?room_id={"hihi"}");
            Console.WriteLine("hellio3333333");
        }

        //async Task SendMeessage()
        //{
        //    if (!Messages.Any())
        //    {
        //        MessagesGroup messageGroup = new MessagesGroup
        //            (
        //                dateTime: DateTime.Now,
        //                groupHeader: TextResources.Today,
        //                messages: new ObservableCollection<Message>()
        //            );
        //        List<MessagesGroup> messagesGroups = new List<MessagesGroup>();
        //        messagesGroups.Add(messageGroup);
        //        Messages = new ObservableCollection<MessagesGroup>(messagesGroups);
        //    }

        //    var message = new Message
        //    {
        //        Content = CurrentMessage,
        //        ReplyTo = ReplyMessage,
        //        CreationDate = DateTime.Now,
        //        Sender = AppLocator.CurrentUser,
        //        //ISentPreviousMessage = Messages.Any() ? (Messages?.Last()?.Last()?.ISent != null ? Messages?.Last()?.Last()?.ISent ?? false : false) : false,
        //        ISentPreviousMessage = false,
        //        ISent = true,
        //        ConversationId = CurrentConversation.Id,
        //        SenderId = AppLocator.CurrentUserId
        //    };
        //    CurrentConversation.LastMessage = message;
        //    await _conversationDataStore.UpdateItemAsync(CurrentConversation);
        //    CurrentMessage = string.Empty;
        //    Messages?.Last()?.Add(message);
        //    _messages.Add(message);
        //    await _messageDataStore.AddItemAsync(message);
        //    ReplyMessage = null;
        //    ScrollToMessage(message);
        //}

        //async Task ReceiveMessage(IncomeMessage incomeMessage)
        //{
        //    if (!Messages.Any())
        //    {
        //        MessagesGroup messageGroup = new MessagesGroup
        //            (
        //                dateTime: DateTime.Now,
        //                groupHeader: TextResources.Today,
        //                messages: new ObservableCollection<Message>()
        //            );
        //        List<MessagesGroup> messagesGroups = new List<MessagesGroup>();
        //        messagesGroups.Add(messageGroup);
        //        Messages = new ObservableCollection<MessagesGroup>(messagesGroups);
        //        Console.WriteLine(":>> 0.5 ne");
        //    }
        //    IsTyping = true;
        //    Message replyMessage = null;
        //    if (incomeMessage.ReplyToId != null)
        //    {
        //        for (int i = 0; i < _messages.Count; i++)
        //        {
        //            if (_messages[i].Id.Equals(incomeMessage.ReplyToId))
        //            {
        //                replyMessage = _messages[i];
        //                break;
        //            }
        //        }
        //    }
        //    Message message = null;
        //    if (replyMessage != null)
        //    {
        //        message = new Message
        //        {
        //            Content = incomeMessage.Content,
        //            CreationDate = DateTime.Now,
        //            Sender = CurrentConversation.Peer,
        //            //ISentPreviousMessage = Messages.Last().Last().ISent,
        //            ISent = false,
        //            ConversationId = CurrentConversation.Id,
        //            SenderId = CurrentConversation.Peer.Id,
        //            ReplyTo = replyMessage
        //        };
        //    }
        //    else
        //    {
        //        message = new Message
        //        {
        //            Content = incomeMessage.Content,
        //            CreationDate = DateTime.Now,
        //            Sender = CurrentConversation.Peer,
        //            //ISentPreviousMessage = Messages.Last().Last().ISent,
        //            ISent = false,
        //            ConversationId = CurrentConversation.Id,
        //            SenderId = CurrentConversation.Peer.Id,
        //        };
        //    }

        //    Messages.Last().Add(message);
        //    _messages.Add(message);
        //    CurrentConversation.LastMessage = message;
        //    //await _conversationDataStore.UpdateItemAsync(CurrentConversation);
        //    await _messageDataStore.AddItemAsync(message);
        //    ScrollToMessage(message);
        //    IsTyping = false;
        //}

        async Task SendMeessage()
        {
            IsSending = true;
            Console.WriteLine("1");
            OutgoingMessage message = null;
            if (ReplyMessage != null)
            {
                Console.WriteLine("2");
                message = new OutgoingMessage { content = CurrentMessage, conversationId = CurrentConversation.Id, replyToId = ReplyMessage.Id };
            }
            else
            {
                Console.WriteLine("3");
                message = new OutgoingMessage { content = CurrentMessage, conversationId = CurrentConversation.Id };
            }
            Console.WriteLine("4");
            await this.SocketManager.sendMessage(message);
            Console.WriteLine("5");
            CurrentMessage = string.Empty;
            Console.WriteLine("6");
            ReplyMessage = null;
            Console.WriteLine("7");
            IsSending = false;
        }

        async Task ReceiveMessage(IncomeMessage incomeMessage)
        {
            if (!Messages.Any())
            {
                MessagesGroup messageGroup = new MessagesGroup
                    (
                        dateTime: DateTime.Now,
                        groupHeader: TextResources.Today,
                        messages: new ObservableCollection<Message>()
                    );
                List<MessagesGroup> messagesGroups = new List<MessagesGroup>();
                messagesGroups.Add(messageGroup);
                Messages = new ObservableCollection<MessagesGroup>(messagesGroups);
                Console.WriteLine(":>> 0.5 ne");
            }
            string userId = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.UserIdKey);
            Message replyMessage = null;
            if (incomeMessage?.ReplyToId != null)
            {
                for (int i = 0; i < _messages.Count; i++)
                {
                    if (_messages[i].Id.Equals(incomeMessage.ReplyToId))
                    {
                        replyMessage = _messages[i];
                        break;
                    }
                }
            }
            List<ImageModel> images = StringHelper.ExtractImagesFromMessage(incomeMessage.Content);
            String content = StringHelper.ExtractContentFromMessage(incomeMessage.Content);
            Message message = null;
            if (replyMessage != null)
            {
                message = new Message
                {
                    Id = incomeMessage.Id,
                    Content = content,
                    CreationDate = DateTime.Now,
                    Sender = CurrentConversation.Peer,
                    //ISentPreviousMessage = Messages.Last().Last().ISent,
                    ISent = incomeMessage.UserId.Equals(userId),
                    ConversationId = CurrentConversation.Id,
                    SenderId = CurrentConversation.Peer.Id,
                    ReplyTo = replyMessage,
                    Images = images,
                };
            }
            else
            {
                message = new Message
                {
                    Id = incomeMessage.Id,
                    Content = content,
                    CreationDate = DateTime.Now,
                    Sender = CurrentConversation.Peer,
                    //ISentPreviousMessage = Messages.Last().Last().ISent,
                    ISent = incomeMessage.UserId.Equals(userId),
                    ConversationId = CurrentConversation.Id,
                    SenderId = CurrentConversation.Peer.Id,
                    Images = images,
                };
            }

            Messages.Last().Add(message);
            _messages.Add(message);
            CurrentConversation.LastMessage = message;
            //await _conversationDataStore.UpdateItemAsync(CurrentConversation);
            await _messageDataStore.AddItemAsync(message);
            ScrollToMessage(message);
        }

        public void ReceiveTyping(IncomingTypingModel incomeMessage)
        {
            if (incomeMessage.UserId.Equals(CurrentConversation.Peer.Id))
            {
                IsTyping = incomeMessage.IsTyping;
            }
        }

        public override Task Stop()
        {
            return Task.CompletedTask;
        }

        //public async Task ExecutePickImagesCommand()
        //{
        //    var cts = new CancellationTokenSource();
        //    IMediaFile[] files = null;

        //    try
        //    {
        //        var request = new MediaPickRequest(20, MediaFileType.Image, MediaFileType.Video)
        //        {
        //            PresentationSourceBounds = System.Drawing.Rectangle.Empty,
        //            UseCreateChooser = true,
        //            Title = "Select image to send"
        //        };

        //        cts.CancelAfter(TimeSpan.FromMinutes(5));

        //        var results = await MediaGallery.PickAsync(request, cts.Token);
        //        files = results?.Files?.ToArray();
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        // handling a cancellation request
        //    }
        //    catch (Exception)
        //    {
        //        // handling other exceptions
        //    }
        //    finally
        //    {
        //        cts.Dispose();
        //    }


        //    if (files == null)
        //        return;

        //    List<ImageModel> images = new List<ImageModel>();
        //    foreach (var file in files)
        //    {
        //        var fileName = file.NameWithoutExtension; //Can return an null or empty value
        //        var stream = await file.OpenReadAsync();
        //        string filePath = await FilesHelper.SaveToCacheAsync(stream, fileName);
        //        images.Add(new ImageModel { Name = fileName, Url = filePath });
        //        //...
        //        //file.Dispose();
        //    }

        //    if (!Messages.Any())
        //    {
        //        MessagesGroup messageGroup = new MessagesGroup
        //            (
        //                dateTime: DateTime.Now,
        //                groupHeader: TextResources.Today,
        //                messages: new ObservableCollection<Message>()
        //            );
        //        List<MessagesGroup> messagesGroups = new List<MessagesGroup>();
        //        messagesGroups.Add(messageGroup);
        //        Messages = new ObservableCollection<MessagesGroup>(messagesGroups);
        //    }

        //    var message = new Message
        //    {
        //        Content = CurrentMessage,
        //        ReplyTo = ReplyMessage,
        //        CreationDate = DateTime.Now,
        //        Sender = AppLocator.CurrentUser,
        //        //ISentPreviousMessage = Messages.Any() ? (Messages?.Last()?.Last()?.ISent != null ? Messages?.Last()?.Last()?.ISent ?? false : false) : false,
        //        ISentPreviousMessage = false,
        //        ISent = true,
        //        ConversationId = CurrentConversation.Id,
        //        SenderId = AppLocator.CurrentUserId
        //    };
        //    CurrentConversation.LastMessage = message;
        //    await _conversationDataStore.UpdateItemAsync(CurrentConversation);
        //    CurrentMessage = string.Empty;
        //    Messages?.Last()?.Add(message);
        //    _messages.Add(message);
        //    await _messageDataStore.AddItemAsync(message, files);
        //    ReplyMessage = null;
        //    ScrollToMessage(message);
        //}

        public async Task ExecutePickImagesCommand()
        {
            IsSending = true;
            var cts = new CancellationTokenSource();
            IMediaFile[] files = null;

            try
            {
                var request = new MediaPickRequest(20, MediaFileType.Image, MediaFileType.Video)
                {
                    PresentationSourceBounds = System.Drawing.Rectangle.Empty,
                    UseCreateChooser = true,
                    Title = "Select images to send"
                };

                cts.CancelAfter(TimeSpan.FromMinutes(5));

                var results = await MediaGallery.PickAsync(request, cts.Token);
                files = results?.Files?.ToArray();
            }
            catch (OperationCanceledException)
            {
                // handling a cancellation request
            }
            catch (Exception)
            {
                // handling other exceptions
            }
            finally
            {
                cts.Dispose();
            }


            if (files == null || files.Count() == 0)
                return;

            List<ImageModel> images = new List<ImageModel>();
            foreach (var file in files)
            {
                var fileName = file.NameWithoutExtension; //Can return an null or empty value
                var stream = await file.OpenReadAsync();
                string filePath = await FilesHelper.SaveToCacheAsync(stream, fileName);
                images.Add(new ImageModel { Name = fileName, Url = filePath });
                //...
                //file.Dispose();
            }

            OutgoingMessage message = null;
            if (ReplyMessage?.ReplyTo != null)
            {
                message = new OutgoingMessage { content = CurrentMessage, conversationId = CurrentConversation.Id, replyToId = ReplyMessage.ReplyTo.Id };
            }
            else
            {
                message = new OutgoingMessage { content = CurrentMessage, conversationId = CurrentConversation.Id };
            }

            if (files != null)
            {
                string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
                if (token == null || token == "")
                {
                    await AppShell.Current.Navigation.PopToRootAsync();
                    return;
                }

                foreach (IMediaFile imageFile in files)
                {
                    if (imageFile != null)
                    {
                        var fileName = imageFile.NameWithoutExtension;
                        var contentType = imageFile.ContentType;
                        var stream = await imageFile.OpenReadAsync();

                        var uploadFileResponse = await this.ApiManager.UploadFile(new StreamPart(stream, fileName, contentType));

                        if (uploadFileResponse.IsSuccessStatusCode)
                        {

                            var fileResponse = await uploadFileResponse.Content.ReadAsStringAsync();
                            var fileJson = await Task.Run(() => JsonConvert.DeserializeObject<FileModel>(fileResponse));

                            ImageModel image = new ImageModel { Name = fileName, Url = fileJson.Url };
                            images.Add(image);
                        }
                    }
                }


            }
            if (images.Count > 0)
            {
                message.content = StringHelper.CreateMessageWithImageLink(message.content, images);
            }
            await this.SocketManager.sendMessage(message);
            CurrentMessage = string.Empty;
            ReplyMessage = null;
            IsSending = false;
        }
    }
}
