using System;
using FreeChat.Services;
using FreeChat.Validations;
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
using FreeChat.Helpers;

namespace FreeChat.ViewModels
{
    public class SignUpViewModel : AuthBaseViewModel
    {
        SignUpValidation signUpValidation;
        Services.Navigation.INavigationService _navigationService;

        public SignUpViewModel(IDataStore<User> userDataStore, Services.Navigation.INavigationService navigationService, IApiManager apiManager, SocketManager socketManager) : base(userDataStore, apiManager, socketManager)
        {
            _navigationService = navigationService;
            signUpValidation = new SignUpValidation();
        }

        public override Task Initialize()
        {
            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            return Task.CompletedTask;
        }

        private string _signUpError;
        public string SignUpError
        {
            get => _signUpError;
            set => this.RaiseAndSetIfChanged(ref _signUpError, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private string _email;
        public string Email
        {
            get => _email;
            set => this.RaiseAndSetIfChanged(ref _email, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        private string _confirmPassword;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => this.RaiseAndSetIfChanged(ref _confirmPassword, value);
        }

        private Command _signUpCommand;
        public Command SignUpCommand
        {
            get
            {
                return _signUpCommand ?? (_signUpCommand = new Command(async () => await RunSafe(ExecuteSignUpCommand())));
            }
        }

        public async Task ExecuteSignUpCommand()
        {
            var result = signUpValidation.Validate(this);
            if (!result.IsValid) return;
            SignUpError = string.Empty;
            SignUpModel signUpModel = new SignUpModel(Name, Email, Password);
            var signUpResponse = await ApiManager.SignUp(signUpModel);
            if (signUpResponse.IsSuccessStatusCode)
            {
                var response = await signUpResponse.Content.ReadAsStringAsync();
                var authJson = await Task.Run(() => JsonConvert.DeserializeObject<AuthModel>(response));
                // Save access token
                await SecureStorage.SetAsync(FreeChat.Helpers.Constants.AccessTokenKey, authJson.AccessToken);
                await SecureStorage.SetAsync(FreeChat.Helpers.Constants.RefreshTokenKey, authJson.RefreshToken);
                var getProfileResponse = await ApiManager.GetProfile(authJson.AccessToken);
                if (getProfileResponse.IsSuccessStatusCode)
                {
                    var userResponse = await getProfileResponse.Content.ReadAsStringAsync();
                    var userJson = await Task.Run(() => JsonConvert.DeserializeObject<UserModel>(userResponse));
                    // Save user info
                    await SecureStorage.SetAsync(Constants.UserIdKey, userJson.Id);
                    await SecureStorage.SetAsync(Constants.ProfileIdKey, userJson.Profile.Id);
                    await SecureStorage.SetAsync(Constants.UsernameKey, userJson.Profile.Name);
                    if (userJson.Profile.ProfilePic != null)
                    {
                        await SecureStorage.SetAsync(Constants.AvatarKey, userJson.Profile.ProfilePic);
                    }
                    SocketManager.initSocketIO(authJson.AccessToken);
                    await GoToConversations();
                }
                await GoToConversations();
            }
            else
            {
                UserDialogs.Instance.Toast("Unable to sign up", TimeSpan.FromSeconds(3));
            }
        }

        private Command _navigateToSignInCommand;
        public Command NavigateToSignInCommand
        {
            get
            {
                return _navigateToSignInCommand ?? (_navigateToSignInCommand = new Command(async _ =>
                await AppShell.Current.Navigation.PopModalAsync()));
            }
        }

        private Command _navigateToConversationsCommand;
        public Command NavigateToConversationsCommand
        {
            get
            {
                return _navigateToConversationsCommand ?? (_navigateToConversationsCommand = new Command(async () => await GoToConversations()));
            }
        }

        Task GoToConversations()
        {
            return _navigationService.GotoPage($"{Services.Navigation.Constants.ConversationsPageUrl}");
        }
    }
}

