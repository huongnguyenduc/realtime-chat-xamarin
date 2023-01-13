using System;
using FreeChat.Services;
using FreeChat.Validations;
using FreeChat.Helpers;
using Models;
using System.Threading.Tasks;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI;
using Xamarin.Forms;
using Newtonsoft.Json;
using Acr.UserDialogs;
using System.Diagnostics;
using Xamarin.Essentials;
using System.Collections.Generic;

namespace FreeChat.ViewModels
{
    public class SplashViewModel : AuthBaseViewModel
    {
        Services.Navigation.INavigationService _navigationService;

        public SplashViewModel(IDataStore<User> userDataStore, Services.Navigation.INavigationService navigationService, IApiManager apiManager, SocketManager socketManager) : base(userDataStore, apiManager, socketManager)
        {
            _navigationService = navigationService;
        }

        public override Task Initialize()
        {
            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            return Task.CompletedTask;
        }

        private Command _navigateCommand;
        public Command NavigateCommand
        {
            get
            {
                return _navigateCommand ?? (_navigateCommand = new Command(async () => await RunSafe(ExecuteNavigateCommand())));
            }
        }

        public async Task ExecuteNavigateCommand()
        {
            string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
            if (token == null || token == "")
            {
                await GoToSignIn();
                return;
            }
            var getProfileResponse = await ApiManager.GetProfile(token);
            Debug.WriteLine(getProfileResponse);
            if (getProfileResponse.IsSuccessStatusCode)
            {
                var response = await getProfileResponse.Content.ReadAsStringAsync();
                var userJson = await Task.Run(() => JsonConvert.DeserializeObject<UserModel>(response));
                // Save user info
                await SecureStorage.SetAsync(Constants.UserIdKey, userJson.Id);
                await SecureStorage.SetAsync(Constants.ProfileIdKey, userJson.Profile.Id);
                await SecureStorage.SetAsync(Constants.UsernameKey, userJson.Profile.Name);
                if (userJson.Profile.ProfilePic != null)
                {
                    await SecureStorage.SetAsync(Constants.AvatarKey, userJson.Profile.ProfilePic);
                }
                await GoToConversations();
                SocketManager.initSocketIO(token);
            }
            else
            {
                await GoToSignIn();
                return;
            }
        }

        Task GoToConversations()
        {
            return _navigationService.GotoPage($"{Services.Navigation.Constants.ConversationsPageUrl}");
        }


        Task GoToSignIn()
        {
            return _navigationService.GotoPage($"{Services.Navigation.Constants.SignInPageUrl}");
        }
    }
}

