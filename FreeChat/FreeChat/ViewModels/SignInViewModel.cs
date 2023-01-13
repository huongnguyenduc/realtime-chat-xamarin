using System;
using System.Threading.Tasks;
using Acr.UserDialogs;
using FreeChat.Services;
using FreeChat.Validations;
using Models;
using Newtonsoft.Json;
using ReactiveUI;
using Xamarin.Essentials;
using Xamarin.Forms;
using FreeChat.Helpers;
using System.Diagnostics;

namespace FreeChat.ViewModels
{
    public class SignInViewModel : AuthBaseViewModel
    {
        SignInValidation signInValidation;
        Services.Navigation.INavigationService _navigationService;

        public SignInViewModel(IDataStore<User> userDataStore, Services.Navigation.INavigationService navigationService, IApiManager apiManager, SocketManager socketManager) : base(userDataStore, apiManager, socketManager)
        {
            _navigationService = navigationService;
            signInValidation = new SignInValidation();
        }

        public override Task Initialize()
        {
            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            return Task.CompletedTask;
        }

        private string _signInError;
        public string SignInError
        {
            get => _signInError;
            set => this.RaiseAndSetIfChanged(ref _signInError, value);
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

        private Command _signInCommand;
        public Command SignInCommand
        {
            get
            {
                return _signInCommand ?? (_signInCommand = new Command(async () => await RunSafe(ExecuteSignInCommand())));
            }
        }

        public async Task ExecuteSignInCommand()
        {
            try
            {
                Debug.WriteLine(":>> 1");
                Console.WriteLine(":>> 1");
                var result = signInValidation.Validate(this);
                if (!result.IsValid) return;
                Debug.WriteLine(":>> 2");
                Console.WriteLine(":>> 2");
                SignInError = string.Empty;
                SignInModel signInModel = new SignInModel(Email, Password);
                Debug.WriteLine(":>> 3");
                Console.WriteLine(":>> 3");
                Debug.WriteLine(ApiManager);
                Console.WriteLine(SocketManager);
                var signInResponse = await ApiManager.SignIn(signInModel);
                Debug.WriteLine(":>> 4");
                Console.WriteLine(":>> 4");
                if (signInResponse.IsSuccessStatusCode)
                {
                    Debug.WriteLine(":>> 5");
                    Console.WriteLine(":>> 5");
                    var response = await signInResponse.Content.ReadAsStringAsync();
                    var authJson = await Task.Run(() => JsonConvert.DeserializeObject<AuthModel>(response));
                    // Save access token
                    await SecureStorage.SetAsync(FreeChat.Helpers.Constants.AccessTokenKey, authJson.AccessToken);
                    await SecureStorage.SetAsync(FreeChat.Helpers.Constants.RefreshTokenKey, authJson.RefreshToken);
                    var getProfileResponse = await ApiManager.GetProfile(authJson.AccessToken);
                    Debug.WriteLine(":>> 6");
                    Console.WriteLine(":>> 6");
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
                    UserDialogs.Instance.Toast("Unable to sign in", TimeSpan.FromSeconds(3));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't login");
                Console.WriteLine(e);
            }
        }

        private Command _navigateToSignUpCommand;
        public Command NavigateToSignUpCommand
        {
            get
            {
                return _navigateToSignUpCommand ?? (_navigateToSignUpCommand = new Command(async () => await GoToSignUp()));
            }
        }

        Task GoToSignUp()
        {
            return _navigationService.GotoPage($"{Services.Navigation.Constants.SignUpPageUrl}");
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

