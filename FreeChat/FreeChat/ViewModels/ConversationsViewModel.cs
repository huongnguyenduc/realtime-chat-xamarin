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
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace FreeChat.ViewModels
{
    public class ConversationsViewModel : BaseViewModel
    {
        INavigationService _navigationService;
        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set { this.RaiseAndSetIfChanged(ref _searchText, value); }
        }

        private string _username;
        public string Username
        {
            get { return _username; }
            set { this.RaiseAndSetIfChanged(ref _username, value); }
        }

        private int _onlineCount;
        public int OnlineCount
        {
            get { return _onlineCount; }
            set { this.RaiseAndSetIfChanged(ref _onlineCount, value); }
        }

        private ObservableCollection<Conversation> _conversations;
        public ObservableCollection<Conversation> Conversations
        {
            get { return _conversations; }
            set { this.RaiseAndSetIfChanged(ref _conversations, value); }
        }

        public ICommand ConversationSelectedCommand { get; private set; }
        public ICommand FilterOptionChangedCommand { get; private set; }
        public ICommand SearchSelectedCommand { get; private set; }

        public ConversationsViewModel(IDataStore<User> userDataStore, IConversationsDataStore convDataStore,
            IMessageDataStore messageDataStore, INavigationService navigationService, IApiManager apiManager, SocketManager socketManager)
            : base(userDataStore, convDataStore, messageDataStore, apiManager, socketManager)
        {
            _navigationService = navigationService;
            ConversationSelectedCommand = ReactiveCommand.CreateFromTask<Conversation>(ConversationSelected);
            SearchSelectedCommand = ReactiveCommand.CreateFromTask(SearchSelected);
            FilterOptionChangedCommand = ReactiveCommand.CreateFromTask<bool>(FilterOptionChanged, _notBusyObservable);
            MessagingCenter.Subscribe<SocketManager, OnlineModel>(SocketManager, FreeChat.Helpers.Constants.ReceiveOnline, async (s, args) =>
            {
                Console.WriteLine(s);
                Console.WriteLine(args);
                Console.WriteLine(":>> receive broadcast online");
                Console.WriteLine(args.UserId);
                Console.WriteLine(args.IsOnline);
                await UpdateOnline();
            });

            MessagingCenter.Subscribe<SocketManager, IncomeMessage>(SocketManager, FreeChat.Helpers.Constants.ReceiveMessage, async (s, args) =>
            {
                Console.WriteLine(s);
                Console.WriteLine(args);
                Console.WriteLine(":>> receive broadcast message");
                Console.WriteLine(args.UserId);
                Console.WriteLine(args.Content);
                await ReceiveMessage(args);
            });
        }

        async Task ReceiveMessage(IncomeMessage incomeMessage)
        {
            await LoadConversations();
        }

        async Task UpdateOnline()
        {
            await LoadConversations();
        }

        Task ConversationSelected(Conversation conversation)
        {
            return _navigationService.GotoPage($"{Constants.MessagesPageUrl}?conversation_id={conversation.Id}");
        }

        Task SearchSelected()
        {
            return _navigationService.GotoPage(Constants.SearchPageUrl);
        }

        async Task FilterOptionChanged(bool notOnline)
        {
            IsBusy = true;
            if (!notOnline)
            {
                var conversations = await _conversationDataStore.GetConversationsForUser(AppLocator.CurrentUserId);
                var onlineConversations = conversations.Where(c => c.Peer.IsOnline);
                OnlineCount = onlineConversations.Count();
                Conversations = new ObservableCollection<Conversation>(onlineConversations);
            }
            else
            {
                await LoadConversations();
            }
            IsBusy = false;
        }

        public override async Task Initialize()
        {
            IsBusy = true;
            await LoadConversations();
            IsBusy = false;
        }

        async Task LoadConversations()
        {
            string username = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.UsernameKey);
            Username = username;
            var conversations = await _conversationDataStore.GetConversationsForUser(AppLocator.CurrentUserId);
            var onlineConversations = conversations.Where(c => c.Peer.IsOnline);
            OnlineCount = onlineConversations.Count();
            Conversations = new ObservableCollection<Conversation>(conversations);
        }

        public override Task Stop()
        {
            return Task.CompletedTask;
        }
    }
}
