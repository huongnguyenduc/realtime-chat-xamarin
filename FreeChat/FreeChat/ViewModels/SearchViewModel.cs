using FreeChat.Services;
using FreeChat.Services.Navigation;
using Models;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;

namespace FreeChat.ViewModels
{
    public class SearchViewModel : BaseViewModel
    {
        private CancellationTokenSource _throttleCts = new CancellationTokenSource();
        INavigationService _navigationService;
        private string _searchText;
        private int Page = 0;
        private int Take = 10;
        private bool HasNextPage = true;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                DebouncedSearch();
                this.RaiseAndSetIfChanged(ref _searchText, value);
            }
        }

        private ObservableCollection<Conversation> _users;
        public ObservableCollection<Conversation> Users
        {
            get { return _users; }
            set { this.RaiseAndSetIfChanged(ref _users, value); }
        }

        public ICommand UserSelectedCommand { get; private set; }
        public ICommand LoadMoreCommand { get; private set; }

        public SearchViewModel(IDataStore<User> userDataStore, IConversationsDataStore convDataStore,
            IMessageDataStore messageDataStore, INavigationService navigationService, IApiManager apiManager, SocketManager socketManager)
            : base(userDataStore, convDataStore, messageDataStore, apiManager, socketManager)
        {
            _navigationService = navigationService;
            UserSelectedCommand = ReactiveCommand.CreateFromTask<Conversation>(UserSelected);
            LoadMoreCommand = ReactiveCommand.CreateFromTask(LoadMoreUsers);
        }

        async Task UserSelected(Conversation _conversation)
        {
            string userId = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.UserIdKey);
            string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
            ConversationRequest conversationRequest = new ConversationRequest { FriendId = _conversation.Id };
            var createConversationResponse = await ApiManager.CreateConversation(token, conversationRequest);
            var conversationResponse = await createConversationResponse.Content.ReadAsStringAsync();
            var conversationModel = await Task.Run(() => JsonConvert.DeserializeObject<ConversationModel>(conversationResponse));
            UserModel partner = conversationModel.Users.First(user => !user.Id.Equals(userId));
            int randomHours = new Random().Next(0, 24);
            Conversation conversation = new Conversation();
            conversation.Id = conversationModel.Id;
            conversation.UserIds = new string[] { conversationModel.Users[0].Id, conversationModel.Users[1].Id };
            MessageModel lastMessage = conversationModel.LastMessage;
            conversation.LastMessage = new Message
            {
                Content = conversationModel?.LastMessage?.Content ?? "Say hello",
                ISent = conversationModel?.LastMessage != null && conversationModel.LastMessage.SenderId.Equals(userId),
                CreationDate = lastMessage != null ? DateTime.Parse(lastMessage.UpdatedAt, System.Globalization.CultureInfo.CurrentCulture) : DateTime.Parse(conversationModel.UpdatedAt, System.Globalization.CultureInfo.CurrentCulture),
                Sender = new User(
                    partner.Profile?.Name ?? "Unknown",
                    partner.Profile?.Bio ?? "I like chatting online and making new friends on social media",
                    partner?.Profile?.ProfilePic ?? "john.jpg", 5, 230)
                { Id = partner?.Id ?? Guid.NewGuid().ToString(), IsOnline = true }
            };
            conversation.Peer = new User(
                partner?.Profile?.Name ?? "Unknown",
                partner.Profile?.Bio ?? "I like chatting online and making new friends on social media",
            partner?.Profile?.ProfilePic ?? "john.jpg", 5, 230)
            { Id = partner?.Id ?? Guid.NewGuid().ToString(), IsOnline = true };
            await _conversationDataStore.AddItemAsync(conversation);
            await _navigationService.GotoPage($"{Constants.MessagesPageUrl}?conversation_id={conversation.Id}");
        }

        public override async Task Initialize()
        {
            Users = new ObservableCollection<Conversation>();

            await SearchUsers();

        }

        private async Task DebouncedSearch()
        {
            try
            {

                Interlocked.Exchange(ref _throttleCts, new CancellationTokenSource()).Cancel();
                //NOTE THE 500 HERE - WHICH IS THE TIME TO WAIT
                await Task.Delay(TimeSpan.FromMilliseconds(1000), _throttleCts.Token)

                    //NOTICE THE "ACTUAL" SEARCH METHOD HERE
                    .ContinueWith(async task =>
                    {
                        await SearchUsers();
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

        async Task SearchUsers()
        {
            //if (!HasNextPage) return;
            if (IsBusy) return;

            IsBusy = true;
            string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
            if (token == null || token.Equals("")) return;
            var getUsersResponse = await ApiManager.GetProfiles(token, _searchText ?? "", 1, Take);
            var userResponse = await getUsersResponse.Content.ReadAsStringAsync();
            var profilesJson = await Task.Run(() => JsonConvert.DeserializeObject<ProfilesModel>(userResponse));

            Users = new ObservableCollection<Conversation>();

            for (int i = 0; i < profilesJson.Data.Count; i++)
            {
                UserModel user = profilesJson.Data[i];
                Conversation conversation = new Conversation
                {
                    Id = user.Id,
                    Peer = new User
                    {
                        Id = user.Id,
                        Bio = user.Profile.Bio,
                        ProfilePic = user.Profile.ProfilePic,
                        Name = user.Profile.Name

                    },
                };
                Users.Add(conversation);
            }

            IsBusy = false;
            Page = profilesJson.Meta.Page;
            HasNextPage = profilesJson.Meta.HasNextPage;
        }

        async Task LoadMoreUsers()
        {
            if (!HasNextPage) return;
            if (IsBusy) return;

            IsBusy = true;
            string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
            if (token == null || token.Equals("")) return;
            var getUsersResponse = await ApiManager.GetProfiles(token, _searchText ?? "", Page + 1, Take);
            var userResponse = await getUsersResponse.Content.ReadAsStringAsync();
            var profilesJson = await Task.Run(() => JsonConvert.DeserializeObject<ProfilesModel>(userResponse));


            for (int i = 0; i < profilesJson.Data.Count; i++)
            {
                UserModel user = profilesJson.Data[i];
                Conversation conversation = new Conversation
                {
                    Id = user.Id,
                    Peer = new User
                    {
                        Id = user.Id,
                        Bio = user.Profile.Bio,
                        ProfilePic = user.Profile.ProfilePic,
                        Name = user.Profile.Name

                    },
                };
                Users.Add(conversation);
            }

            IsBusy = false;
            Page = profilesJson.Meta.Page;
            HasNextPage = profilesJson.Meta.HasNextPage;
        }

        public override Task Stop()
        {
            return Task.CompletedTask;
        }
    }
}
