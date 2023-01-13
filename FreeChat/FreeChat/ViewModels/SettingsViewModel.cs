using FreeChat.Services;
using Models;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace FreeChat.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        Services.Navigation.INavigationService _navigationService;
        public ICommand UpdateProfileSelectedCommand { get; private set; }
        public SettingsViewModel(IDataStore<User> userDataStore, IConversationsDataStore convDataStore,
            IMessageDataStore messageDataStore, IApiManager apiManager, SocketManager socketManager, Services.Navigation.INavigationService navigationService) : base(userDataStore, convDataStore, messageDataStore, apiManager, socketManager)
        {
            _navigationService = navigationService;
            UpdateProfileSelectedCommand = ReactiveCommand.CreateFromTask<Conversation>(UpdateProfileSelected);
        }

        public override async Task Initialize()
        {
            IsBusy = true;
            await LoadUserProfile();
            IsBusy = false;
        }

        public override Task Stop()
        {
            return Task.CompletedTask;
        }

        Task UpdateProfileSelected(Conversation conversation)
        {
            return _navigationService.GotoPage($"{Services.Navigation.Constants.ProfilePageUrl}");
        }

        private Command _signOutCommand;
        public Command SignOutCommand
        {
            get
            {
                return _signOutCommand ?? (_signOutCommand = new Command(async () => await RunSafe(ExecuteSignOutCommand())));
            }
        }

        public async Task ExecuteSignOutCommand()
        {
            string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
            if (token == null || token == "")
            {
                await AppShell.Current.Navigation.PopToRootAsync();
                return;
            }
            SocketManager.dispose();
            await ApiManager.Logout(token);
            SecureStorage.RemoveAll();
            await AppShell.Current.Navigation.PopToRootAsync();
        }

        private User _userProfile;
        public User UserProfile
        {
            get { return _userProfile; }
            set { this.RaiseAndSetIfChanged(ref _userProfile, value); }
        }

        public async Task LoadUserProfile()
        {
            string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
            if (token == null || token == "")
            {
                await AppShell.Current.Navigation.PopToRootAsync();
                return;
            }

            var getProfileResponse = await ApiManager.GetProfile(token);

            if (getProfileResponse.IsSuccessStatusCode)
            {
                var userResponse = await getProfileResponse.Content.ReadAsStringAsync();
                var userJson = await Task.Run(() => JsonConvert.DeserializeObject<UserModel>(userResponse));

                UserProfile = new User(
                       userJson.Profile?.Name ?? "Unknown",
                       userJson.Profile?.Bio ?? "I like chatting online and making new friends on social media",
                       userJson?.Profile?.ProfilePic ?? "john.jpg", 5, 230,
                       userJson?.Email ?? "example@abc.com"
                       )
                { Id = userJson?.Id ?? Guid.NewGuid().ToString(), IsOnline = userJson.IsOnline };
            }


        }
    }
}
