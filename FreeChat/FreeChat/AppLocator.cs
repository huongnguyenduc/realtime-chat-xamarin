using FreeChat.Services;
using FreeChat.Services.MockDataStores;
using FreeChat.Services.Navigation;
using FreeChat.ViewModels;
using Models;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeChat
{
    public static class AppLocator
    {
        public static SocketManager SocketManager => Locator.Current.GetService<SocketManager>();
        public static IApiManager ApiManager = new ApiManager(new ApiService<IChatApi>(Config.ApiUrl));
        public static IDataStore<User> UserDataStores => Locator.Current.GetService<IDataStore<User>>();
        public static IConversationsDataStore ConversationsDataStore => Locator.Current.GetService<IConversationsDataStore>();
        public static IMessageDataStore MessagesDataStore => Locator.Current.GetService<IMessageDataStore>();
        public static SettingsViewModel SettingsViewModel => Locator.Current.GetService<SettingsViewModel>();
        public static ProfileViewModel ProfileViewModel => Locator.Current.GetService<ProfileViewModel>();
        public static ConversationsViewModel ConversationsViewModel => Locator.Current.GetService<ConversationsViewModel>();
        public static SearchViewModel SearchViewModel => Locator.Current.GetService<SearchViewModel>();
        public static MessagesViewModel MessagesViewModel => Locator.Current.GetService<MessagesViewModel>();
        public static SignUpViewModel SignUpViewModel => Locator.Current.GetService<SignUpViewModel>();
        public static SignInViewModel SignInViewModel => Locator.Current.GetService<SignInViewModel>();
        public static SplashViewModel SplashViewModel => Locator.Current.GetService<SplashViewModel>();
        public static INavigationService NavigationService => Locator.Current.GetService<INavigationService>();
        public static string CurrentUserId { get; set; }
        public static User CurrentUser { get; set; }

        public static async void Initialize()
        {
            Locator.CurrentMutable.RegisterConstant<SocketManager>(new SocketManager());
            //Locator.CurrentMutable.RegisterConstant<ApiManager>(new ApiManager(new ApiService<IChatApi>(Config.ApiUrl)));

            Locator.CurrentMutable.Register<INavigationService>(() => new SimpleNavigationService());
            Locator.CurrentMutable.RegisterConstant<IDataStore<User>>(new UserDataStores());
            var users = await UserDataStores.GetItemsAsync();
            Locator.CurrentMutable.RegisterConstant<IConversationsDataStore>(new ConversationsDataStore(users.Last(), new List<User>(users), ApiManager));
            var conversations = await ConversationsDataStore.GetItemsAsync();
            Locator.CurrentMutable.RegisterConstant<IMessageDataStore>(new MessagesDataStore(conversations.First(), ApiManager, SocketManager));
            Locator.CurrentMutable.Register(() => new ConversationsViewModel(UserDataStores,
                ConversationsDataStore, MessagesDataStore, NavigationService, ApiManager, SocketManager));
            Locator.CurrentMutable.Register(() => new SearchViewModel(UserDataStores,
                ConversationsDataStore, MessagesDataStore, NavigationService, ApiManager, SocketManager));
            Locator.CurrentMutable.Register(() => new MessagesViewModel(UserDataStores,
                ConversationsDataStore, MessagesDataStore, ApiManager, SocketManager, NavigationService));
            Locator.CurrentMutable.Register(() => new SettingsViewModel(UserDataStores,
                ConversationsDataStore, MessagesDataStore, ApiManager, SocketManager, NavigationService));
            Locator.CurrentMutable.Register(() => new ProfileViewModel(UserDataStores,
                ConversationsDataStore, MessagesDataStore, ApiManager, SocketManager));
            Locator.CurrentMutable.Register(() => new SignUpViewModel(UserDataStores, NavigationService, ApiManager, SocketManager));
            Locator.CurrentMutable.Register(() => new SignInViewModel(UserDataStores, NavigationService, ApiManager, SocketManager));
            Locator.CurrentMutable.Register(() => new SplashViewModel(UserDataStores, NavigationService, ApiManager, SocketManager));

            CurrentUserId = users.Last().Id;
            CurrentUser = users.Last();
        }
    }
}
